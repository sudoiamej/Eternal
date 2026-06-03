using System.Windows;
using System.Windows.Input;

namespace Eternal.Views.Helpers
{
    public partial class AboutVersionWindow : Window
    {
        public AboutVersionWindow()
        {
            InitializeComponent();
            BuildDateText.Text = System.DateTime.Today.ToString("yyyy-MM-dd");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
