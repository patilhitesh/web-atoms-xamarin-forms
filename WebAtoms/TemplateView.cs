using System;
using System.Linq;
using Xamarin.Forms;

namespace WebAtoms
{
    public class TemplateView : ViewCell
    {

        public TemplateView()
        {

        }

        public Action<object> SetBindingContext { get; set; }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            this.SetBindingContext?.Invoke(this.BindingContext);
        }

    }
}
