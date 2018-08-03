using System;
using WebAtoms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation (XamlCompilationOptions.Compile)]
namespace WebAtomsDemo
{
	public partial class App : Application
	{
		public App ()
		{
			InitializeComponent();

            // MainPage = new MainPage();

            MainPage = new ContentPage {
                Content = new Label {
                    Text = "Loading..."
                }
            };

            var engine = AtomBridge.Instance.engine;

            var amdLoader = "http://192.168.1.117:8081";

            var webAtomsCore = "http://192.168.1.117:8080";
            //var webAtomsCore = "https://cdn.jsdelivr.net/npm/web-atoms-core@1.0.41/";

            AtomBridge.Instance.ModuleUrls["web-atoms-core"] = webAtomsCore;

            var start = "http://192.168.0.105:8080/";
            // var start = "https://cdn.jsdelivr.net/npm/web-atoms-xamarin-forms-sample@1.0.13";

            Device.BeginInvokeOnMainThread(async () => {
                try
                {
                    await AtomBridge.Instance.InitAsync($"{amdLoader}");
                    AtomBridge.Instance.Execute($"UMD.map('reflect-metadata', '{webAtomsCore}/node_modules/reflect-metadata/Reflect.js')");
                    AtomBridge.Instance.Execute($"UMD.map('web-atoms-core', '{webAtomsCore}/')");
                    AtomBridge.Instance.Execute(
                        "UMD.loadView('web-atoms-core/dist/xf/samples/views/MovieList')" +
                        ".then(function (r) { console.log(r); })" +
                        ".catch(function (e) { console.log(e); });");

                    // var val = AtomBridge.Instance.engine.Global.Get("Promise");
                    // System.Diagnostics.Debug.WriteLine(val);
                }
                catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
            });



            // start point to download all modules and run it finally...
        }

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}
