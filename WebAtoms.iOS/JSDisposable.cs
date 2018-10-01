using Foundation;
using JavaScriptCore;
using System;

namespace WebAtoms
{
    public class JSDisposable
    {
        public static JSValue From(JSContext engine, Action action)
        {
            var d = JSValue.CreateObject(engine);
            d[(NSString)"dispose"] = JSClrFunction.From(engine, (t, v) =>
            {
                action();
                return null;
            });
            return d;
        }
    }
}
