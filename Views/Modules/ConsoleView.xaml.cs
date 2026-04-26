using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace Eternal.Views.Modules
{
    public partial class ConsoleView : System.Windows.Controls.UserControl
    {
        public ConsoleView()
        {
            InitializeComponent();
        }

        private void AddTabMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (AddTabMenuButton.ContextMenu != null)
            {
                AddTabMenuButton.ContextMenu.PlacementTarget = AddTabMenuButton;
                AddTabMenuButton.ContextMenu.IsOpen = true;
            }
        }

        private void CommandReference_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                "COMMON WINDOWS CLI TOOLS:\n\n" +
                "• sfc /scannow - System File Checker\n" +
                "• chkdsk /f - Check Disk\n" +
                "• ipconfig /flushdns - DNS Clear\n" +
                "• gpupdate /force - Group Policy Update\n" +
                "• netstat -ano - Network Connections\n" +
                "• tasklist - Process List\n" +
                "• systeminfo - System Summary\n" +
                "• wmic product get name - List Installed Apps",
                "Eternal Command Reference", 
                MessageBoxButton.OK, 
                MessageBoxImage.Information);
        }

        private void SessionOutputList_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Console: SessionOutputList_Loaded triggered.");
            if (sender is System.Windows.Controls.ListBox listBox && listBox.ItemsSource is INotifyCollectionChanged collection)
            {
                // Scroll to bottom initially
                if (listBox.Items.Count > 0)
                {
                    listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
                }

                // Subscribe to future changes
                collection.CollectionChanged += (s, args) =>
                {
                    if (args.Action == NotifyCollectionChangedAction.Add)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (listBox.Items.Count > 0)
                            {
                                listBox.ScrollIntoView(listBox.Items[listBox.Items.Count - 1]);
                            }
                        });
                    }
                };
            }
        }
    }
}
