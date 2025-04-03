using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChatAppFrontend.ViewsModel;
using ChatAppFrontend.Services;

namespace ChatAppFrontend.Views
{
    public partial class RegisterView : UserControl
    {
        private readonly RegisterViewModel _viewModel;

        public RegisterView()
        {
            InitializeComponent();
            _viewModel = new RegisterViewModel();
            DataContext = _viewModel;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password;
            }
        }

        private void LoginText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            NavigationService.NavigateToLogin();
        }
    }
}
