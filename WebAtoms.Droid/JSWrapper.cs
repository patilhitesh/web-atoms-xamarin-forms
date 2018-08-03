using Org.Liquidplayer.Javascript;
using System;
using System.Linq;

namespace WebAtoms
{
    public class JSWrapper : JSValue
    {

        public JSWrapper(JSContext c, Object target) : base(c)
        {
            this.Target = target;
        }

        public object Target { get; }

        public override JSObject ToObject()
        {
            return base.ToObject();
        }

        internal T As<T>()
        {
            return (T)Target;
        }
    }
}