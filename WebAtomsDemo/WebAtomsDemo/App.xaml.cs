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

            var engine = AtomBridge.Instance.Engine;

            var amdLoader = "https://cdn.jsdelivr.net/npm/web-atoms-amd-loader@1.0.41";

            // AtomBridge.Instance.Client.BaseAddress = new Uri("http://192.168.1.9:8080"); 
            // AtomBridge.Instance.Client.BaseAddress = new Uri("https://cdn.jsdelivr.net/npm/web-atoms-samples@1.0.6");

            AtomBridge.Instance.Client.BaseAddress = new Uri("https://v2018-test.800casting.com/uiv/ts-apps/dist/xf/Admin?version=1.0.61");

            Device.BeginInvokeOnMainThread(async () => {
                try
                {
                    await AtomBridge.Instance.InitAsync($"{amdLoader}");
                    // await AtomBridge.Instance.ExecuteScriptAsync($"/src/xf/samples/index.js");
                    // await AtomBridge.Instance.ExecuteScriptAsync("/src/xf/index.js");
                    await AtomBridge.Instance.ExecuteScriptAsync("https://v2018-test.800casting.com/uiv/ts-apps@1.0.63/dist/xf/Admin?platform=xf");                   // var val = AtomBridge.Instance.engine.Global.Get("Promise");
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
