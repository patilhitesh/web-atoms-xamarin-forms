using JavaScriptCore;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace WebAtoms
{
    public class JSGroupList : List<IGrouping<string, object>>, System.Collections.Specialized.INotifyCollectionChanged
    {

        System.Collections.IEnumerable list;
        readonly string field;
        readonly string itemsField;

        public JSGroupList(System.Collections.IEnumerable list, string field, string itemsField)
        {
            this.itemsField = itemsField;
            this.field = field;
            this.list = list.OfType<object>();

            if (list is System.Collections.Specialized.INotifyCollectionChanged icn)
            {
                icn.CollectionChanged += Icn_CollectionChanged;
            }

            ResetList();
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void Icn_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            ResetList(true);
        }

        void ResetList(bool notify = false)
        {
            this.Clear();

            if (itemsField != null)
            {
                foreach (var x in list.OfType<ManagedArrayItem>())
                {
                    var jobj = x.Array.Value[x.Index];
                    //var item = jobj.
                    string group = jobj.GetJSPropertyValue(field).ToString();
                    var items = new JSManagedValue( jobj.GetJSPropertyValue(itemsField) );
                    var children = new AtomEnumerable(items);
                    if (!children.Any())
                        continue;
                    children.Key = group;
                    this.Add(children);
                }
            }
            else
            {
                this.AddRange(list.OfType<object>().GroupBy(x => (x as JSValue).GetJSPropertyValue(field)?.ToString()));
            }
            if (notify)
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }
}