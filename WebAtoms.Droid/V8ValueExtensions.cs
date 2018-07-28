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
using Com.Eclipsesource.V8;
using Java.Lang;

namespace WebAtoms
{
    public static class V8ValueExtensions
    {

        public static V8Value AddClrObject(this V8Value v8, IJSObject obj) {

            WeakReference wr = new WeakReference(obj);
            Type type = obj.GetType();
            var name = type.FullName;
            var v = new V8Object(v8.Rutime);

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)) {
                v.RegisterJavaMethod(new JavaCallback((thisParam, parameters) => {
                    if (!wr.IsAlive) {
                        throw new ObjectDisposedException(name);
                    }
                    var r = method.Invoke(wr.Target, parameters.ToArray<object>());
                    if (r != null && r is V8Value rv) {
                        return rv;
                    }
                    return V8.Undefined;
                }), method.Name);
            }

            //foreach (var property in type.GetProperties())
            //{
            //    v.RegisterJavaMethod(new JavaCallback((thisParam, parameters) => {
            //        if (!wr.IsAlive)
            //        {
            //            throw new ObjectDisposedException(name);
            //        }

            //        return V8.Undefined;
            //    }), $"_get_{property.Name}");
            //}

            return v;
        }
        public static V8Array ToV8Array(V8 v8, params object[] value)
        {
            throw new NotImplementedException();
        }

    }

    public class JavaCallback : Java.Lang.Object, IJavaCallback
    {
        Func<V8Object, V8Array, V8Value> func;
        public JavaCallback(Func<V8Object, V8Array, V8Value> func) {
            this.func = func;
        }

        public Java.Lang.Object Invoke(V8Object p0, V8Array p1)
        {
            return this.func(p0, p1);
        }
    }
}