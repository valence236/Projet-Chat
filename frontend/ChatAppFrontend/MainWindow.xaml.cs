using System;
using System.Windows;
using ChatAppFrontend.Services;
using ChatAppFrontend.Views;

namespace ChatAppFrontend
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                
                // Configurer le service de navigation
                NavigationService.SetMainWindow(this);
                
                // Naviguer vers la page de login
                MainContent.Content = new LoginView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'initialisation de l'application: {ex.Message}", 
                    "Erreur critique", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }
    }
}
