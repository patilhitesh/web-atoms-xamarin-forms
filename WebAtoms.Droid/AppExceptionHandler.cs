using Org.Liquidplayer.Javascript;
using System;
using System.Linq;

namespace WebAtoms
{
    public class AppExceptionHandler : Java.Lang.Object, JSContext.IJSExceptionHandler
    {
        Action<JSException> action;
        public AppExceptionHandler(Action<JSException> action)
        {
            this.action = action;
        }

        void JSContext.IJSExceptionHandler.Handle(JSException p0)
        {
            action(p0);
        }
    }
}