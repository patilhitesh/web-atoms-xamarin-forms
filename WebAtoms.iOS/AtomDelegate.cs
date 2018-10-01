
using JavaScriptCore;
using System;
using System.Reflection;

namespace WebAtoms
{
    public class AtomDelegate
    {

        public JSValue callback;

        public void OnEvent(Object sender, Object arg)
        {
            callback.Call(null);
        }

        public static MethodInfo OnEventMethod = typeof(AtomDelegate).GetMethod("OnEvent");

    }

}