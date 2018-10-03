using JavaScriptCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace WebAtoms
{
    public class AtomEnumerable: 
        IEnumerable, 
        INotifyPropertyChanged, 
        INotifyCollectionChanged, 
        IDisposable,
        IGrouping<string,object>
    {
        JSManagedValue disposable;
        JSManagedValue array;

        public string Key { get; set; }
        
        public AtomEnumerable(JSManagedValue array)
        {
            this.array = array;

            // var watch = this.array.GetJSPropertyValue("watch");

            JSContext context = array.Value.Context;
            var clrFunc = JSClrFunction.From(context, (t,plist) => {
                CollectionChanged?.Invoke(this, CreateEventArgs(plist[0]));
                return null;
            });

            // var retValue = watch.Call(array, clrFunc, JSValue.From(true, context));

            var retVal = this.array.Value.Invoke("watch", clrFunc, JSValue.From(true, context) );
            this.disposable = new JSManagedValue(retVal);
        }

        NotifyCollectionChangedEventArgs CreateEventArgs(JSManagedValue args)
        {
            // var first = plist[0];
            // var array = (first as JSValue).ToJSArray();
            var plist = args.Value.ToJSValueArray();
            var mode = plist[1].Value.ToString();
            var index = plist[2].Value.ToInt32();
            var item = plist[3].Value;

            switch (mode) {
                case "refresh":
                    return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                case "remove":
                    return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index);
                case "add":
                    return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index);
            }

            throw new NotImplementedException();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Dispose()
        {
            disposable.Value.Invoke("dispose");
            // (disposable?.GetJSPropertyValue("dispose") as JSValue)
            //    ?.Call(array);
        }

        public IEnumerator GetEnumerator()
        {
            var v = array.Value;
            var a = v.GetJSPropertyValue("length").ToInt32();
            for (var i = 0; i < a; i++) {
                yield return new ManagedArrayItem { Array = array, Index = (uint)i };
            }    
        }

        IEnumerator<object> IEnumerable<object>.GetEnumerator()
        {
            return (IEnumerator<object>)this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class ManagedArrayItem {

        public JSManagedValue Array;
        public uint Index;

    }
}
