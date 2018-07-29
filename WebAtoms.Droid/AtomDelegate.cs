
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
            callback.Call((JSObject )arg.Wrap(callback.Context));
        }

        public static MethodInfo OnEventMethod = typeof(AtomDelegate).GetMethod("OnEvent");

    }

}