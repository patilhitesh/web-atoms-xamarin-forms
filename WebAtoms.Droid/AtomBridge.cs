using Jint;
using Jint.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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

        public void AttachControl(View element, JsValue control) {
            var ac = WAContext.GetAtomControl(element);
            if(ac != null)
            {
                throw new InvalidOperationException("Control already attached");
            }
            WAContext.SetAtomControl(element, control);
        }

        public IDisposable AddEventHandler(View element, string name, JsValue callback, bool? capture)
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

        public object AtomParent(Element element, JsValue climbUp)
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

        public void VisitDescendents(Element element, JsValue action)
        {
            foreach (var e in (element as IElementController).LogicalChildren) {
                var ac = WAContext.GetAtomControl(e);
                var r = action.Invoke(
                    JsValue.FromObject(engine,e), 
                    (JsValue)ac);
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

        public JsValue GetValue(Element view, string name) {
            return null;
        }

        public void SetValue(Element view, string name, JsValue value) {

            bool isNull = value.IsNull() || value.IsUndefined();

            var pv = view.GetProperty(name);

            if (isNull) {
                pv.SetValue(view, null);
                return;
            }

            var pt = pv.PropertyType;

            if (pt == typeof(string)) {
                pv.SetValue(view, value.AsString());
                return;
            }

            pt = Nullable.GetUnderlyingType(pt) ?? pt;

            if (value.IsDate()) {
                // conver to datetime and set...
                pv.SetValue(view, value.AsDate().ToDateTime());
                return;
            }

            if (pt.IsValueType) {
                // convert...
                var v = Convert.ChangeType(value.ToObject(), pt);
                pv.SetValue(view, v);
            }
        }



    }

    public static class DictionaryExtensions {

        static Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();

        public static TValue GetOrCreate<TKey, TValue>(
            this Dictionary<TKey,TValue> d,
            TKey key,
            Func<TKey, TValue> factory) {
            if (d.TryGetValue(key, out TValue value))
                return value;
            TValue v = factory(key);
            d[key] = v;
            return v;
        }

        public static PropertyInfo GetProperty(this object value, string name) {
            Type type = value.GetType();
            string key = $"{type.FullName}.{name}";

            return properties.GetOrCreate(key, k => type.GetProperties().First(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
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