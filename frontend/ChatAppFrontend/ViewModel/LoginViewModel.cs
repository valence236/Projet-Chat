using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows.Input;
using ChatAppFrontend.Services;  


namespace ChatAppFrontend.ViewsModel
{
    public partial class LoginViewModel : ObservableObject
    {
        // TODO : Remplacer ce faux service par l'intégration réelle de l'API backend.
        private readonly AuthService _authService;

        public LoginViewModel()
        {
            _authService = new AuthService();
            LoginCommand = new AsyncRelayCommand(LoginAsync);
        }

        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public ICommand LoginCommand { get; }

        private async Task LoginAsync()
        {
            // Simulation d'un vrai compte utilisateur pour tester le login en local

            // TODO : Intégrer ici l'appel API backend réel avec Username et Password
         if (await _authService.LoginAsync(Username, Password))

            {
                ErrorMessage = "";

                // Navigation vers la page Home après succès
          NavigationService.NavigateToHome();


            }
            else
            {
                // Affichage du message d'erreur
                ErrorMessage = "Nom d'utilisateur ou mot de passe incorrect.";
            }

            await Task.CompletedTask; // Placeholder en attendant l'intégration API
        }
    }
}
