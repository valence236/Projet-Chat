using System.Windows;
using System.Windows.Controls;
using ChatAppFrontend.ViewsModel;
using ChatAppFrontend.Services; 



namespace ChatAppFrontend.Views
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
            DataContext = new HomeViewModel();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
{
    SessionManager.Logout();
    NavigationService.NavigateToLogin();
}
    }
}
