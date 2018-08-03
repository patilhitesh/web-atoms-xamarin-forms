﻿using Org.Liquidplayer.Javascript;
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
                    System.Diagnostics.Debug.WriteLine(e.ToString());
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
                Execute("console.log = function(l) { bridge.Log('log', l); };");
                Execute("console.warn = function(l) { bridge.Log('warn', l); };");
                Execute("console.error = function(l) { bridge.Log('error', l); };");

                Execute("console.log('Started .... ');");

                Execute("var setInterval = function(v,i){ return bridge.SetInterval(v,i, false); };");
                Execute("var clearInterval = function(i){ bridge.ClearInterval(i); };");
                Execute("var setTimeout = function(v,i){ return bridge.SetInterval(v,i, true); };");
                Execute("var clearTimeout = clearInterval;");
            }
            catch (Exception ex) {
                throw;
            }
        }

        private bool initialized = false;

        public void Log(string title, JSValue text) {
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
                Execute("AmdLoader.moduleLoader = function(n,u,s,e) { bridge.LoadModuleScript(n,u,s,e); }");
                Execute("AmdLoader.moduleProgress= function(n,i) { bridge.ModuleProgress(n,i); }");
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
            string script = await client.GetStringAsync(url);
            Execute(script, url);
        }

        public void LoadContent(JSWrapper elementTarget, string content) {
            Element element = elementTarget.As<Element>();
            (element as Page).LoadFromXaml(content);
        }

        public JSWrapper FindChild(JSWrapper rootTarget, string name)
        {
            Element root = rootTarget.As<Element>();
            var item = root.FindByName<Element>(name);
            //var v = new ObjectReferenceWrapper(engine) {Target = item };
            //return v;
            return (JSWrapper)item.Wrap(engine);
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

        public Dictionary<string, string> ModuleUrls = new Dictionary<string, string>();

        public static AtomBridge Instance = new AtomBridge();

        public string BaseUrl = "";


        IEnumerable<System.Reflection.TypeInfo> types;

        public JSWrapper Create(string name)
        {

            types = types ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.DefinedTypes).ToList();

            var type = types.FirstOrDefault(x => x.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));

            Element view = Activator.CreateInstance(type) as Element;
            return (JSWrapper)view.Wrap(engine);
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
                        callback.Call( null, new Java.Lang.Object[] { new JSWrapper(engine, obj) } );
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

        public void VisitDescendents(JSWrapper target, JSFunction action)
        {
            Element element = target.As<Element>();
            foreach (var e in (element as IElementController).LogicalChildren) {
                var ac = WAContext.GetAtomControl(e);
                var child = e.Wrap(engine);
                var r = action.Call(null, new Java.Lang.Object[] {
                    child,
                    (JSValue)ac});
                if ((bool)r.IsUndefined() || (bool)r.IsNull() || !((bool)r.ToBoolean()))
                    continue;
                VisitDescendents( child as JSWrapper, action);
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

        public string ResolveName(string baseUrl, string item) {
            if (item.StartsWith("."))
            {
                var currentUrl = new Uri(baseUrl);
                var relUrl = new Uri(item, UriKind.Relative);
                var absUrl = new Uri(currentUrl, relUrl);
                var s = absUrl.ToString() + ".js";
                Log("Info", new JSValue(engine, $"Resolve(\"{baseUrl}\",\"{item}\") = \"{s}\""));
                return s;
            }

            var tokens = item.Split('/');

            var packageName = tokens.First();
            var path = string.Join("/", tokens.Skip(1));

            var url = this.ModuleUrls[packageName];

            return url + path + ".js";
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
                } catch (JSException ex) {
                    System.Diagnostics.Debug.WriteLine($"{ex.Stack()} {ex.ToString()}");
                    error.Call(null, new Java.Lang.Object[] { ex.ToString() + "\r\n" + ex.Stack() });
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

        public void LogObject(object a) {
            OnLog(a);
        }



    }
}