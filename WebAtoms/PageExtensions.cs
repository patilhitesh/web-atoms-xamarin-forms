using System;
using System.Linq;
using Xamarin.Forms;

namespace WebAtoms
{
    public static class PageExtensions
    {

        public static INavigation GetNavigation(this Page page)
        {
            if (page is NavigationPage)
            {
                return page.Navigation;
            }
            if (page is MasterDetailPage mdp)
            {
                return mdp.Detail.GetNavigation();
            }
            return null;
        }

    }
}
