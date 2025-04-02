using System.Windows.Controls;
using ChatAppFrontend.ViewsModel;

namespace ChatAppFrontend.Views
{
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
            DataContext = new HomeViewModel();
        }
    }
}
