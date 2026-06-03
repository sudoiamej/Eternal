using System.Windows;
using Eternal.ViewModels.Modules;

namespace Eternal.Views.Modules
{
    public partial class FileForensicsView : System.Windows.Controls.UserControl
    {
        public FileForensicsView()
        {
            InitializeComponent();
        }

        private async void ImportBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Title = "Select File for Security & Integrity Analysis";
            dialog.Filter = "All Files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                if (DataContext is FileForensicsViewModel vm)
                {
                    await vm.AnalyzeFileCommand.ExecuteAsync(dialog.FileName);
                }
            }
        }
    }
}
