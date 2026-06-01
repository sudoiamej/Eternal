using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Eternal.Views.Modules
{
    public partial class HomeGridDashboard : System.Windows.Controls.UserControl
    {
        private System.Windows.Point _startPoint;

        public HomeGridDashboard()
        {
            InitializeComponent();
        }

        private void Tile_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void Tile_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Point mousePos = e.GetPosition(null);
            System.Windows.Vector diff = _startPoint - mousePos;

            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > System.Windows.SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(diff.Y) > System.Windows.SystemParameters.MinimumVerticalDragDistance))
            {
                if (sender is System.Windows.Controls.Button button && button.CommandParameter != null)
                {
                    string viewName = button.CommandParameter.ToString();
                    System.Windows.DragDrop.DoDragDrop(button, viewName, System.Windows.DragDropEffects.Copy);
                }
            }
        }
    }
}
