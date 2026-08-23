using UserControl = System.Windows.Controls.UserControl;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using Eternal.Models;
using Eternal.ViewModels.Modules;

namespace Eternal.Views.Modules
{
    public partial class ExplorerView : UserControl
    {
        public ExplorerView()
        {
            InitializeComponent();
        }

        private void AddressTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (DataContext is ExplorerViewModel vm)
                {
                    _ = vm.NavigateToPathAsync(AddressTextBox.Text);
                }
            }
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ExplorerViewModel vm && vm.SelectedItem != null)
            {
                vm.ExecuteOrOpenItemCommand.Execute(vm.SelectedItem);
            }
        }
    }
}
