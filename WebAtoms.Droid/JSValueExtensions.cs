using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Interop;
using Org.Liquidplayer.Javascript;
using Xamarin.Forms;

namespace WebAtoms
{

    public class JSWrapper : JSValue {

        public JSWrapper(JSContext c, Object target): base(c, (Java.Lang.Object) target)
        {
            this.Target = target;
        }

        public object Target { get; }

        internal T As<T>()
        {
            return (T)Target;
        }
    }

    public static class JSValueExtensions
    {

        public static JSValue Wrap(this object value, JSContext context) {
            if (value == null)
                return null;
            if (value is JSValue jv)
                return jv;
            if (value is string s)
                return new JSValue(context, s);
            if (value is int i)
                return new JSValue(context, i);
            if (value is float f)
                return new JSValue(context, f);
            if (value is double d)
                return new JSValue(context, d);
            if (value is decimal dec)
                return new JSValue(context, (double)dec);
            if (value is bool b)
                return new JSValue(context, b);
            if (value is DateTime dt)
                return dt.ToJSDate(context);
            return new JSWrapper(context, value);
        }

        public static JSDate ToJSDate(this DateTime dateTime, JSContext context) {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long time = dateTime.Ticks - epoch.Ticks;
            long ms = (long)(time / TimeSpan.TicksPerMillisecond);
            return new JSDate(context, (Java.Lang.Long)ms);
        }

        public static DateTime ToDateTime(this JSDate date) {
            return new DateTime(
                (int)date.FullYear,
                (int)date.Month,
                (int)date.Day,
                (int)date.Hours,
                (int)date.Minutes,
                (int)date.Seconds,
                (int)date.Milliseconds,
                DateTimeKind.Local);
        }

        public static object ExecuteScript(this JSContext target, string script, string source, int line = 0) {
            return target.EvaluateScript(script, source, line);
        }

        public static void SetJSPropertyValue(this JSObject target, string name, Java.Lang.Object value) {
            target.InvokeProperty(name, value);
        }

        public static JSValue GetJSPropertyValue(this JSObject target, string name)
        {
            return target.InvokeProperty(name);
        }

        public static JSValue AddClrObject(this JSObject target, IJSService value) {

            var jobj = new JSObject(target.Context);


            foreach (var item in value.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                try
                {
                    JSClrFunction clrFunction = new JSClrFunction(target.Context, (x) =>
                    {

                        return item.Invoke(value, x);
                    });

                    jobj.InvokeProperty(item.Name, clrFunction);
                }
                catch (Exception ex) {
                    throw;
                }
            }

            return jobj;

        }

    }

    public class JSClrFunction : JSFunction {

        private readonly Func<object[], object> runFunction;

        public JSClrFunction(JSContext context, Func<object[], object> run)
            : base(context, 
                GetMethodName(),
                Java.Lang.Class.FromType(typeof(JSClrFunction)))
        {
            this.runFunction = run;
        }

        private static Java.Lang.Reflect.Method GetMethodName()
        {
            var c = Java.Lang.Class.FromType(typeof(JSClrFunction));
            var ms = c.GetMethods();
            var mns = ms.Select(x => x.Name).ToList();
            var m = ms.FirstOrDefault(x => x.Name.Equals("RunAction", StringComparison.OrdinalIgnoreCase));
            return m;
        }

        //[Register("runAction", "([Ljava/lang/object;)Ljava/lang/Object;", "GetAddHandler")]
        [Export("runAction")]
        public Java.Lang.Object RunAction(JSArray args) {
            return runFunction(args.ToArray())?.Wrap(this.Context);
        }

    }
}