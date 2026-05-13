using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Eternal.Views.Helpers
{
    public partial class MonitorTestWindow : Window
    {
        private int _colorIndex = 0;
        private readonly SolidColorBrush[] _colors = new[]
        {
            System.Windows.Media.Brushes.Red,
            System.Windows.Media.Brushes.Green,
            System.Windows.Media.Brushes.Blue,
            System.Windows.Media.Brushes.White,
            System.Windows.Media.Brushes.Black,
            System.Windows.Media.Brushes.Gray
        };

        public MonitorTestWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _colorIndex = (_colorIndex + 1) % _colors.Length;
            MainGrid.Background = _colors[_colorIndex];
            
            // Adjust text color for visibility
            InfoText.Foreground = _colors[_colorIndex] == System.Windows.Media.Brushes.White ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        }
    }
}
