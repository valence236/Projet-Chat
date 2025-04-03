using System.Windows;
using System.Windows.Controls;
using ChatAppFrontend.ViewsModel;
using ChatAppFrontend.Services;
using ChatAppFrontend.Models;




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
        private async void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is HomeViewModel vm && e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is Room selectedRoom)
                {
                    await vm.SelectionnerSalonAsync(selectedRoom);
                }
            }
        }


    }
}
