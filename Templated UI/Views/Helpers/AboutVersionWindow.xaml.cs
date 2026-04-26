using System.Windows;

namespace Eternal.Views.Helpers
{
    public partial class AboutVersionWindow : Window
    {
        public AboutVersionWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
