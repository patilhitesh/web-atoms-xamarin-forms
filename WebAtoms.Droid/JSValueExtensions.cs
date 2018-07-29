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
using Org.Liquidplayer.Javascript;

namespace WebAtoms
{

    public class JSWrapper : JSValue {

        public JSWrapper(JSContext c, Object target): base(c, (Java.Lang.Object) target)
        {
            this.Target = target;
        }

        public object Target { get; }
    }

    public static class JSValueExtensions
    {

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


            foreach (var item in value.GetType().GetMethods())
            {
                JSClrFunction clrFunction = new JSClrFunction(target.Context, (x) => {

                    return item.Invoke(value, x);
                });

                jobj.InvokeProperty(item.Name, clrFunction);
            }

            return jobj;

        }

    }

    public class JSClrFunction : JSFunction {

        private readonly Func<object[], object> runFunction;

        public JSClrFunction(JSContext context, Func<object[], object> run)
            : base(context, 
                "Run", 
                Java.Lang.Class.FromType(typeof(JSClrFunction)))
        {
            this.runFunction = run;
        }

        public object Run(params object[] args) {
            return runFunction(args);
        }

    }
}