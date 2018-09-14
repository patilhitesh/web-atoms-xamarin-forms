using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Xamarin.Forms;

namespace WebAtoms
{
    public class AtomListView : ListView
    {



        #region Property GroupHeaderField

        /// <summary>
        /// Bindable Property GroupHeaderField
        /// </summary>
        public static readonly BindableProperty GroupHeaderFieldProperty = BindableProperty.Create(
          nameof(GroupHeaderField),
          typeof(string),
          typeof(AtomListView),
          null,
          BindingMode.OneWay,
          // validate value delegate
          // (sender,value) => true
          null,
          // property changed, delegate
          (sender, oldValue, newValue) => ((AtomListView)sender).OnGroupHeaderFieldChanged(oldValue, newValue),
          // property changing delegate
          // (sender,oldValue,newValue) => {}
          null,
          // coerce value delegate 
          // (sender,value) => value
          null,
          // create default value delegate
          // () => Default(T)
          null
        );


        /// <summary>
        /// On GroupHeaderField changed
        /// </summary>
        /// <param name="oldValue">Old Value</param>
        /// <param name="newValue">New Value</param>
        protected virtual void OnGroupHeaderFieldChanged(object oldValue, object newValue)
        {
            ResetList();
        }


        /// <summary>
        /// Property GroupHeaderField
        /// </summary>
        public string GroupHeaderField
        {
            get
            {
                return (string)GetValue(GroupHeaderFieldProperty);
            }
            set
            {
                SetValue(GroupHeaderFieldProperty, value);
            }
        }
        #endregion

        #region Property GroupItemsField

        /// <summary>
        /// Bindable Property GroupItemsField
        /// </summary>
        public static readonly BindableProperty GroupItemsFieldProperty = BindableProperty.Create(
          nameof(GroupItemsField),
          typeof(string),
          typeof(AtomListView),
          null,
          BindingMode.OneWay,
          // validate value delegate
          // (sender,value) => true
          null,
          // property changed, delegate
          (sender, oldValue, newValue) => ((AtomListView)sender).OnGroupItemsFieldChanged(oldValue, newValue),
          // property changing delegate
          // (sender,oldValue,newValue) => {}
          null,
          // coerce value delegate 
          // (sender,value) => value
          null,
          // create default value delegate
          // () => Default(T)
          null
        );


        /// <summary>
        /// On GroupItemsField changed
        /// </summary>
        /// <param name="oldValue">Old Value</param>
        /// <param name="newValue">New Value</param>
        protected virtual void OnGroupItemsFieldChanged(object oldValue, object newValue)
        {
            ResetList();
        }


        /// <summary>
        /// Property GroupItemsField
        /// </summary>
        public string GroupItemsField
        {
            get
            {
                return (string)GetValue(GroupItemsFieldProperty);
            }
            set
            {
                SetValue(GroupItemsFieldProperty, value);
            }
        }
        #endregion



        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);

            if (GroupHeaderField == null && GroupItemsField == null)
                return;

            if (propertyName == nameof(ItemsSource))
            {
                ResetList();
            }

        }

        private void ResetList()
        {
            var items = this.ItemsSource;
            if (items == null || items is JSGroupList)
                return;

            this.ItemsSource = null;

            this.IsGroupingEnabled = true;
            this.GroupDisplayBinding = new Binding("Key");

            this.ItemsSource = new JSGroupList(items, GroupHeaderField, GroupItemsField);
        }



    }
}
