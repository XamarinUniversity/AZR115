using CastMyVote.Services;
using CastMyVote.ViewModels;
using Xamarin.Forms;
using XamarinUniversity.Interfaces;
using XamarinUniversity.Services;

[assembly: Xamarin.Forms.Xaml.XamlCompilationAttribute(Xamarin.Forms.Xaml.XamlCompilationOptions.Compile)]

namespace CastMyVote
{
    public class App : Application
    {
        public App()
        {
            // Register dependencies.
            var serviceLocator = XamUInfrastructure.Init();

            // Register the pages we can navigate to
            var navService = serviceLocator.Get<INavigationService>() as FormsNavigationPageService;
            navService.RegisterPage(AppPages.Responses, () => new ResultsPage());

            // Create the primary view model
            var vm = new MainViewModel(new AzureSurveyService(), serviceLocator);

            // The root page of your application
            MainPage = new NavigationPage(new MainPage() { BindingContext = vm });
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
