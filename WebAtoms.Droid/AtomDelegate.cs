
using Org.Liquidplayer.Javascript;
using System;
using System.Reflection;

namespace WebAtoms
{
    public class AtomDelegate
    {

        public JSFunction callback;

        public void OnEvent(Object sender, Object arg)
        {
            callback.Call(null);
        }

        public static MethodInfo OnEventMethod = typeof(AtomDelegate).GetMethod("OnEvent");

    }

}