using Jint.Native;
using System;
using System.Reflection;

namespace WebAtoms
{
    public class AtomDelegate
    {

        public JsValue callback;

        public void OnEvent(Object sender, Object arg)
        {
            callback.Invoke(JsValue.FromObject(AtomBridge.Instance.engine, arg));
        }

        public static MethodInfo OnEventMethod = typeof(AtomDelegate).GetMethod("OnEvent");

    }

}