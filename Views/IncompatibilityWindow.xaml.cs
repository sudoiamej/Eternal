using System.Windows;

namespace Eternal.Views
{
    public partial class IncompatibilityWindow : Window
    {
        private readonly bool _isTestMode;

        public IncompatibilityWindow(bool isTestMode = false)
        {
            _isTestMode = isTestMode;
            InitializeComponent();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            if (_isTestMode)
            {
                this.Close();
            }
            else
            {
                System.Windows.Application.Current.Shutdown();
            }
        }
    }
}
