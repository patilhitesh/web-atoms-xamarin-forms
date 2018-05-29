using Jint;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using Xamarin.Forms;

namespace WebAtoms
{
    public class AtomBridge
    {

        public static Engine engine = new Engine();

        IEnumerable<System.Reflection.TypeInfo> types;

        public View Create(string name)
        {

            types = types ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.DefinedTypes).ToList();

            var type = types.FirstOrDefault(x => x.FullName.Equals(name, StringComparison.OrdinalIgnoreCase));

            return Activator.CreateInstance(type) as View;
        }

        public void AttachControl(View element, Jint.Native.JsValue control) {
            var ac = WAContext.GetAtomControl(element);
            if(ac != null)
            {
                throw new InvalidOperationException("Control already attached");
            }
            WAContext.SetAtomControl(element, control);
        }

        public IDisposable AddEventHandler(View element, string name, Jint.Native.JsValue callback, bool? capture)
        {
            PropertyChangedEventHandler handler = (s, e) => {
                if (e.PropertyName == name)
                {
                    callback.Invoke(e.PropertyName);
                }
            };

            element.PropertyChanged += handler;
            return new AtomDisposable(() => {
                element.PropertyChanged -= handler;
            });
        }

        public object AtomParent(Element element, Jint.Native.JsValue climbUp)
        {
            bool cu = !climbUp.IsUndefined() && !climbUp.IsNull() && climbUp.AsBoolean();
            do {
                var e = WAContext.GetAtomControl(element);
                if (e != null)
                    return e;
                element = element.Parent;
            } while (cu && element != null);
            return null;
        }

        public object ElementParent(Element element) {
            return element.Parent;
        }

        public object TemplateParent(Element element) {
            do {
                var e = WAContext.GetTemplateParent(element);
                if (e != null)
                    return e;
                element = element.Parent;
            } while (element != null);
            return null;
        }

        public void VisitDescendents(Element element, Jint.Native.JsValue action)
        {
            foreach (var e in (element as IElementController).LogicalChildren) {
                var ac = WAContext.GetAtomControl(e);
                var r = action.Invoke(
                    Jint.Native.JsValue.FromObject(engine,e), 
                    (Jint.Native.JsValue)ac);
                if (r.IsUndefined() || r.IsNull() || !r.AsBoolean())
                    continue;
                VisitDescendents(e, action);
            }
        }

        public void Dispose(Element e) {
            WAContext.SetAtomControl(e, null);
            WAContext.SetLogicalParent(e, null);
            WAContext.SetTemplateParent(e, null);
        }

        public void AppendChild(Element view, Element child) {
            switch (view) {
                case ContentView cv:
                    cv.Content = child as View;
                    break;
                case Layout<View> grid:
                    grid.Children.Add(child as View);
                    break;
            }
        }



    }

    public class AtomDisposable : IDisposable
    {
        readonly Action action;

        public AtomDisposable(Action action)
        {
            this.action = action;
        }

        public void Dispose()
        {
            action?.Invoke();
        }
    }

}