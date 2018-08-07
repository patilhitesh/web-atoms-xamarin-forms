using Org.Liquidplayer.Javascript;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace WebAtoms
{
    public class AppExceptionHandler : Java.Lang.Object, JSContext.IJSExceptionHandler
    {
        Action<JSException> action;
        public AppExceptionHandler(Action<JSException> action)
        {
            this.action = action;
        }

        void JSContext.IJSExceptionHandler.Handle(JSException p0)
        {
            action(p0);
        }
    }

    public class AtomBridge: IJSService
    {

        public JSContext engine;

        public HttpClient client;

        public AtomBridge()
        {
            try
            {
                engine = new JSContext();
                engine.SetExceptionHandler(new AppExceptionHandler((e) => {
                    if (e.Error != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"{e.Error.Message()}\r\n{e.Error.Stack()}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(e.ToString());
                    }
                }));

                client = new HttpClient();
                engine.ExecuteScript("function __paramArrayToArrayParam(t, f) { return function() { var a = []; for(var i=0;i<arguments.length;i++) { a.push(arguments[i]); } return f.call(t, a); } }", "vm");
                // engine.SetJSPropertyValue("global", engine);
                engine.SetJSPropertyValue("document", null);

                // engine.Global.Put("global", engine.Global, false);
                // engine.Global.Put("App", Jint.Native.JsValue.FromObject(engine, WAContext.Current), true);
                // engine.Global.Put("bridge", Jint.Native.JsValue.FromObject(engine, this), true);

                // engine.Global.Put("document", Jint.Native.JsValue.Null, true);
                // Execute("var global = {};");
                var v8Bridge = engine.AddClrObject(this);
                engine.SetJSPropertyValue("bridge", v8Bridge);
                Execute("var console = {};");
                Execute("console.log = function(l) { bridge.log('log', l); };");
                Execute("console.warn = function(l) { bridge.log('warn', l); };");
                Execute("console.error = function(l) { bridge.log('error', l); };");

                Execute("console.log('Started .... ');");

                Execute("var setInterval = function(v,i){ return bridge.setInterval(v,i, false); };");
                Execute("var clearInterval = function(i){ bridge.clearInterval(i); };");
                Execute("var setTimeout = function(v,i){ return bridge.setInterval(v,i, true); };");
                Execute("var clearTimeout = clearInterval;");
            }
            catch (Exception ex) {
                throw;
            }
        }

        private bool initialized = false;

        public void Log(string title, JSValue text) {
            //if (title != "log")
            //{
            //    Debugger.Break();
            //}
            System.Diagnostics.Debug.WriteLine($"{title}: {text}");
        }

        public async Task InitAsync(string url) {
            if (initialized)
                return;

            try
            {
                //await ExecuteScriptAsync("https://cdn.jsdelivr.net/npm/promise-polyfill@8/dist/polyfill.min.js");
                //await ExecuteScriptAsync($"{url}/polyfills/endsWith.js");
                //await ExecuteScriptAsync($"{url}/polyfills/startsWith.js");
                //await ExecuteScriptAsync($"{url}/polyfills/includes.js");

                await ExecuteScriptAsync($"{url}/umd.js");

                Execute("UMD.viewPrefix = 'xf';");
                Execute("UMD.defaultApp = 'web-atoms-core/dist/xf/XFApp';");
                Execute("AmdLoader.moduleLoader = function(n,u,s,e) { bridge.loadModuleScript(n,u,s,e); }");
                Execute("AmdLoader.moduleProgress= function(n,i) { bridge.moduleProgress(n,i); }");
            }
            catch (Exception ex) {
                throw;
            }
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
            catch ( JSException  jse) {
                System.Diagnostics.Debug.WriteLine($"{jse.ToString()}\r\n{jse.Stack()}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            //});
        }

        public async Task ExecuteScriptAsync(string url) {
            //            System.Diagnostics.Debug.WriteLine($"Loading url {url}");
            try
            {
                string script = await client.GetStringAsync(url);
                Execute(script, url);
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Failed to load url {url}\r\n{ex}");
                throw;
            }
        }

        public void LoadContent(JSWrapper elementTarget, string content) {
            Element element = elementTarget.As<Element>();
            (element as Page).LoadFromXaml(content);
        }

        public Element FindChild(JSWrapper rootTarget, string name)
        {
            Element root = rootTarget.As<Element>();
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

        public int SetInterval(JSFunction value, int milliSeconds, bool once) {

            System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();

            Device.BeginInvokeOnMainThread(async () => {
                while (!cancellation.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(milliSeconds), cancellation.Token);
                        value.Call(null, new Java.Lang.Object[] {  });
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

        public void Alert(string message, string title, JSFunction success, JSFunction error) {
            Device.BeginInvokeOnMainThread(async () => {
                try
                {
                    await Application.Current.MainPage.DisplayAlert(title, message, "Ok");
                    success.Call(null, new Java.Lang.Object[] { });
                }
                catch (Exception ex)
                {
                    error.Call(null, new Java.Lang.Object[] { ex.ToString() });
                }
            });
        }

        public void Confirm(string message, string title, JSFunction success, JSFunction error)
        {
            Device.BeginInvokeOnMainThread(async () => {
                try
                {
                    bool result = await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No");
                    success.Call(null, new Java.Lang.Object[] { result });
                }
                catch (Exception ex)
                {
                    error.Call(null, new Java.Lang.Object[] { ex.ToString() });
                }
            });
        }

        public void Ajax(string url, JSObject ajaxOptions, JSFunction success, JSFunction failed, JSFunction progress) {
            var client = this.client;
            var service = AjaxService.Instance;
            service.Invoke(client, url, ajaxOptions, success, failed, progress);
        }

        public void PushPage(JSWrapper wrapper, JSFunction success, JSFunction error) {
            Device.BeginInvokeOnMainThread(async () => {
                try
                {
                    var e = wrapper.As<Page>();
                    await Application.Current.MainPage.Navigation.PushAsync(e, true);
                    success.Call(null, new Java.Lang.Object[] { });
                }
                catch (Exception ex) {
                    error.Call(null, new Java.Lang.Object[] { ex.ToString() });
                }
            });
        }

        public static AtomBridge Instance = new AtomBridge();

        IEnumerable<System.Reflection.TypeInfo> types;

        public Element Create(string name)
        {

            types = types ?? AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany<System.Reflection.Assembly, System.Reflection.TypeInfo>(x => {
                    try {
                        return x.ExportedTypes.Select(t => t.GetTypeInfo());
                    }
                    catch { }
                    return new System.Reflection.TypeInfo[] { };
                }).ToList();

            var type = types.FirstOrDefault(x => x.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));

            Element view = Activator.CreateInstance(type) as Element;
            return view;
        }

        public void AttachControl(JSWrapper target, JSValue control) {
            Element element = target.As<Element>();
            var ac = WAContext.GetAtomControl(element);
            if(ac != null)
            {
                if (ac == control as object)
                    return;
                throw new InvalidOperationException("Control already attached");
            }
            WAContext.SetAtomControl(element, control);
        }

        public JSDisposable AddEventHandler(
            JSWrapper elementTarget, 
            string name, JSFunction callback, bool? capture)
        {
            Element element = elementTarget.As<Element>();

            var e = element.GetType().GetEvent(name);

            var pe = new AtomDelegate() { callback = callback };

            var handler = Delegate.CreateDelegate(e.EventHandlerType, pe, AtomDelegate.OnEventMethod);

            e.AddEventHandler(element, handler);

            return new JSDisposable(engine, () => {
                e.RemoveEventHandler(element, handler);
                pe.callback = null;
            });
        }
        public JSDisposable WatchProperty(JSWrapper objTarget, string name, JSFunction callback)
        {
            object obj = objTarget.Target;
            if (obj is INotifyPropertyChanged element)
            {

                var pinfo = obj.GetProperty(name);

                PropertyChangedEventHandler handler = (s, e) =>
                {
                    if (e.PropertyName == name)
                    {
                        callback.Call( null, new Java.Lang.Object[] { obj.Wrap(engine) } );
                    }
                };

                element.PropertyChanged += handler;
                return new JSDisposable(engine, () =>
                {
                    element.PropertyChanged -= handler;
                });
            }

            return new JSDisposable(engine, () => { });
        }

        public JSValue AtomParent(JSWrapper target, JSValue climbUp)
        {
            Element element = target.As<Element>();
            bool cu = !((bool)climbUp.IsUndefined()) && !((bool)climbUp.IsNull()) && (bool)climbUp.ToBoolean();
            do {
                var e = WAContext.GetAtomControl(element);
                if (e != null)
                    return e.Wrap(engine);
                element = element.Parent;
            } while (cu && element != null);
            return null;
        }

        public JSValue ElementParent(JSWrapper elementTarget) {
            Element element = elementTarget.As<Element>();
            return element.Parent?.Wrap(engine);
        }

        public JSValue TemplateParent(JSWrapper elementTarget) {
            Element element = elementTarget.As<Element>();
            do {
                var e = WAContext.GetTemplateParent(element);
                if (e != null)
                    return e.Wrap(engine);
                element = element.Parent;
            } while (element != null);
            return null;
        }

        public void SetRoot(JSWrapper wrapper) {
            WAContext.Current.CurrentPage = wrapper.As<Page>();
        }

        public void VisitDescendents(JSWrapper target, JSFunction action)
        {
            Element element = target?.As<Element>();
            if (element == null) {
                throw new ObjectDisposedException("Cannot visit descendents of null");
            }
            foreach (var e in (element as IElementController).LogicalChildren) {
                var ac = WAContext.GetAtomControl(e);
                var child = e.Wrap(engine);
                var r = action.Call(null, new Java.Lang.Object[] {
                    child,
                    (JSValue)ac});
                if ((bool)r.IsUndefined() || (bool)r.IsNull() || !((bool)r.ToBoolean()))
                    continue;
                VisitDescendents(JSWrapper.Register(e), action);
            }
        }

        public void Dispose(JSWrapper et) {
            Element e = et.As<Element>();
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

        public JSValue GetValue(JSWrapper viewTarget, string name) {
            Element view = viewTarget.As<Element>();
            var pv = view.GetProperty(name);
            var value = pv.GetValue(view);
            return value.Wrap(engine);
            // return null;
        }

        public void SetValue(JSWrapper target, string name, JSValue value) {

            Element view = target.As<Element>();

            bool isNull = (bool)value.IsNull() || (bool)value.IsUndefined();

            var pv = view.GetProperty(name);

            if (isNull) {
                pv.SetValue(view, null);
                return;
            }

            var pt = pv.PropertyType;



            if (pt == typeof(string)) {
                pv.SetValue(view, value.ToString());
                return;
            }

            // check if it is an array
            if (value is JSBaseArray array) {
                var old = pv.GetValue(view);
                if (old is IDisposable d) {
                    d.Dispose();
                }
                pv.SetValue(view, new AtomEnumerable(array));
                return;
            }

            pt = Nullable.GetUnderlyingType(pt) ?? pt;

            if (value is JSDate date) {
                // conver to datetime and set...
                pv.SetValue(view, date.ToDateTime());
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

        public void LoadModuleScript(string name, string url, JSFunction success, JSFunction error) {
            Device.BeginInvokeOnMainThread(async () => {
                try {

                    string script = await client.GetStringAsync(url);

                    success.Call(null, new Java.Lang.Object[] {new JSClrFunction(engine, (args) =>
                    {

                        Execute(script, url);

                        return new JSValue(engine);
                    }) });
                }
                catch (JSException ex) {
                    var msg = $"Failed to load url {url}\r\n{ex.Stack()}\r\n{ex}";
                    System.Diagnostics.Debug.WriteLine(msg);
                    error.Call(null, new Java.Lang.Object[] { msg });
                } catch (Exception ex) {
                    var msg = $"Failed to load url {url}\r\n{ex}";
                    System.Diagnostics.Debug.WriteLine(msg);
                    error.Call(null, new Java.Lang.Object[] { msg });
                }

            });
        }

    }
}