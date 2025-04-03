using System;
using System.Windows;
using System.Windows.Controls;
using ChatAppFrontend.Views;

namespace ChatAppFrontend.Services
{
    public static class NavigationService
    {
        private static Window? _mainWindow;
        private static ContentControl? _mainContentControl;

        public static void SetMainWindow(Window mainWindow)
        {
            try
            {
                _mainWindow = mainWindow;
                _mainContentControl = (ContentControl?)_mainWindow.FindName("MainContent");

                if (_mainContentControl == null)
                    throw new InvalidOperationException("Le ContentControl 'MainContent' est introuvable dans MainWindow.");
            }
            catch (Exception ex)
            {
                // Afficher un message d'erreur dans la console ou dans un journal
                Console.WriteLine($"Erreur dans SetMainWindow: {ex.Message}");
                MessageBox.Show($"Erreur lors de l'initialisation de la navigation: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void NavigateToHome()
        {
            try
            {
                if (Application.Current != null && _mainContentControl != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _mainContentControl.Content = new HomeView();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans NavigateToHome: {ex.Message}");
                MessageBox.Show($"Erreur lors de la navigation vers Home: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void NavigateToLogin()
        {
            try
            {
                if (Application.Current != null && _mainContentControl != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _mainContentControl.Content = new LoginView();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans NavigateToLogin: {ex.Message}");
                MessageBox.Show($"Erreur lors de la navigation vers Login: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        public static void NavigateToRegister()
        {
            try
            {
                if (Application.Current != null && _mainContentControl != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _mainContentControl.Content = new RegisterView();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans NavigateToRegister: {ex.Message}");
                MessageBox.Show($"Erreur lors de la navigation vers Register: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
