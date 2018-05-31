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


            // var webAtomsCore = "http://192.168.0.31:8080/";
            var webAtomsCore = "https://cdn.jsdelivr.net/npm/web-atoms-core@1.0.36/";

            AtomBridge.Instance.ModuleUrls["web-atoms-core"] = webAtomsCore;

            // var start = "http://192.168.0.31:8081/";
            var start = "https://cdn.jsdelivr.net/npm/web-atoms-xamarin-forms-sample@1.0.7";

            Device.BeginInvokeOnMainThread(async () => { 
                await AtomBridge.Instance.ExecuteScriptAsync($"{webAtomsCore}define.js");
                await AtomBridge.Instance.ExecuteScriptAsync($"{start}/bin/app.js");
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
