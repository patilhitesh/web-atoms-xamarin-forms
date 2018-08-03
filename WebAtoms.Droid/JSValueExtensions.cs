using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public static object ToType(this JSValue value, Type type) {
            if (type == typeof(int))
            {
                if (value == null)
                    return 0;
                return value.ToNumber().IntValue();
            }
            if (type == typeof(short))
            {
                if (value == null)
                    return 0;
                return value.ToNumber().ShortValue();
            }
            if (type == typeof(float))
            {
                if (value == null)
                    return 0;
                return value.ToNumber().FloatValue();
            }
            if (type == typeof(double))
            {
                if (value == null)
                    return 0;
                return value.ToNumber().DoubleValue();
            }
            if (type == typeof(bool))
            {
                if (value == null)
                    return 0;
                return (bool)value.ToBoolean();
            }
            if (type == typeof(DateTime)) {
                if (value == null)
                    return DateTime.MinValue;
                return (value as JSDate).ToDateTime();
            }
            if (value == null) {
                return null;
            }
            if (type == typeof(string)) {
                return value.ToString();
            }
            if (type == typeof(JSFunction) || type.IsSubclassOf(typeof(JSFunction))) {
                return value.ToFunction();
            }
            if (type == typeof(JSWrapper)) {
                return value.ToObject();
            }
            if (type == typeof(JSValue) || type.IsSubclassOf(typeof(JSValue)))
                return value;

            if (value is JSArray j) {
                // type is IList...
                var list = Activator.CreateInstance(type) as System.Collections.IList;
                for (int i = 0; i < j.Size(); i++)
                {
                    Type itemType = type.GetGenericArguments()[0];
                    list[i] = (j.Get(i) as JSValue).ToType(itemType);
                }
                return list;
            }
            return null;
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

        public static string ToCamelCase(this string text) {
            return Char.ToLower(text[0]) + text.Substring(1);
        }

        public static JSValue AddClrObject(this JSObject target, IJSService value) {
            JSContext context = target.Context;
            var jobj = new JSObject(context);

            context.SetJSPropertyValue("__clr__obj", jobj);


            var methods = value
                .GetType()
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .ToList();
            foreach (var item in methods)
            {
                try
                {
                    JSClrFunction clrFunction = new JSClrFunction(context, (x) =>
                    {
                        // map parameters
                        return item.Invoke(value, 
                            item
                                .GetParameters()
                                .Select( (p,i) => (x[i] is JSValue jv) ? jv.ToType(p.ParameterType) : null  ).ToArray());
                    });

                    jobj.InvokeProperty($"__{item.Name}", clrFunction);
                    var r = context.ExecuteScript($"__paramArrayToArrayParam(__clr__obj,__clr__obj.__{item.Name})", "AddClrObject", 0);
                    jobj.InvokeProperty(item.Name.ToCamelCase(), (Java.Lang.Object)r);
                }
                catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
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

        [Export("runAction")]
        public Java.Lang.Object RunAction(JSArray args) {
            try
            {
                return runFunction(args?.ToArray())?.Wrap(this.Context);
            }
            catch (Exception ex) {
                Context.ThrowJSException(new JSException(Context, ex.ToString()));
                return null;
            }
        }

    }
}