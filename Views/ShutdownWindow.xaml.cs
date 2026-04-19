using System.Windows;

namespace Eternal.Views
{
    public partial class ShutdownWindow : Window
    {
        public ShutdownWindow()
        {
            InitializeComponent();
        }

        public void UpdateStatus(string status, string detail = "")
        {
            StatusText.Text = status;
            if (!string.IsNullOrEmpty(detail))
                DetailText.Text = detail;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }
    }
}