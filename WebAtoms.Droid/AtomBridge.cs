using Jint;
using Jint.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace WebAtoms
{
    public class AtomBridge
    {

        public AtomBridge()
        {
            engine.Global.Put("App", Jint.Native.JsValue.FromObject(engine, WAContext.Current), true);
            engine.Global.Put("bridge", Jint.Native.JsValue.FromObject(engine, this), true);

            engine.Execute("var setInterval = function(v,i){ return bridge.SetInterval(v,i); };");
            engine.Execute("var clearInterval = function(i){ bridge.ClearInterval(i); };");
        }

        private Dictionary<int,System.Threading.CancellationTokenSource> intervalCancells = new Dictionary<int, System.Threading.CancellationTokenSource>();
        private int intervalId = 1;

        public void ClearInterval(int id) {
            if (intervalCancells.Remove(id, out var cancel)) {
                cancel.Cancel();
            }
        }

        public int SetInterval(JsValue value, int milliSeconds) {

            System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();

            Device.BeginInvokeOnMainThread(async () => {
                while (!cancellation.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(milliSeconds), cancellation.Token);
                        value.Invoke();
                    }
                    catch (TaskCanceledException) { }
                }
            });

            lock (intervalCancells) {
                intervalCancells.Add(intervalId, cancellation);
                return intervalId++;
            }
        }

        public Dictionary<string, string> ModuleUrls = new Dictionary<string, string>();

        public static AtomBridge Instance = new AtomBridge();

        public string BaseUrl = "";


        public Engine engine = new Engine(a => {
            a.CatchClrExceptions(f => true);
        });

        IEnumerable<System.Reflection.TypeInfo> types;

        public Element Create(string name)
        {

            types = types ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.DefinedTypes).ToList();

            var type = types.FirstOrDefault(x => x.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));

            Element view = Activator.CreateInstance(type) as Element;
            return view;
        }

        public void AttachControl(Element element, JsValue control) {
            var ac = WAContext.GetAtomControl(element);
            if(ac != null)
            {
                throw new InvalidOperationException("Control already attached");
            }
            WAContext.SetAtomControl(element, control);
        }

        public IDisposable AddEventHandler(Element element, string name, JsValue callback, bool? capture)
        {
            PropertyChangedEventHandler handler = (s, e) => {
                if (e.PropertyName == name)
                {
                    callback.Invoke(e.PropertyName);
                }
            };

            element.PropertyChanged += handler;
            return new AtomDisposable(() => {
                element.PropertyChanged -= handler;
            });
        }

        public object AtomParent(Element element, JsValue climbUp)
        {
            bool cu = !climbUp.IsUndefined() && !climbUp.IsNull() && climbUp.AsBoolean();
            do {
                var e = WAContext.GetAtomControl(element);
                if (e != null)
                    return e;
                element = element.Parent;
            } while (cu && element != null);
            return null;
        }

        public object ElementParent(Element element) {
            return element.Parent;
        }

        public object TemplateParent(Element element) {
            do {
                var e = WAContext.GetTemplateParent(element);
                if (e != null)
                    return e;
                element = element.Parent;
            } while (element != null);
            return null;
        }

        public void VisitDescendents(Element element, JsValue action)
        {
            foreach (var e in (element as IElementController).LogicalChildren) {
                var ac = WAContext.GetAtomControl(e);
                var r = action.Invoke(
                    JsValue.FromObject(engine,e), 
                    (JsValue)ac);
                if (r.IsUndefined() || r.IsNull() || !r.AsBoolean())
                    continue;
                VisitDescendents(e, action);
            }
        }

        public void Dispose(Element e) {
            WAContext.SetAtomControl(e, null);
            WAContext.SetLogicalParent(e, null);
            WAContext.SetTemplateParent(e, null);
        }

        public void AppendChild(Element view, Element child) {
            switch (view) {
                case ContentView cv:
                    cv.Content = child as View;
                    break;
                case Layout<View> grid:
                    grid.Children.Add(child as View);
                    break;
                case ContentPage page:
                    page.Content = child as View;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        public JsValue GetValue(Element view, string name) {
            return null;
        }

        public void SetValue(Element view, string name, JsValue value) {

            bool isNull = value.IsNull() || value.IsUndefined();

            var pv = view.GetProperty(name);

            if (isNull) {
                pv.SetValue(view, null);
                return;
            }

            var pt = pv.PropertyType;

            if (pt == typeof(string)) {
                pv.SetValue(view, value.AsString());
                return;
            }

            pt = Nullable.GetUnderlyingType(pt) ?? pt;

            if (value.IsDate()) {
                // conver to datetime and set...
                pv.SetValue(view, value.AsDate().ToDateTime());
                return;
            }

            if (pt.IsValueType) {
                // convert...
                var v = Convert.ChangeType(value.ToObject(), pt);
                pv.SetValue(view, v);
            }
        }

        public string ResolveName(string baseUrl, string item) {
            if (item.StartsWith("."))
            {
                var currentUrl = new Uri(baseUrl);
                var relUrl = new Uri(item, UriKind.Relative);
                var absUrl = new Uri(currentUrl, relUrl);
                var s = absUrl.ToString() + ".js";
                Log($"Resolve(\"{baseUrl}\",\"{item}\") = \"{s}\"");
                return s;
            }

            var tokens = item.Split('/');

            var packageName = tokens.First();
            var path = string.Join("/", tokens.Skip(1));

            var url = this.ModuleUrls[packageName];

            return url + path + ".js";
        }

        public async Task ExecuteScriptAsync(string item) {
            using (var client = new HttpClient())
            {

                try
                {

                    Log($"Downloading {item}");
                    var script = await client.GetStringAsync(item);
                    Log($"Executing {item}");
                    BaseUrl = item;
                    engine.Execute(script, new Jint.Parser.ParserOptions {
                        Source = item
                    });
                }
                catch (Exception ex) {
                    Log($"Failed: {item}");
                    Log(ex);
                    throw;
                }
            }
        }

        public void AppLoaded(JsValue require, JsValue exports) {

            try
            {
                //Jint.Native.Object.ObjectConstructor oc = exports.AsObject().GetProperty("App").Value.AsObject() as Jint.Native.Object.ObjectConstructor;
                //var appObject = oc.Construct(new JsValue[] { });

                //appObject.GetProperty("main").Value.Invoke();

                engine.Global.Put("_require", require, true);
                engine.Global.Put("_exports", exports, true);

                engine.Execute($"var appBridge = _require('web-atoms-core/bin/core/bridge');");
                engine.Execute($"appBridge.AtomBridge.instance = bridge;");

                engine.Execute($"var app = new _exports.App();" +
                    $"app.main();");


                Log("App loaded");
            }
            catch (Exception ex) {
                Log(ex);

            }
        }

        public void ExecuteScript(string item, JsValue callback) {
            Device.BeginInvokeOnMainThread(async () => {

                await ExecuteScriptAsync(item);

                try
                {
                    if (callback != null)
                    {
                        callback.Invoke();
                    }
                }
                catch (Exception ex) {
                    Log(ex);
                }
                
            });
        }


        public Action<object> OnLog = l => { System.Diagnostics.Debug.WriteLine(l); };

        public void Log(object a) {
            OnLog(a);
        }



    }

    public static class DictionaryExtensions {

        static Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();

        public static TValue GetOrCreate<TKey, TValue>(
            this Dictionary<TKey,TValue> d,
            TKey key,
            Func<TKey, TValue> factory) {
            if (d.TryGetValue(key, out TValue value))
                return value;
            TValue v = factory(key);
            d[key] = v;
            return v;
        }

        public static PropertyInfo GetProperty(this object value, string name) {
            Type type = value.GetType();
            string key = $"{type.FullName}.{name}";

            return properties.GetOrCreate(key, k => type.GetProperties().First(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
        }

        public static bool IsEmpty(this string text) {
            return string.IsNullOrWhiteSpace(text);
        }
    }

    public class AtomDisposable : IDisposable
    {
        readonly Action action;

        public AtomDisposable(Action action)
        {
            this.action = action;
        }

        public void Dispose()
        {
            action?.Invoke();
        }
    }

}