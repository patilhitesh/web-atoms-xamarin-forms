using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Foundation;
using JavaScriptCore;
using Xamarin.Forms;

namespace WebAtoms
{

    public static class JSValueExtensions
    {

        public static JSValue Wrap(this object value, JSContext context) {
            if (value == null)
                return JSValue.From(new NSNull(),  context);
            if (value is JSValue jv)
                return jv;
            if (value is string s)
                return JSValue.From(s, context);
            if (value is int i)
                return JSValue.From(i, context);
            if (value is float f)
                return JSValue.From(f, context);
            if (value is double d)
                return JSValue.From(d, context);
            if (value is decimal dec)
                return JSValue.From((double)dec, context);
            if (value is bool b)
                return JSValue.From(b, context);
            if (value is DateTime dt)
                return JSValue.From((NSDate)dt, context);
            if (value is JSWrapper w) {
                return JSValue.From(w.Key, context);
            }
            return JSValue.From(JSWrapper.Register(value).Key, context);
        }

        public static object ToType(this JSValue value, Type type) {

            Type nt = Nullable.GetUnderlyingType(type);
            if (nt != null) {
                type = nt;
            }
            if (type == typeof(JSValue))
                return value;
            if (value == null) {
                return null;
            }
            //if (value != null)
            //{
            //    if (value.IsNull)
            //        return null;
            //}
            if (type == typeof(int))
            {
                if (value == null)
                    return 0;
                return value.ToInt32();
            }
            if (type == typeof(short))
            {
                if (value == null)
                    return 0;
                return (short)value.ToInt32();
            }
            if (type == typeof(float))
            {
                if (value == null)
                    return 0;
                return (float)value.ToDouble();
            }
            if (type == typeof(double))
            {
                if (value == null)
                    return 0;
                return value.ToDouble();
            }
            if (type == typeof(decimal))
            {
                if (value == null)
                    return 0;
                return (decimal)value.ToDouble();
            }
            if (type == typeof(bool))
            {
                if (value == null)
                    return 0;
                return (bool)value.ToBool();
            }
            if (type == typeof(DateTime)) {
                if (value == null)
                    return DateTime.MinValue;
                return value.ToDate();
            }
            if (type == typeof(string)) {
                if (value.IsString)
                {
                    return (string)(object)value;
                }
                return value.ToString();
            }
            //if (type == typeof(JSFunction) || type.IsSubclassOf(typeof(JSFunction))) {
            //    return value.ToFunction();
            //}
            if (type == typeof(JSWrapper)) {
                return JSWrapper.FromKey(value.ToString());
            }

            if (value.IsArray) {

                if (type == typeof(System.Collections.IEnumerable)) {
                    return new AtomEnumerable(value);
                }

                var j = value.ToArray();

                // type is IList...
                var list = Activator.CreateInstance(type) as System.Collections.IList;
                for (int i = 0; i < j.Length; i++)
                {
                    Type itemType = type.GetGenericArguments()[0];
                    list[i] = j[i].ToType(itemType);
                }
                return list;
            }
            if (type == typeof(JSValue)) {
                return value;
            }
            if (type == typeof(JSValue) || type.IsSubclassOf(typeof(JSValue)))
                return value;
            return null;
        }

        public static JSValue ToJSDate(this DateTime dateTime, JSContext context) {
            // var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            // long time = dateTime.Ticks - epoch.Ticks;
            // long ms = (long)(time / TimeSpan.TicksPerMillisecond);
            return JSValue.From((NSDate)dateTime, context);
        }

        public static JSValue Invoke(this JSValue value, object thisValue, params object[] args) {
            return value.Call(args.Select(x => JSValue.From((NSObject)x, value.Context)).ToArray());
        }

        public static DateTime ToDateTime(this JSValue date) {
            return (DateTime)date.ToDate();
            //return new DateTime(
            //    (int)date.FullYear,
            //    (int)date.Month,
            //    (int)date.Day,
            //    (int)date.Hours,
            //    (int)date.Minutes,
            //    (int)date.Seconds,
            //    (int)date.Milliseconds,
            //    DateTimeKind.Local);
        }

        public static object ExecuteScript(this JSContext target, string script, string source, int line = 0) {
            // return target.EvaluateScript(script, source, line);
            return target.EvaluateScript(script, NSUrl.FromString(source ?? "a.js"));
        }

        public static void SetJSPropertyValue(this JSValue target, string name, object value) {
            //target.SetValueForKey((NSObject)value, (NSString)name);
            target[(NSString)name] = value.Wrap(target.Context);
        }

        public static JSValue GetJSPropertyValue(this JSValue target, string name)
        {
            return target[(NSObject)(NSString)name];
        }

        public static void SetJSPropertyValue(this JSContext target, string name, object value)
        {
            target[(NSString)name] = value.Wrap(target);
        }

        public static JSValue GetJSPropertyValue(this JSContext target, string name)
        {
            return target[(NSObject)(NSString)name];
        }

        public static string ToCamelCase(this string text) {
            return Char.ToLower(text[0]) + text.Substring(1);
        }

        public static JSValue AddClrObject(this JSValue target, IJSService value, string name)
        {
            JSContext context = target.Context;
            JSValue jobj = CreateNewObject(target.Context, value);

            target.SetJSPropertyValue(name, jobj);

            return jobj;

        }

        public static JSValue AddClrObject(this JSContext context, IJSService value, string name)
        {
            JSValue jobj = CreateNewObject(context, value);

            context.SetJSPropertyValue(name, jobj);

            return jobj;

        }

        private static JSValue CreateNewObject(JSContext context, IJSService value)
        {
            var jobj = JSValue.CreateObject(context);

            //context.SetJSPropertyValue("__clr__obj", jobj);
            context[(NSString)"clrobj"] = jobj;


            var methods = value
                .GetType()
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .ToList();

            var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var item in methods)
            {
                try
                {
                    JSValue clrFunction = JSClrFunction.From(context, (t, x) =>
                    {

                        var prs = item.GetParameters();
                        var ps = prs.Select((p, i) => x[i].ToType(p.ParameterType));
                        var pa = ps.ToArray();


                        // map parameters
                        return item.Invoke(value, pa)
                                .Wrap(context);
                    });

                    jobj[(NSString)item.Name.ToCamelCase()] = clrFunction;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to execute {item.Name}");
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    throw;
                }
            }

            return jobj;
        }
    }

    [Protocol]
    public interface IJSClrFunction : IJSExport {

        [Export("callMe:a:")]
        JSValue Execute(JSValue thisValue, JSValue parameters);

    }

    public class JSClrFunction : NSObject, IJSClrFunction
    {

        readonly Func<JSValue, JSValue[], JSValue> func;
        private JSClrFunction(Func<JSValue,JSValue[],JSValue> func)
        {
            this.func = func;
        }

        public JSValue Execute(JSValue thisValue, JSValue parameters)
        {
            JSManagedValue v = new JSManagedValue(parameters);
            return func(thisValue, v.Value[(NSString)"args"].ToArray());
        }

        public static JSValue From(JSContext context, Func<JSValue, JSValue[], JSValue> func) {
            var f = new JSClrFunction(func);
            context[(NSString)"_____clrobj"] = JSValue.From(f, context);
            return context.ExecuteScript(code, "FROM.js") as JSValue;
        }

        public static string code = "(function(src) { " +
            "return function () { " +
            "var args = { args:[] };" +
                "for(var i=0;i<arguments.length;i++) {" +
                    " args.args[i] = arguments[i]; " +
                "}" +
                "return src.callMeA(this, args); }; " +
            "})(_____clrobj)";
    }
}