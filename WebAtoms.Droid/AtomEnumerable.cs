using Jint.Native;
using Jint.Native.Array;
using Jint.Runtime.Interop;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace WebAtoms
{
    public class AtomEnumerable: 
        IEnumerable, 
        INotifyPropertyChanged, 
        INotifyCollectionChanged, 
        IDisposable
    {
        readonly ArrayInstance array;
        JsValue disposable;

        public AtomEnumerable(ArrayInstance array)
        {
            this.array = array;

            var watch = this.array.Get("watch");

            var clrFunc = new ClrFunctionInstance(array.Engine, (_this, plist) => {
                CollectionChanged?.Invoke(this, CreateEventArgs(plist));
                return JsValue.Undefined;
            });

            this.disposable = watch.Invoke(clrFunc);
        }

        NotifyCollectionChangedEventArgs CreateEventArgs(JsValue[] plist)
        {
            var mode = plist[0].AsString();
            var index = (int)plist[1].AsNumber();

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
            disposable?.Invoke();
        }

        public IEnumerator GetEnumerator()
        {
            for (var i = 0; i < array.GetLength(); i++) {
                yield return array.Get($"{i}").AsObject();
            }    
        }
    }
}
