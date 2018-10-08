using Foundation;
using JavaScriptCore;
using Rg.Plugins.Popup.Pages;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
[assembly: Xamarin.Forms.Dependency(typeof(WebAtoms.AtomBridge))]
[assembly: Xamarin.Forms.Dependency(typeof(WebAtoms.NavigationService))]
namespace WebAtoms
{

    public class NavigationService : IJSService
    {

        private string location;

        public string GetLocation() {
            return location;
        }

        public void SetLocation(string location)
        {
            this.location = location;
            AtomBridge.Client.BaseAddress = new Uri(location);
            AtomBridge.LoadApplication(location);
        }

        public void Refresh()
        {

            AtomBridge.Instance.Reset();

        }

        public void Back() {

            Application.Current.MainPage.SendBackButtonPressed();

        }

    }


    public class AtomBridge: IJSService
    {

        public JSContext Engine { get; }

        public static HttpClient Client { get; } = new AtomWebClient().Client;

        private static List<(string, IJSService)> registrations = new List<(string, IJSService)>() {
            ("preferences", DependencyService.Get<PreferenceService>()),
            ("navigationService", DependencyService.Get<NavigationService>())
        };

        public static void RegisterService(string name, IJSService service) {
            registrations.Add((name, service));
        }

        public AtomBridge()
        {
            try
            {
                Engine = new JSContext();
                Engine.ExceptionHandler = (c, e) => {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                    if (e[(NSString)"stack"] != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"{e[(NSString)"message"]?.ToString()}\r\n{e[(NSString)"stack"]?.ToString()}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine(e.ToString());
                    }
                };

                // Client = (new AtomWebClient()).Client;
                Engine.ExecuteScript("function __paramArrayToArrayParam(t, f) { return function() { var a = []; for(var i=0;i<arguments.length;i++) { a.push(arguments[i]); } return f.call(t, a); } }", "vm");
                // engine.SetJSPropertyValue("global", engine);
                Engine.SetJSPropertyValue("document", null);
                Engine.SetJSPropertyValue("location", null);

                // Engine.ExecuteScript("function __setPropertiesToJSObject(t, n, g, s) { Object.defineProperty(t, n, { get: g, set: s, enumerable: true, configurable: true }); }", "setProperties");

                // engine.Global.Put("global", engine.Global, false);
                // engine.Global.Put("App", Jint.Native.JsValue.FromObject(engine, WAContext.Current), true);
                // engine.Global.Put("bridge", Jint.Native.JsValue.FromObject(engine, this), true);

                // engine.Global.Put("document", Jint.Native.JsValue.Null, true);
                // Execute("var global = {};");
                var v8Bridge = Engine.AddClrObject(this, "bridge");

                foreach (var (name, service) in registrations) {
                    v8Bridge.AddClrObject(service, name);
                }

                Execute("var console = {};");
                Execute("console.log = function(l) { bridge.log('log', l); };");
                Execute("console.warn = function(l) { bridge.log('warn', l); };");
                Execute("console.error = function(l) { bridge.log('error', l); if(l.stack) { bridge.log('error', l.stack); } };");

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

        // public PreferenceService Preferences { get; set; }

        public void Log(string title, JSValue text) {
            //if (title != "log")
            //{
            //    Debugger.Break();
            //    var obj = text.ToObject();
            //}
            System.Diagnostics.Debug.WriteLine($"{title}: {text}");
        }

        public static string AmdUrl;
        private static string startUrl;

        public static AtomBridge Instance;

        public void Reset() {
            Instance = DependencyService.Get<AtomBridge>(DependencyFetchTarget.NewInstance);
            if (startUrl != null) {
                LoadApplication(startUrl);
            }
        }

        public static void LoadApplication(string url) {
            startUrl = url;
            Device.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    if (Instance == null) {
                        Instance = DependencyService.Get<AtomBridge>(DependencyFetchTarget.NewInstance);
                    }
                    await Instance.InitAsync();
                    await Instance.ExecuteScriptAsync(url);
                }
                catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            });
        }

        public async Task InitAsync() {
            if (initialized)
                return;
            string url = AmdUrl;
            try
            {
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
                Engine.ExecuteScript(script, url, 0);
            }
            //catch (   jse) {
            //    System.Diagnostics.Debug.WriteLine($"{jse.ToString()}\r\n{jse.Stack()}");
            //}
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            //});
        }

