using System.Windows;
using Eternal.Models;
using Microsoft.Win32;

namespace Eternal.Views.Helpers
{
    public partial class RegistryEditWindow : Window
    {
        public object NewValue { get; private set; }
        private readonly RegistryValueInfo _original;

        public RegistryEditWindow(RegistryValueInfo info)
        {
            InitializeComponent();
            _original = info;
            NewValue = info.Value;

            ValueNameText.Text = string.IsNullOrEmpty(info.Name) ? "(Default)" : info.Name;
            TypeText.Text = info.Kind.ToString().ToUpper();
            CurrentValueText.Text = info.Summary;
            ValueInput.Text = info.Value?.ToString() ?? "";
            
            // Set focus to input
            ValueInput.Focus();
            ValueInput.SelectAll();
        }

        private void Commit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = ValueInput.Text;
                
                // Type-safe conversion
                NewValue = _original.Kind switch
                {
                    RegistryValueKind.DWord => uint.Parse(input),
                    RegistryValueKind.QWord => ulong.Parse(input),
                    RegistryValueKind.String => input,
                    RegistryValueKind.ExpandString => input,
                    RegistryValueKind.MultiString => input.Split('\n'),
                    _ => input
                };

                DialogResult = true;
                Close();
            }
            catch
            {
                MessageBox.Show("Invalid input format for the selected registry type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
