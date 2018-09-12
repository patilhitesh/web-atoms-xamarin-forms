using Rg.Plugins.Popup.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using Rg.Plugins.Popup.Extensions;
using System.Runtime.CompilerServices;

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

                    var id = WAContext.GetWAName(page);

                    AtomBridge.Instance.Broadcast(page, $"atom-window-cancel:{id}");

                    if (page is PopupPage pp)
                    {
                        if (!Rg.Plugins.Popup.Services.PopupNavigation.Instance.PopupStack.Any(x => x == page))
                        {
                            // AtomBridge.Instance.DisposePage(page, true);
                            page.Disappearing -= WAContext.GetDisappearHandler(page);
                            WAContext.SetDisappearHandler(page, null);
                        }
                        return;
                    }

                    if (!Navigation.NavigationStack.Any(x => x == page))
                    {
                        // AtomBridge.Instance.DisposePage(page, true);
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
                if (!(value is NavigationPage))
                {
                    if (value is MasterDetailPage mdp)
                    {
                        mdp.IsPresented = false;
                        if (!(mdp.Detail is NavigationPage))
                        {
                            var n = new NavigationPage(mdp.Detail);
                            mdp.Detail = new ContentPage { Title = "None", Content = new Label { Text = "None" } };
                            mdp.Detail = n;
                        }
                        Application.Current.MainPage = value;
                    }
                    else
                    {
                        var np = new NavigationPage(value);
                        if (string.IsNullOrWhiteSpace(value.Title))
                        {
                            value.Title = "App";
                        }
                        Application.Current.MainPage = np;
                    }
                }
                else
                {
                    Application.Current.MainPage = value;
                }
            }
        }

        public async Task PushAsync(Page e, bool value)
        {
            BindDisposer(e);

            var nav = Application.Current.MainPage.GetNavigation();

            if (e is PopupPage pp) {
                await nav.PushPopupAsync(pp, true);
                return;
            }

            if (Application.Current.MainPage is MasterDetailPage mdp)
            {
                nav = mdp.Detail.Navigation;
            }
            else {
                if (!(e is NavigationPage))
                {
                    e = new NavigationPage(e);
                }
            }

            if (nav != null) {
                await nav.PushAsync(e, value);
                return;
            }


            Application.Current.MainPage = e;
        }
        public async Task PopAsync(Element e, bool value)
        {
            var nav = Application.Current.MainPage.GetNavigation();

            if (e is PopupPage p)
            {
                await nav.RemovePopupPageAsync(p, value);
            }
            else {
                await nav.PopAsync();
            }
        }

        public INavigation Navigation {
            get {
                return Application.Current.MainPage.GetNavigation();
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

        #region WAName
        public static string GetWAName(BindableObject obj)
        {
            return (string)obj.GetValue(WANameProperty);
        }

        public static void SetWAName(BindableObject obj, string value)
        {
            obj.SetValue(WANameProperty, value);
        }

        // Using a DependencyProperty as the backing store for AtomControl.  This enables animation, styling, binding, etc...
        public static readonly BindableProperty WANameProperty =
            BindableProperty.CreateAttached(
                "WAName",
                typeof(string),
                typeof(WAContext),
                null);
        #endregion

        public IDisposable AddEventHandler(Element element, string name, Action action)
        {
            View view = element as View;

            if (view is Button button) {
                button.Command = new AtomCommand(action);
                return new AtomDisposable(() => {
                    button.Command = null;
                });
            }

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

    public static class PageExtensions {

        public static INavigation GetNavigation(this Page page) {
            if (page is NavigationPage) {
                return page.Navigation;
            }
            if (page is MasterDetailPage mdp) {
                return mdp.Detail.GetNavigation();
            }
            return null;
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


    public class JSListView : ListView {



        #region Property GroupHeaderField

        /// <summary>
        /// Bindable Property GroupHeaderField
        /// </summary>
        public static readonly BindableProperty GroupHeaderFieldProperty = BindableProperty.Create(
          nameof(GroupHeaderField),
          typeof(string),
          typeof(JSListView),
          null,
          BindingMode.OneWay,
          // validate value delegate
          // (sender,value) => true
          null,
          // property changed, delegate
          (sender,oldValue,newValue) => ((JSListView)sender).OnGroupHeaderFieldChanged(oldValue,newValue),
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
          typeof(JSListView),
          null,
          BindingMode.OneWay,
          // validate value delegate
          // (sender,value) => true
          null,
          // property changed, delegate
          (sender,oldValue,newValue) => ((JSListView)sender).OnGroupItemsFieldChanged(oldValue,newValue),
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

            if (propertyName == nameof(ItemsSource)) {
                ResetList();
            }

        }

        private void ResetList() {
            var items = this.ItemsSource;
            if (items == null || items is JSGroupList)
                return;

            this.ItemsSource = null;

            this.IsGroupingEnabled = true;
            this.GroupDisplayBinding = new Binding("Key");

            this.ItemsSource = new JSGroupList(items, GroupHeaderField, GroupItemsField );
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
