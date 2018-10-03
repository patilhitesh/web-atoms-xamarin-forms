
using JavaScriptCore;
using System;
using System.Reflection;

namespace WebAtoms
{
    public class AtomDelegate
    {

        public JSManagedValue callback;

        public void OnEvent(Object sender, Object arg)
        {
            callback.Value.CallJS(null);
        }

        public static MethodInfo OnEventMethod = typeof(AtomDelegate).GetMethod("OnEvent");

    }

}