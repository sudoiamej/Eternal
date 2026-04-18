using System.Windows.Controls;
using System.Windows.Input;
using Eternal.ViewModels.Modules;
using Eternal.Services.System;

namespace Eternal.Views.Modules
{
    /// <summary>
    /// Interaction logic for ServicesView.xaml
    /// </summary>
    public partial class ServicesView : System.Windows.Controls.UserControl
    {
        public ServicesView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.DataGrid dg && dg.SelectedItem is ServiceInfo service)
            {
                if (DataContext is ServicesViewModel vm)
                {
                    vm.ShowDetailsCommand.Execute(service);
                }
            }
        }
    }
}
