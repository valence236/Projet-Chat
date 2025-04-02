using System.Windows;
using ChatAppFrontend.Services;

namespace ChatAppFrontend
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            NavigationService.SetMainWindow(this);
            NavigationService.NavigateToLogin(); // Affiche la LoginView au lancement
        }
    }
}
