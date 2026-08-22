using System.Windows;
using System.Windows.Input;

namespace Eternal.Views.Helpers
{
    public partial class BsodSimulatorWindow : Window
    {
        public BsodSimulatorWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
