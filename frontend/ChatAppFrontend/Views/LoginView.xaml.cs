using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ChatAppFrontend.ViewsModel;
using ChatAppFrontend.Services;

namespace ChatAppFrontend.Views
{
    public partial class LoginView : UserControl
    {
        private readonly LoginViewModel _viewModel;

        public LoginView()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel();
            DataContext = _viewModel;
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = ((PasswordBox)sender).Password;
            }
        }

        private void SignUpText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            NavigationService.NavigateToRegister();
        }


    }
}