        public async Task ExecuteScriptAsync(string url) {
            System.Diagnostics.Debug.WriteLine($"Loading url {url}");
            try
            {
                if (url.StartsWith("//")) {
                    url = "https:" + url;
                }
                if (url.StartsWith("/")) {
                    url = (new Uri(Client.BaseAddress,url)).ToString();
                }
                string script = await Client.GetStringAsync(url);
                if (string.IsNullOrWhiteSpace(script)) {
                    throw new Exception("Script is null");
                }
                Execute(script, url);
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Failed to load url {url}\r\n{ex}");
                throw;
            }
        }

        public void LoadContent(JSWrapper elementTarget, string content) {
            try
            {
                Element element = elementTarget.As<Element>();
                if (element is View v)
                {
                    v.LoadFromXaml(content);
                }
                else if (element is Page p) {
                    p.LoadFromXaml(content);
                }
                // (element as View).LoadFromXaml(content);
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex);
                throw;
            }
        }

        public JSValue CreateBusyIndicator() {

            PopupPage pp  = null;

            Device.BeginInvokeOnMainThread(async () => {

                pp = new BusyPopup();

                await WAContext.Current.PushAsync( pp  , true);
            });

            return JSDisposable.From(this.Engine, () => {
                Device.BeginInvokeOnMainThread(async () => {
                    await WAContext.Current.PopAsync(pp, true);
                });
            });

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

        public int SetInterval(JSValue value, int milliSeconds, bool once) {

            System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();

            Device.BeginInvokeOnMainThread(async () => {
                while (!cancellation.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(milliSeconds), cancellation.Token);
                        value.Call();
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

        public void Alert(string message, string title, JSValue success, JSValue error) {
            Device.BeginInvokeOnMainThread(async () => {
                try
                {
                    await Application.Current.MainPage.DisplayAlert(title, message, "Ok");
                    success.Call();
                }
                catch (Exception ex)
                {
                    error.CallJS(null, ex);
                }
            });
        }

        public void Confirm(string message, string title, JSValue success, JSValue error)
        {
            Device.BeginInvokeOnMainThread(async () => {
                try
                {
                    bool result = await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No");
                    success.CallJS(null,result);
                }
                catch (Exception ex)
                {
                    error.CallJS(null, ex.ToString());
                }
            });
        }

        public void Ajax(string url, JSValue ajaxOptions, JSValue success, JSValue failed, JSValue progress) {
            var client = Client;
            var service = AjaxService.Instance;
            service.Invoke(client, url, ajaxOptions, success, failed, progress);
        }

        public void SetImport(JSWrapper elementWrapper, string name, JSValue factory) {
            var element = elementWrapper.As<Element>();
            var d = WAContext.GetImports(element);
            if (d == null) {
                d = new Dictionary<string, Func<Element>>();
                WAContext.SetImports(element, d);
            }
            d[name] = () => {
                var t = WAContext.GetAtomControl(element);
                var jv = factory.Call(t.Wrap(Engine)) as JSValue;
                return JSWrapper.FromKey(jv.GetJSPropertyValue("element").ToString()).As<Element>();
            };
        }

        public void SetTemplate(JSWrapper elementWrapper, string name, JSValue factory) {
            var element = elementWrapper.As<Element>();
            PropertyInfo p = element.GetType().GetProperty(name);
            p.SetValue(element, new DataTemplate(() => {
                try
                {
                    var ac = (factory.Call() as JSValue);
                    var eid = ac.GetJSPropertyValue("element");
                    var e = JSWrapper.FromKey(eid.ToString()).As<View>();
                    return new TemplateView
                    {
                        View = e,
                        SetBindingContext = (obj) =>
                        {
                            if (obj is ManagedArrayItem mi) {
                                ac.SetJSPropertyValue("data", mi.Array.Value[mi.Index]);
                            }
                            else
                            {
                                ac.SetJSPropertyValue("data", obj);
                            }
                        }
                    };
                }
                catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex);
                    throw;
                }
            }));
        }

        public void PopPage(JSWrapper wrapper, JSValue success, JSValue error) {
            Device.BeginInvokeOnMainThread(async () => {
                try {
                    var e = wrapper.As<Page>();
                    await WebAtoms.WAContext.Current.PopAsync(e, true);
                    success.Call(null);
                } catch (Exception ex) {
                    error.CallJS(null, ex.ToString());
                }
            });
        }

        public void PushPage(JSWrapper wrapper, JSValue success, JSValue error) {
            Device.BeginInvokeOnMainThread(async () => {
                try
                {
                    var e = wrapper.As<Page>();
                    await WebAtoms.WAContext.Current.PushAsync(e, true);
                    success.CallJS(null);
                }
                catch (Exception ex) {
                    error.CallJS(null, ex.ToString());
                }
            });
        }

        public void Close(JSWrapper wrapper, JSValue success, JSValue error) {
            Device.BeginInvokeOnMainThread(async () => {
                try {
                    var e = wrapper.As<Element>();
                    await WebAtoms.WAContext.Current.PopAsync(e, true);
                    success.Call();
                } catch (Exception ex) {
                    error.CallJS(null, ex.ToString());
                }
            });
        }

        IEnumerable<System.Reflection.TypeInfo> types;

        List<Element> pending = new List<Element>();

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
            pending.Add(view);
            Device.BeginInvokeOnMainThread(async () => {
                await Task.Delay(30000);
                pending.Remove(view);
            });
            return view;
        }

        public void AttachControl(JSWrapper target, JSManagedValue control) {
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

        public JSValue AddEventHandler(
            JSWrapper elementTarget, 
            string name, JSManagedValue callback, bool? capture)
        {
            Element element = elementTarget.As<Element>();

            // var callback = new JSManagedValue(callback1);

            var e = element.GetType().GetEvent(name);

            if (e == null) {
                var disposable = WAContext.Current.AddEventHandler(element, name, () => {
                    try
                    {
                        callback.Value.CallJS(null);
                    }
                    catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine(ex);
                        // Engine.ThrowJSException(new JSException(Engine, ex.Message));
                        throw;
                    }
                });
                return JSDisposable.From(Engine, () => {
                    disposable.Dispose();
                });
            }

            var pe = new AtomDelegate() { callback = callback };

            var handler = Delegate.CreateDelegate(e.EventHandlerType, pe, AtomDelegate.OnEventMethod);

            e.AddEventHandler(element, handler);

            return JSDisposable.From(Engine, () => {
                e.RemoveEventHandler(element, handler);
                pe.callback = null;
            });
        }
        public JSValue WatchProperty(JSWrapper objTarget, string name, JSValue events, JSValue callback)
        {
            object obj = objTarget.As<object>();

            if (obj is INotifyPropertyChanged element)
            {

                var pinfo = obj.GetProperty(name);

                PropertyChangedEventHandler handler = (s, e) =>
                {
                    if (e.PropertyName == name)
                    {
                        var value = pinfo.GetValue(obj);
                        callback.CallJS( null, value.Wrap(Engine));
                    }
                };

                element.PropertyChanged += handler;
                return JSDisposable.From(Engine, () =>
                {
                    element.PropertyChanged -= handler;
                });
            }

            return JSDisposable.From(Engine, () => { });
        }

        public JSValue AtomParent(JSWrapper target, JSValue climbUp)
        {
            Element element = target.As<Element>();

            bool cu = !((bool)climbUp.IsUndefined) && !((bool)climbUp.IsNull) && (bool)climbUp.ToBool();
            if (cu) {
                element = element.Parent;
                if (element == null)
                    return null;
            }
            do {
                var e = WAContext.GetAtomControl(element);
                if (e != null)
                    return e.Wrap(Engine);
                element = element.Parent;
            } while (cu && element != null);
            return null;
        }

        public JSValue ElementParent(JSWrapper elementTarget) {
            Element element = elementTarget.As<Element>();
            return element.Parent?.Wrap(Engine);
        }

        public JSValue TemplateParent(JSWrapper elementTarget) {
            Element element = elementTarget.As<Element>();
            do {
                var e = WAContext.GetTemplateParent(element);
                if (e != null)
                    return e.Wrap(Engine);
                element = element.Parent;
            } while (element != null);
            return null;
        }

        public void SetRoot(JSWrapper wrapper) {
            WAContext.Current.CurrentPage = wrapper.As<Page>();
        }

        public void VisitDescendents(JSWrapper target, JSValue action)
        {
            Element element = target?.As<Element>();
            if (element == null) {
                throw new ObjectDisposedException("Cannot visit descendents of null");
            }

            var views = element as IViewContainer<View>;
            if (views == null)
                return;
            foreach (var e in views.Children) {
                var ac = WAContext.GetAtomControl(e);
                var child = e.Wrap(Engine);
                var r = action.CallJS(null, 
                    child,
                    (JSValue)ac);
                if ((bool)r.IsUndefined || (bool)r.IsNull || !((bool)r.ToBool()))
                    continue;
                VisitDescendents(JSWrapper.Register(e), action);
            }
        }

        public void Dispose(JSWrapper et) {
            try
            {
                Element e = et.As<Element>();
                WAContext.SetAtomControl(e, null);
                WAContext.SetLogicalParent(e, null);
                WAContext.SetTemplateParent(e, null);

                //if (e is Page page)
                //{
                //    // we need to remove this page if the page is on the stack...
                //    try
                //    {
                //        // WAContext.(page);
                //    }
                //    catch (Exception ex)
                //    {
                //        System.Diagnostics.Debug.WriteLine(ex);
                //    }
                //}
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex);
            }
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
            return value.Wrap(Engine);
            // return null;
        }

        public void SetValue(JSWrapper target, string name, JSManagedValue mValue) {

            Element view = target.As<Element>();

            if (view != null && name.Equals("name", StringComparison.OrdinalIgnoreCase)) {
                WAContext.SetWAName(view, name);
                return;
            }

            var value = mValue.Value;
            bool isNull = (bool)value.IsNull || (bool)value.IsUndefined;

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
            if ((bool)value.IsArray) {
                var old = pv.GetValue(view);
                if (old is IDisposable d) {
                    d.Dispose();
                }
                pv.SetValue(view, new AtomEnumerable(mValue));
                return;
            }

            pt = Nullable.GetUnderlyingType(pt) ?? pt;

            if (value.IsDate) {
                // conver to datetime and set...
                pv.SetValue(view, value.ToDateTime());
                return;
            }

            if (pt.IsValueType)
            {
                object clrValue = null;
                // convert...
                if (value.IsBoolean) {
                    clrValue = value.ToBool();
                } else if (value.IsNumber) {
                    if (pt == typeof(double) || pt == typeof(float) || pt == typeof(short))
                    {
                        clrValue = value.ToDouble();
                    }
                    else {
                        clrValue = value.ToInt32();
                    }
                }
                var v = Convert.ChangeType(clrValue, pt);
                pv.SetValue(view, v);
                return;
            }
            else {

                if (pv.PropertyType == typeof(ICommand))
                {
                    pv.SetValue(view, new AtomCommand(() => {
                        value.Call(null);
                    }));
                }
                else
                {
                    pv.SetValue(view, value.ToObject());
                }
            }
        }

        public void LoadModuleScript(string name, string url, JSValue success, JSValue error) {
            //JSManagedValue jSuccess = new JSManagedValue(success);
            //JSManagedValue jError = new JSManagedValue(error);
            Device.BeginInvokeOnMainThread(async () => {
                try {

                    if (url.StartsWith("//")) {
                        url = "https:" + url;
                    }
                    string script = await Client.GetStringAsync(url);

                    success.CallJS(null, JSClrFunction.From(Engine, (t,args) =>
                    {

                        Execute(script, url);

                        return JSValue.Undefined(Engine);
                    }));
                }
                //catch (JSException ex) {
                //    var msg = $"Failed to load url {url}\r\n{ex.Stack()}\r\n{ex}";
                //    System.Diagnostics.Debug.WriteLine(msg);
                //    error.Call(null, new Java.Lang.Object[] { msg });
                //}
                catch (Exception ex) {
                    var msg = $"Failed to load url {url}\r\n{ex}";
                    System.Diagnostics.Debug.WriteLine(msg);
                    error.CallJS(null, msg);
                }

            });
        }

        public void DisposePage(Element e, bool disposeFromCLR)
        {
            if (disposeFromCLR)
            {
                var ac = (WAContext.GetAtomControl(e) as JSManagedValue);
                if (ac != null)
                {
                    // var func = ac.Value.GetJSPropertyValue("dispose");
                    // func.CallJS(ac.Value);
                    ac.Value.Invoke("dispose");
                }
                return;
            }
        }

        public void Broadcast(Page page, string str, object p = null)
        {
            var ac = (WAContext.GetAtomControl(page) as JSManagedValue);
            var app = ac?.Value?.GetJSPropertyValue("app");
            app.Invoke("broadcast", str.Wrap(Engine), p == null ? JSValue.From(new NSNull(), Engine) : p.Wrap(Engine));
            // var function = app?.GetJSPropertyValue("broadcast");
            // function?.CallJS(app, str, p == null ? null : p.Wrap(Engine));
        }

        public void ShowAlert(string str)
        {
            // this.Engine.ExecuteScript("", null);

            // var ac = (WAContext.GetAtomControl(WAContext.Current.JSPage) as JSValue)?.ToObject();
            // var app = ac?.GetJSPropertyValue("app")?.ToObject();
            // var nav = 
        }

    }
}