using System;
using System.Windows;
using System.Windows.Controls;
using ChatAppFrontend.Views;

namespace ChatAppFrontend.Services
{
    public static class NavigationService
    {
        private static Window _mainWindow;
        private static ContentControl _mainContentControl;

        public static void SetMainWindow(Window mainWindow)
        {
            _mainWindow = mainWindow;
            _mainContentControl = (ContentControl)_mainWindow.FindName("MainContent");

            if (_mainContentControl == null)
                throw new InvalidOperationException("Le ContentControl 'MainContent' est introuvable dans MainWindow.");
        }

        public static void NavigateToHome()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _mainContentControl.Content = new HomeView();
            });
        }

        public static void NavigateToLogin()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _mainContentControl.Content = new LoginView();
            });
        }
        public static void NavigateToRegister()
{
    Application.Current.Dispatcher.Invoke(() =>
    {
        _mainContentControl.Content = new RegisterView();
    });
}

    }
}
