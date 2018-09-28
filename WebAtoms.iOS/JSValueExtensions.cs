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
                return null;
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
            if (value == null) {
                return null;
            }
            if (value != null)
            {
                if (value.IsUndefined || value.IsNull)
                    return null;
            }
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
                return value.ToString();
            }
            if (type == typeof(JSFunction) || type.IsSubclassOf(typeof(JSFunction))) {
                return value.ToFunction();
            }
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
            return target.EvaluateScript(script, NSUrl.FromString(script));
        }

        public static void SetJSPropertyValue(this JSValue target, string name, object value) {
            target.SetValueForKey((NSObject)value, (NSString)name);
        }

        public static JSValue GetJSPropertyValue(this JSValue target, string name)
        {
            return target[(NSObject)(NSString)name];
        }

        public static string ToCamelCase(this string text) {
            return Char.ToLower(text[0]) + text.Substring(1);
        }

        public static JSObject AddClrObject(this JSObject target, IJSService value, string name) {
            JSContext context = target.Context;
            var jobj = new JSObject(context);

            context.SetJSPropertyValue("__clr__obj", jobj);


            var methods = value
                .GetType()
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .ToList();

            var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

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
                    System.Diagnostics.Debug.WriteLine($"Failed to execute {item.Name}");
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    throw;
                }
            }

            //foreach (var property in properties) {
            //    try
            //    {
            //        JSClrFunction getFunction = new JSClrFunction(context, (x) =>
            //        {
            //            // map parameters
            //            return property.GetValue(value);
            //        });

            //        JSClrFunction setFunction = new JSClrFunction(context, (x) =>
            //        {
            //            // map parameters
            //            property.SetValue(value, x[0] is JSValue jv ? jv.ToType(property.PropertyType) : null);
            //            return null;
            //        });


            //        jobj.InvokeProperty($"__{property.Name}", setFunction);
            //        // jobj.InvokeProperty($"get__{property.Name}", getFunction);
            //        var r = context.ExecuteScript($"__paramArrayToArrayParam(__clr__obj,__clr__obj.__{property.Name})", "AddClrObject", 0);
            //        // jobj.InvokeProperty(item.Name.ToCamelCase(), (Java.Lang.Object)r);

            //        var obj = target.Context.GetJSPropertyValue("Object").ToObject();
            //        var def = obj.GetJSPropertyValue("defineProperty").ToFunction();
            //        var pconf = new JSObject(target.Context);
            //        pconf.SetJSPropertyValue("set", setFunction);
            //        pconf.SetJSPropertyValue("get", (Java.Lang.Object)r);
            //        pconf.SetJSPropertyValue("enumerable", true);
            //        pconf.SetJSPropertyValue("configurable", true);
            //        def.Call(obj, jobj, property.Name, pconf);

                    
            //    }
            //    catch (Exception ex)
            //    {
            //        System.Diagnostics.Debug.WriteLine($"Failed to set/get {property.Name}");
            //        System.Diagnostics.Debug.WriteLine(ex.ToString());
            //        throw;
            //    }

            //}

            target.SetJSPropertyValue(name, jobj);

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