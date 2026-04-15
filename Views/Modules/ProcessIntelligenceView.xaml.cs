using System.Windows.Controls;
using System.Windows.Input;
using Eternal.ViewModels.Modules;
using Eternal.Models;

namespace Eternal.Views.Modules
{
    public partial class ProcessIntelligenceView : UserControl
    {
        public ProcessIntelligenceView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is ProcessDetail process)
            {
                if (DataContext is ProcessIntelligenceViewModel vm)
                {
                    vm.ShowDetailsCommand.Execute(process);
                }
            }
        }
    }
}