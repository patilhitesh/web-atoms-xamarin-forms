using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace WebAtomsDemo.Droid
{
    [Activity(Label = "WebAtomsDemo", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle bundle)
        {

            base.OnCreate(bundle);

            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            Rg.Plugins.Popup.Popup.Init(this, bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);
            try
            {
                LoadApplication(new App());
            }
            catch (Exception ex) {
                throw;
            }
        }
    }
}

