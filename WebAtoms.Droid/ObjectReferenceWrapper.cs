﻿using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using System.Collections.Generic;

namespace WebAtoms
{
    public class ObjectReferenceWrapper : ObjectInstance, IObjectWrapper
    {

        public ObjectReferenceWrapper(Engine engine) : base(engine)
        {

        }


        public object Target { get; set; }

        public override Types Type => Types.Symbol;

        public override bool Equals(JsValue other)
        {
            if (other is ObjectReferenceWrapper orw)
            {
                return orw.Target == Target;
            }
            return false;
        }

        public override IEnumerable<KeyValuePair<string, IPropertyDescriptor>> GetOwnProperties()
        {
            yield break;
        }

        public override IPropertyDescriptor GetOwnProperty(string propertyName)
        {
            return null;
        }

        public override object ToObject()
        {
            return Target;
        }

    }
}