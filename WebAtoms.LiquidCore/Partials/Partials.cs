using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Org.Liquidplayer.Javascript
{

    public partial class JSBaseArray {

        public partial class ArrayIterator {

            public void Add(Java.Lang.Object arg) {
            }

            public Java.Lang.Object Next() {
                throw new NotImplementedException();
            }

            public Java.Lang.Object Previous() {
                throw new NotImplementedException();
            }

            public void Set(Java.Lang.Object obj) {
            }

        }

    }

    public partial class JSTypedArray {

        public override IList SubList(int fromIndex, int toIndex)
        {
            throw new NotImplementedException();
        }

    }

    public partial class JSIterator
    {

        Java.Lang.Object Java.Util.IIterator.Next() {
            throw new NotImplementedException();
        }

    }

    public partial class JSObjectPropertiesMap {

        public System.Collections.ICollection EntrySet() {
            return this.EntrySetGenerated().ToList();
        }

    }

}