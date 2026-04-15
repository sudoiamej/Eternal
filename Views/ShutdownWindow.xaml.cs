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
    }
}