using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace WebAtoms
{
    public class WAContext
    {

        public static WAContext Current = new WAContext();

        public WAContext()
        {

        }

        public Page CurrentPage {
            get => Application.Current.MainPage;
            set => Application.Current.MainPage = value;
        }



        #region AtomControl

        public static object GetAtomControl(BindableObject obj)
        {
            return (object)obj.GetValue(AtomControlProperty);
        }

        public static void SetAtomControl(BindableObject obj, object value)
        {
            obj.SetValue(AtomControlProperty, value);
        }

        // Using a DependencyProperty as the backing store for AtomControl.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty AtomControlProperty =
            BindableProperty.CreateAttached(
                "AtomControl",
                typeof(object),
                typeof(WAContext),
                null);
        #endregion

        #region TemplateParent
        public static object GetTemplateParent(BindableObject obj)
        {
            return (object)obj.GetValue(TemplateParentProperty);
        }

        public static void SetTemplateParent(BindableObject obj, object value)
        {
            obj.SetValue(TemplateParentProperty, value);
        }

        // Using a DependencyProperty as the backing store for AtomControl.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty TemplateParentProperty =
            BindableProperty.CreateAttached(
                "TemplateParent",
                typeof(object),
                typeof(WAContext),
                null);
        #endregion

        #region LogicalParent
        public static object GetLogicalParent(BindableObject obj)
        {
            return (object)obj.GetValue(LogicalParentProperty);
        }

        public static void SetLogicalParent(BindableObject obj, object value)
        {
            obj.SetValue(LogicalParentProperty, value);
        }

        // Using a DependencyProperty as the backing store for AtomControl.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty LogicalParentProperty =
            BindableProperty.CreateAttached(
                "LogicalParent",
                typeof(object),
                typeof(WAContext),
                null);
        #endregion
    }

}
