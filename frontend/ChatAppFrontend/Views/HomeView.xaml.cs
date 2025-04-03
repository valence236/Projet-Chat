using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChatAppFrontend.ViewsModel;
using ChatAppFrontend.Services;
using ChatAppFrontend.ViewModel;

namespace ChatAppFrontend.Views
{
    public partial class HomeView : UserControl
    {
        private readonly HomeViewModel _viewModel;

        public HomeView()
        {
            InitializeComponent();
            _viewModel = new HomeViewModel();
            DataContext = _viewModel;
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            SessionManager.Logout();
            NavigationService.NavigateToLogin();
        }

        private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Envoyer le message quand on appuie sur Entrée
            if (e.Key == Key.Enter)
            {
                Console.WriteLine("Touche Entrée détectée");
                
                // S'assurer que la commande existe et peut être exécutée
                if (_viewModel.EnvoyerMessageCommand != null && _viewModel.EnvoyerMessageCommand.CanExecute(null))
                {
                    Console.WriteLine("Exécution de la commande EnvoyerMessageCommand via touche Entrée");
                    _viewModel.EnvoyerMessageCommand.Execute(null);
                    e.Handled = true;  // Empêcher le traitement supplémentaire de l'événement
                }
                else
                {
                    Console.WriteLine("La commande EnvoyerMessageCommand n'est pas disponible ou ne peut pas être exécutée");
                }
            }
        }

        private void PublicChat_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectConversation(ConversationType.Public, null, "Chat Public");
        }
    }
}
