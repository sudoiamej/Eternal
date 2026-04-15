using System.Windows.Controls;
using System.Windows.Input;
using Eternal.ViewModels.Modules;
using Eternal.Services.System;

namespace Eternal.Views.Modules 
{
    public partial class DriversView : UserControl 
    {
        public DriversView() { InitializeComponent(); }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is DriverInfo driver)
            {
                if (DataContext is DriversViewModel vm)
                {
                    vm.ShowDetailsCommand.Execute(driver);
                }
            }
        }
    }
}
