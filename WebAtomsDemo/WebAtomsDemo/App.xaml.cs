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

            MainPage = new NavigationPage( new ContentPage {
                Title = "Loading..",
                Content = new Label {
                    Text = "Loading..."
                }
            });

            AtomBridge.AmdUrl = "https://cdn.jsdelivr.net/npm/web-atoms-amd-loader@1.0.41";

            DependencyService.Get<NavigationService>().SetLocation("https://www.webatoms.in/xf/samples.js");



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
