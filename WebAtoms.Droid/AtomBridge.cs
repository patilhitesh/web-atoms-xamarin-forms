using Com.Eclipsesource.V8;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace WebAtoms
{
    public class AtomBridge: IJSObject
    {

        public HttpClient client;
        private V8Object nullValue;

        public AtomBridge()
        {
            client = new HttpClient();
            engine.Add("global", engine);
            engine.AddNull("document");
            nullValue = engine.GetObject("document");
            // engine.Global.Put("global", engine.Global, false);
            // engine.Global.Put("App", Jint.Native.JsValue.FromObject(engine, WAContext.Current), true);
            // engine.Global.Put("bridge", Jint.Native.JsValue.FromObject(engine, this), true);

            // engine.Global.Put("document", Jint.Native.JsValue.Null, true);
            // Execute("var global = {};");
            var v8Bridge = engine.AddClrObject(this);
            engine.Add("bridge", v8Bridge);
            Execute("var console = {};");
            Execute("console.log = function(l) { bridge.Log('log', l); };");
            Execute("console.warn = function(l) { bridge.Log('warn', l); };");
            Execute("console.error = function(l) { bridge.Log('error', l); };");

            Execute("var setInterval = function(v,i){ return bridge.SetInterval(v,i, false); };");
            Execute("var clearInterval = function(i){ bridge.ClearInterval(i); };");
            Execute("var setTimeout = function(v,i){ return bridge.SetInterval(v,i, true); };");
            Execute("var clearTimeout = clearInterval;");
        }

        private bool initialized = false;

        public void Log(string title, V8Value text) {
            System.Diagnostics.Debug.WriteLine($"{title}: {text}");
        }

        public async Task InitAsync(string url) {
            if (initialized)
                return;

            await ExecuteScriptAsync("https://cdn.jsdelivr.net/npm/promise-polyfill@8/dist/polyfill.min.js");
            await ExecuteScriptAsync($"{url}/polyfills/endsWith.js");
            await ExecuteScriptAsync($"{url}/polyfills/startsWith.js");
            await ExecuteScriptAsync($"{url}/polyfills/includes.js");

            await ExecuteScriptAsync($"{url}/umd.js");

            Execute("UMD.viewPrefix = 'xf';");
            Execute("UMD.defaultApp = 'web-atoms-core/dist/xf/XFApp';");
            Execute("AmdLoader.moduleLoader = function(n,u,s,e) { bridge.LoadModuleScript(n,u,s,e); }");
            Execute("AmdLoader.moduleProgress= function(n,i) { bridge.ModuleProgress(n,i); }");
        }

        public void ModuleProgress(string name, double progress) {
            System.Diagnostics.Debug.WriteLine(name);
        }

        public void Execute(string script, string url = null) {
            ///Device.BeginInvokeOnMainThread(() => { 
            if (url != null)
            {
                System.Diagnostics.Debug.WriteLine($"Executing: {url}");
            }
            try
            {
                engine.ExecuteScript(script, url, 0);
            }
            catch (V8ScriptException jse) {
                System.Diagnostics.Debug.WriteLine($"{jse.LineNumber} {jse.ToString()}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            //});
        }

        public async Task ExecuteScriptAsync(string url) {
//            System.Diagnostics.Debug.WriteLine($"Loading url {url}");
            string script = await client.GetStringAsync(url);
            Execute(script, url);
        }

        public void LoadContent(Element element, string content) {
            (element as Page).LoadFromXaml(content);
        }

        public Element FindChild(Element root, string name)
        {
            var item = root.FindByName<Element>(name);
            //var v = new ObjectReferenceWrapper(engine) {Target = item };
            //return v;
            return item;
        }

        private Dictionary<int,System.Threading.CancellationTokenSource> intervalCancells = new Dictionary<int, System.Threading.CancellationTokenSource>();
        private int intervalId = 1;

        public void ClearInterval(int id) {
            if (intervalCancells.Remove(id, out var cancel)) {
                cancel.Cancel();
            }
        }

        public int SetInterval(V8Function value, int milliSeconds, bool once) {

            System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();

            Device.BeginInvokeOnMainThread(async () => {
                while (!cancellation.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(milliSeconds), cancellation.Token);
                        value.Call(nullValue, new V8Array(value.Rutime));
                        if (once) {
                            break;
                        }
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


        public V8 engine = V8.CreateV8Runtime();

        IEnumerable<System.Reflection.TypeInfo> types;

        public Element Create(string name)
        {

            types = types ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.DefinedTypes).ToList();

            var type = types.FirstOrDefault(x => x.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));

            Element view = Activator.CreateInstance(type) as Element;
            return view;
        }

        public void AttachControl(Element element, V8Object control) {
            var ac = WAContext.GetAtomControl(element);
            if(ac != null)
            {
                if (ac == control as object)
                    return;
                throw new InvalidOperationException("Control already attached");
            }
            WAContext.SetAtomControl(element, control);
        }

        public IDisposable AddEventHandler(Element element, string name, V8Function callback, bool? capture)
        {
            var e = element.GetType().GetEvent(name);

            var pe = new AtomDelegate() { callback = callback };

            var handler = Delegate.CreateDelegate(e.EventHandlerType, pe, AtomDelegate.OnEventMethod);

            e.AddEventHandler(element, handler);

            return new AtomDisposable(() => {
                e.RemoveEventHandler(element, handler);
                pe.callback = null;
            });
        }
        public IDisposable WatchProperty(object obj, string name, V8Function callback)
        {
            if (obj is INotifyPropertyChanged element)
            {

                var pinfo = obj.GetProperty(name);

                PropertyChangedEventHandler handler = (s, e) =>
                {
                    if (e.PropertyName == name)
                    {
                        callback.Call( nullValue, V8ValueExtensions.ToV8Array(engine, pinfo.GetValue(obj)) );
                    }
                };

                element.PropertyChanged += handler;
                return new AtomDisposable(() =>
                {
                    element.PropertyChanged -= handler;
                });
            }

            return EmptyDisposable.instance;
        }

        public object AtomParent(Element element, V8Object climbUp)
        {
            bool cu = !climbUp.IsUndefined && climbUp != null && climbUp.GetBoolean("");
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

        public object GetValue(Element view, string name) {
            var pv = view.GetProperty(name);
            var value = pv.GetValue(view);
            return value;
            // return null;
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

            // check if it is an array
            if (value.IsArray()) {
                var old = pv.GetValue(view);
                if (old is IDisposable d) {
                    d.Dispose();
                }
                pv.SetValue(view, new AtomEnumerable(value.AsArray()));
                return;
            }

            pt = Nullable.GetUnderlyingType(pt) ?? pt;

            if (value.IsDate()) {
                // conver to datetime and set...
                pv.SetValue(view, value.AsDate().ToDateTime());
                return;
            }

            if (pt.IsValueType)
            {
                // convert...
                var v = Convert.ChangeType(value.ToObject(), pt);
                pv.SetValue(view, v);
                return;
            }
            else {
                pv.SetValue(view, value.ToObject());
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

        public void LoadModuleScript(string name, string url, JsValue success, JsValue error) {
            Device.BeginInvokeOnMainThread(async () => {
                try {

                    string script = await client.GetStringAsync(url);

                    success.Invoke(new ClrFunctionInstance(engine, (_this, args) =>
                    {

                        Execute(script, url);

                        return JsValue.Undefined;
                    }));
                } catch (Jint.Runtime.JavaScriptException ex) {
                    System.Diagnostics.Debug.WriteLine($"{ex.LineNumber} {ex.ToString()}");
                    error.Invoke(JsValue.Null, new JsString(ex.ToString()));
                }

            });
        }

        //public async Task ExecuteScriptAsync(string item) {
        //    using (var client = new HttpClient())
        //    {

        //        try
        //        {

        //            Log($"Downloading {item}");
        //            var script = await client.GetStringAsync(item);
        //            Log($"Executing {item}");
        //            BaseUrl = item;
        //            //Execute(script, new Jint.Parser.ParserOptions {
        //            //    Source = item
        //            //});
        //            Execute(script, new Esprima.ParserOptions(item) {
        //                 SourceType = SourceType.Script
        //            });
        //        }
        //        catch (Exception ex) {
        //            Log($"Failed: {item}");
        //            Log(ex);
        //            throw;
        //        }
        //    }
        //}

        //public void AppLoaded(JsValue require, JsValue exports) {

        //    try
        //    {
        //        //Jint.Native.Object.ObjectConstructor oc = exports.AsObject().GetProperty("App").Value.AsObject() as Jint.Native.Object.ObjectConstructor;
        //        //var appObject = oc.Construct(new JsValue[] { });

        //        //appObject.GetProperty("main").Value.Invoke();

        //        engine.Global.Put("_require", require, true);
        //        engine.Global.Put("_exports", exports, true);

        //        Execute($"var appBridge = _require('web-atoms-core/bin/core/bridge');");
        //        Execute($"appBridge.AtomBridge.instance = bridge;");

        //        Execute($"var app = new _exports.App();" +
        //            $"app.main();");


        //        Log("App loaded");
        //    }
        //    catch (Exception ex) {
        //        Log(ex);

        //    }
        //}

        //public void ExecuteScript(string item, JsValue callback) {
        //    Device.BeginInvokeOnMainThread(async () => {

        //        await ExecuteScriptAsync(item);

        //        try
        //        {
        //            if (callback != null)
        //            {
        //                callback.Invoke();
        //            }
        //        }
        //        catch (Exception ex) {
        //            Log(ex);
        //        }
                
        //    });
        //}


        public Action<object> OnLog = l => { System.Diagnostics.Debug.WriteLine(l); };

        public void Log(object a) {
            OnLog(a);
        }



    }
}