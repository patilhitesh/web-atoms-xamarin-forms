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

            engine.Global.Put("App", Jint.Native.JsValue.FromObject(engine, WAContext.Current), true);
            engine.Global.Put("bridge", Jint.Native.JsValue.FromObject(engine, AtomBridge.Instance), true);

            AtomBridge.Instance.BaseUrl = "https://cdn.jsdelivr.net/npm/web-atoms-xamarin-forms-sample@1.0.5/bin/app.js";
            AtomBridge.Instance.ModuleUrls["web-atoms-core"] = "https://cdn.jsdelivr.net/npm/web-atoms-core@1.0.26/";

            Device.BeginInvokeOnMainThread(async () => { 
                await AtomBridge.Instance.ExecuteScriptAsync("https://cdn.jsdelivr.net/npm/web-atoms-core@1.0.26/define.js");
                await AtomBridge.Instance.ExecuteScriptAsync("https://cdn.jsdelivr.net/npm/web-atoms-xamarin-forms-sample@1.0.5/bin/app.js");
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
