using Org.Liquidplayer.Javascript;
using System;

namespace WebAtoms
{
    public class AtomDisposable : IDisposable
    {
        readonly Action action;

        public AtomDisposable(Action action)
        {
            this.action = action;
        }

        public void Dispose()
        {
            action?.Invoke();
        }
    }

    public class JSDisposable : JSObject {

        public JSDisposable(JSContext context, Action action): base(context)
        {
            JSClrFunction a = new JSClrFunction(context, (aa) => {
                action();
                return new JSValue(context);
            });

            this.SetJSPropertyValue("dispose", a);
        }


    }

}