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

        private void FileZone_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private async void FileZone_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    if (DataContext is FileForensicsViewModel vm)
                    {
                        await vm.AnalyzeFileCommand.ExecuteAsync(files[0]);
                    }
                }
            }
        }
    }
}
