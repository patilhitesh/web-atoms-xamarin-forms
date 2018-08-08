﻿using Org.Liquidplayer.Javascript;
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
        JSObject disposable;
        JSBaseArray array;

        public AtomEnumerable(JSBaseArray array)
        {
            this.array = array;

            var watch = this.array.GetJSPropertyValue("watch").ToFunction();

            var clrFunc = new JSClrFunction(array.Context, (plist) => {
                CollectionChanged?.Invoke(this, CreateEventArgs(plist));
                return null;
            });

            var retValue = watch.Apply(array, new Java.Lang.Object[] { null, clrFunc });

            this.disposable = retValue.ToObject();
        }

        NotifyCollectionChangedEventArgs CreateEventArgs(object[] plist)
        {
            // var first = plist[0];
            // var array = (first as JSValue).ToJSArray();
            var mode = plist[0].ToString();
            var index = (plist[1] as JSValue).ToNumber().IntValue();

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
    }
}
