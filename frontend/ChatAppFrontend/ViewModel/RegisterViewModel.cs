using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows.Input;
using ChatAppFrontend.Services;

namespace ChatAppFrontend.ViewsModel
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly AuthService _authService;

        public RegisterViewModel()
        {
            _authService = new AuthService();
            RegisterCommand = new AsyncRelayCommand(RegisterAsync);
        }

        [ObservableProperty]
        private string username;

        [ObservableProperty]
        private string email;

        [ObservableProperty]
        private string password;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public ICommand RegisterCommand { get; }

        private async Task RegisterAsync()
        {
            if (await _authService.RegisterAsync(Username, Email, Password))
            {
                ErrorMessage = "";
                NavigationService.NavigateToLogin();
            }
            else
            {
                ErrorMessage = "Erreur lors de l'inscription. Veuillez vérifier vos informations.";
            }

            await Task.CompletedTask;
        }
    }
}
