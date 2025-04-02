using CommunityToolkit.Mvvm.ComponentModel;
using ChatAppFrontend.Services;

namespace ChatAppFrontend.ViewsModel
{
    public partial class HomeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string welcomeMessage;

        public HomeViewModel()
        {
            WelcomeMessage = $"Bienvenue {SessionManager.Username} sur l'application de Chat !";
        }
    }
}
