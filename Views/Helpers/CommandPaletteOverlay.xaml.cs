using System.Windows.Input;
using Eternal.ViewModels.Modules;

namespace Eternal.Views.Helpers
{
    public partial class CommandPaletteOverlay : System.Windows.Controls.UserControl
    {
        public CommandPaletteOverlay()
        {
            InitializeComponent();
            this.IsVisibleChanged += (s, e) =>
            {
                if ((bool)e.NewValue)
                {
                    SearchBox.Focus();
                }
            };
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is CommandPaletteViewModel vm) vm.Close();
        }

        private void SearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is not CommandPaletteViewModel vm) return;

            if (e.Key == System.Windows.Input.Key.Escape)
            {
                vm.Close();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                vm.ExecuteSelectedCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Down)
            {
                if (vm.FilteredCommands.Count > 0)
                {
                    int index = vm.SelectedCommand == null ? 0 : vm.FilteredCommands.IndexOf(vm.SelectedCommand) + 1;
                    if (index < vm.FilteredCommands.Count) vm.SelectedCommand = vm.FilteredCommands[index];
                }
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Up)
            {
                if (vm.FilteredCommands.Count > 0)
                {
                    int index = vm.SelectedCommand == null ? 0 : vm.FilteredCommands.IndexOf(vm.SelectedCommand) - 1;
                    if (index >= 0) vm.SelectedCommand = vm.FilteredCommands[index];
                }
                e.Handled = true;
            }
        }
    }
}
