using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Eternal.ViewModels.Modules;

namespace Eternal.Views.Modules
{
    public partial class ComponentsView : System.Windows.Controls.UserControl
    {
        public ComponentsView()
        {
            InitializeComponent();
            Loaded += (s, e) => 
            {
                Focus();
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.Activated += (s2, e2) => ResumeIfActive();
                    window.Deactivated += (s2, e2) => SuspendIfActive();
                }
            };

            IsVisibleChanged += (s, e) =>
            {
                if ((bool)e.NewValue) ResumeIfActive();
                else SuspendIfActive();
            };
        }

        private void ResumeIfActive()
        {
            if (IsVisible && DataContext is ComponentsViewModel vm)
            {
                vm.Resume();
            }
        }

        private void SuspendIfActive()
        {
            if (DataContext is ComponentsViewModel vm)
            {
                vm.Suspend();
            }
        }

        private void UserControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is ComponentsViewModel vm)
            {
                vm.HandleKeyDown(e.Key.ToString());
                e.Handled = true; // Prevent accidental shortcuts
            }
        }

        private void UserControl_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (DataContext is ComponentsViewModel vm)
            {
                vm.HandleKeyUp(e.Key.ToString());
                e.Handled = true;
            }
        }

        private void ClearCanvas_Click(object sender, RoutedEventArgs e)
        {
            var canvas = FindVisualChild<InkCanvas>(DetailArea);
            if (canvas != null)
            {
                canvas.Strokes.Clear();
            }
        }

        private T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T t)
                    return t;
                
                T? childOfChild = FindVisualChild<T>(child!);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
    }
}
