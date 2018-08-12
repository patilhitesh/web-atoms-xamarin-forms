using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace WebAtoms
{

    public class AtomCommand : ICommand
    {
        readonly Action action;
        public AtomCommand(Action action)
        {
            this.action = action;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            this.action();
        }
    }

    public class WAContext
    {

        public static WAContext Current = new WAContext();

        public WAContext()
        {

        }

        private void BindDisposer(Page page) {

            if (WAContext.GetDisappearHandler(page) != null)
                return;

            EventHandler handler = (s, e) =>
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100);
                    if (Navigation.NavigationStack.Any(x => x == page))
                    {
                        AtomBridge.Instance.DisposePage(page, true);
                        page.Disappearing -= WAContext.GetDisappearHandler(page);
                        WAContext.SetDisappearHandler(page, null);
                    }
                });
            };
            WAContext.SetDisappearHandler(page, handler);
            page.Disappearing += handler;
        }

        public Page CurrentPage {
            get => Application.Current.MainPage;
            set
            {
                if (value != null)
                {
                    BindDisposer(value);
                }
                Application.Current.MainPage = value;
            }
        }

        public async Task PushAsync(Page e, bool value)
        {
            BindDisposer(e);
            if (!(e is NavigationPage)) {
                e = new NavigationPage(e);
            }
            await Navigation.PushAsync(e, value);
        }

        public INavigation Navigation {
            get {
                var page = Application.Current.MainPage;
                if (page is MasterDetailPage mdp)
                    return mdp.Detail.Navigation;
                return page.Navigation;
            }
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

        #region JSRefKey
        public static string GetJSRefKey(BindableObject obj)
        {
            return (string)obj.GetValue(JSRefKeyProperty);
        }

        public static void SetJSRefKey(BindableObject obj, string value)
        {
            obj.SetValue(JSRefKeyProperty, value);
        }

        // Using a DependencyProperty as the backing store for AtomControl.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty JSRefKeyProperty =
            BindableProperty.CreateAttached(
                "JSRefKey",
                typeof(string),
                typeof(WAContext),
                null);
        #endregion

        #region Imports
        public static Dictionary<string,Func<Element>> GetImports(BindableObject obj)
        {
            return (Dictionary<string, Func<Element>>)obj.GetValue(ImportsProperty);
        }

        public static void SetImports(BindableObject obj, Dictionary<string, Func<Element>> value)
        {
            obj.SetValue(ImportsProperty, value);
        }
        // Using a DependencyProperty as the backing store for AtomControl.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty ImportsProperty =
            BindableProperty.CreateAttached(
                "Imports",
                typeof(Dictionary<string, Func<Element>>),
                typeof(WAContext),
                null);
        #endregion

        #region DisapperHandler
        public static EventHandler GetDisappearHandler(BindableObject obj)
        {
            return (EventHandler)obj.GetValue(DisappearHandlerProperty);
        }

        public static void SetDisappearHandler(BindableObject obj, EventHandler value)
        {
            obj.SetValue(DisappearHandlerProperty, value);
        }
        // Using a DependencyProperty as the backing store for AtomControl.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty DisappearHandlerProperty =
            BindableProperty.CreateAttached(
                "DisappearHandler",
                typeof(EventHandler),
                typeof(WAContext),
                null);
        #endregion
        public IDisposable AddEventHandler(Element element, string name, Action action)
        {
            View view = element as View;
            IGestureRecognizer recognizer = null;
            switch (name.ToLower()) {
                case "tapgesture":
                    recognizer = new TapGestureRecognizer
                    {
                        Command = new AtomCommand(() => {
                            action();
                        })
                    };
                    break;
                case "pangesture":
                    break;
                case "pinchgesture":
                    break;
            }
            if (recognizer != null) {
                view.GestureRecognizers.Add(recognizer);
                return new AtomDisposable(() => {
                    view.GestureRecognizers.Remove(recognizer);
                });
            }
            throw new NotImplementedException($"No gesture recognizer found for {name}");
        }
    }

    public class TemplateView: ViewCell {

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

    [ContentProperty("Type")]
    public class JSObjectCreator : IMarkupExtension<Element>
    {

        public string Type { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            var root = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;

            var d = WAContext.GetImports(root.RootObject as Element);

            if (d.TryGetValue(Type, out Func<Element> f)) {
                return f();
            }

            throw new NotImplementedException();
        }

        Element IMarkupExtension<Element>.ProvideValue(IServiceProvider serviceProvider)
        {
            return (Element)(this as IMarkupExtension).ProvideValue(serviceProvider);
        }
    }

}
