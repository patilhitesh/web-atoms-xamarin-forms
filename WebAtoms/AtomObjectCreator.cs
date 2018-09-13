using System;
using System.Linq;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace WebAtoms
{
    [ContentProperty("Type")]
    public class AtomObjectCreator : IMarkupExtension<Element>
    {

        public string Type { get; set; }

        public object ProvideValue(IServiceProvider serviceProvider)
        {
            var root = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;

            var d = WAContext.GetImports(root.RootObject as Element);

            if (d.TryGetValue(Type, out Func<Element> f))
            {
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
