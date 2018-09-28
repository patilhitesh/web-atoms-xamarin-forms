using JavaScriptCore;
using Org.Liquidplayer.Javascript;
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
        JSValue disposable;
        JSValue array;

        public string Key { get; set; }
        
        public AtomEnumerable(JSValue array[])
        {
            this.array = array;

            var watch = this.array.GetJSPropertyValue("watch").ToFunction();

            var clrFunc = new JSClrFunction(array.Context, (plist) => {
                CollectionChanged?.Invoke(this, CreateEventArgs(plist));
                return null;
            });

            var retValue = watch.Call(array, clrFunc, true );

            this.disposable = retValue.ToObject();
        }

        NotifyCollectionChangedEventArgs CreateEventArgs(object[] plist)
        {
            // var first = plist[0];
            // var array = (first as JSValue).ToJSArray();
            var mode = plist[1].ToString();
            var index = (plist[2] as JSValue).ToNumber().IntValue();

            switch (mode) {
                case "refresh":
                    return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                case "remove":
                    return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, plist[2], index);
                case "add":
                    return new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, plist[2], index);
            }

            throw new NotImplementedException();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Dispose()
        {
            (disposable?.GetJSPropertyValue("dispose") as JSFunction)
                ?.Call(array);
        }

        public IEnumerator GetEnumerator()
        {
            for (var i = 0; i < array.Size(); i++) {
                yield return array.Get(i);
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
}
