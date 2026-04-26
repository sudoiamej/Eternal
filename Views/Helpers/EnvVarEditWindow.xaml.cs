using System.Windows;
using System.Windows.Input;

namespace Eternal.Views.Helpers
{
    public partial class EnvVarEditWindow : Window
    {
        public string VarName => NameInput.Text;
        public string VarValue => ValueInput.Text;
        public bool IsSystem => SystemScopeRadio.IsChecked ?? false;

        public EnvVarEditWindow(string name = "", string value = "", bool isSystem = false, bool isEdit = false)
        {
            InitializeComponent();
            
            NameInput.Text = name;
            ValueInput.Text = value;
            
            if (isSystem)
            {
                SystemScopeRadio.IsChecked = true;
                UserScopeRadio.IsChecked = false;
            }
            else
            {
                UserScopeRadio.IsChecked = true;
                SystemScopeRadio.IsChecked = false;
            }

            if (isEdit)
            {
                HeaderTitle.Text = "EDIT ENVIRONMENT VARIABLE";
                NameInput.IsEnabled = false;
                UserScopeRadio.IsEnabled = false;
                SystemScopeRadio.IsEnabled = false;
            }
            else
            {
                HeaderTitle.Text = "ADD NEW ENVIRONMENT VARIABLE";
            }

            NameInput.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(VarName))
            {
                System.Windows.MessageBox.Show("Variable name cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
