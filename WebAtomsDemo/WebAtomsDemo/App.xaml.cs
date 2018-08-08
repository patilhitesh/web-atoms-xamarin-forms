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

            // var amdLoader = "http://192.168.1.117:8081";
            var amdLoader = "https://cdn.jsdelivr.net/npm/web-atoms-amd-loader@1.0.22";

            var webAtomsCore = "http://192.168.1.105:8080";
            // var webAtomsCore = "https://cdn.jsdelivr.net/npm/web-atoms-core@1.0.291";

            // var reflectMetadata = "https://cdn.jsdelivr.net/npm/reflect-metadata@0.1.12";

            Device.BeginInvokeOnMainThread(async () => {
                try
                {
                    await AtomBridge.Instance.InitAsync($"{amdLoader}");
                    // await AtomBridge.Instance.ExecuteScriptAsync($"{webAtomsCore}/src/xf/samples/index.js");
                    await AtomBridge.Instance.ExecuteScriptAsync("https://cdn.jsdelivr.net/npm/web-atoms-samples@1.0.5/src/xf/index.js");
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
