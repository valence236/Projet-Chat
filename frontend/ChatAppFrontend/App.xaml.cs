using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using ChatAppFrontend.Services;

namespace ChatAppFrontend
{
    public partial class App : Application
    {
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        // Variable pour éviter la récursion infinie
        private static bool _isHandlingException = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

#if DEBUG
            AllocConsole(); // ✅ Ouvre la console uniquement en mode debug
#endif

            // Ajouter un gestionnaire global d'exceptions non gérées
            Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            try
            {
                // Configurer le service de navigation avec la fenêtre principale
                var mainWindow = MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    NavigationService.SetMainWindow(mainWindow);
                }
            }
            catch (Exception ex)
            {
                HandleException(ex, "Erreur lors du démarrage de l'application");
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            HandleException(exception, "Erreur non gérée dans le domaine de l'application");
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception, "Erreur non gérée dans l'interface utilisateur");
            e.Handled = true; // Empêcher l'application de se fermer
        }

        private void HandleException(Exception ex, string title)
        {
            // Éviter les boucles récursives d'exceptions
            if (_isHandlingException)
            {
                // Écrire dans la console au lieu d'afficher une boîte de dialogue
                Console.WriteLine($"[ERREUR RÉCURSIVE] {title}: {ex?.Message}");
                return;
            }

            _isHandlingException = true;

            try
            {
                // Enregistrer l'erreur dans un fichier log
                string errorMessage = $"[{DateTime.Now}] {title}: {ex?.Message}\r\n{ex?.StackTrace}\r\n";
                File.AppendAllText("app_errors.log", errorMessage);

                // Écrire dans la console au lieu d'utiliser MessageBox
                Console.WriteLine($"[ERREUR] {title}: {ex?.Message}");
                
                // Afficher une fenêtre d'erreur simple sans TextBlock
                ShowSimpleErrorWindow(title, ex?.Message ?? "Erreur inconnue");
            }
            catch (Exception logEx)
            {
                // Dernier recours : écrire dans la console
                Console.WriteLine($"[ERREUR CRITIQUE] Impossible de gérer l'exception : {logEx.Message}");
                Console.WriteLine($"Exception originale : {ex?.Message}");
            }
            finally
            {
                _isHandlingException = false;
            }
        }
        
        private void ShowSimpleErrorWindow(string title, string message)
        {
            try
            {
                // Créer une fenêtre d'erreur très simple sans TextBlock
                var errorWindow = new Window
                {
                    Title = title,
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize
                };
                
                var button = new System.Windows.Controls.Button
                {
                    Content = "OK",
                    Width = 80,
                    Height = 30,
                    Margin = new Thickness(0, 20, 0, 0)
                };
                
                button.Click += (s, e) => errorWindow.Close();
                
                var stackPanel = new System.Windows.Controls.StackPanel
                {
                    Margin = new Thickness(20)
                };
                
                // Utiliser Label au lieu de TextBlock
                var label = new System.Windows.Controls.Label
                {
                    Content = message.Length > 500 ? message.Substring(0, 500) + "..." : message
                };
                
                stackPanel.Children.Add(label);
                stackPanel.Children.Add(button);
                
                errorWindow.Content = stackPanel;
                
                errorWindow.ShowDialog();
            }
            catch
            {
                // Si même cette méthode échoue, on abandonne silencieusement
                Console.WriteLine("Impossible d'afficher la fenêtre d'erreur simple");
            }
        }
    }
}
