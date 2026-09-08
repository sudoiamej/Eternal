using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Media;

namespace Eternal.Views.Modules
{
    public partial class ConsoleView : System.Windows.Controls.UserControl
    {
        public ConsoleView()
        {
            InitializeComponent();
        }

        private void AddTabMenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (AddTabMenuButton.ContextMenu != null)
            {
                AddTabMenuButton.ContextMenu.PlacementTarget = AddTabMenuButton;
                AddTabMenuButton.ContextMenu.IsOpen = true;
            }
        }

        private void TerminalArea_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                var inputBox = FindChild<System.Windows.Controls.TextBox>(element, "InlineInputBox");
                if (inputBox != null)
                {
                    inputBox.Focus();
                    inputBox.CaretIndex = inputBox.Text?.Length ?? 0;
                }
            }
        }

        private void TerminalScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToBottom();

                if (scrollViewer.Content is FrameworkElement frameworkElement)
                {
                    var inputBox = FindChild<System.Windows.Controls.TextBox>(frameworkElement, "InlineInputBox");
                    if (inputBox != null)
                    {
                        inputBox.Focus();
                    }
                }
            }
        }

        private void TerminalOutputItems_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ItemsControl itemsControl && itemsControl.ItemsSource is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged += (s, args) =>
                {
                    if (args.Action == NotifyCollectionChangedAction.Add)
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            var scrollViewer = FindParent<ScrollViewer>(itemsControl);
                            scrollViewer?.ScrollToBottom();
                        });
                    }
                };
            }
        }

        private static T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && (child is FrameworkElement fe && fe.Name == childName))
                {
                    return typedChild;
                }

                var childOfChild = FindChild<T>(child, childName);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }

        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
    }
}
