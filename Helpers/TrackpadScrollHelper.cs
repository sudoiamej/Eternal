using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Eternal.Helpers
{
    public static class TrackpadScrollHelper
    {
        public static readonly DependencyProperty EnableTrackpadScrollingProperty =
            DependencyProperty.RegisterAttached(
                "EnableTrackpadScrolling",
                typeof(bool),
                typeof(TrackpadScrollHelper),
                new PropertyMetadata(false, OnEnableTrackpadScrollingChanged));

        public static bool GetEnableTrackpadScrolling(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableTrackpadScrollingProperty);
        }

        public static void SetEnableTrackpadScrolling(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableTrackpadScrollingProperty, value);
        }

        private static void OnEnableTrackpadScrollingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                if ((bool)e.NewValue)
                {
                    scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
                }
                else
                {
                    scrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
                }
            }
        }

        private static void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // Scroll instantly by the system wheel delta to match native Windows behavior
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }
    }

    // Helper behavior to animate VerticalOffset which is otherwise read-only in WPF directly
    public static class ScrollViewerBehavior
    {
        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.RegisterAttached(
                "VerticalOffset",
                typeof(double),
                typeof(ScrollViewerBehavior),
                new PropertyMetadata(0.0, OnVerticalOffsetChanged));

        public static double GetVerticalOffset(DependencyObject obj)
        {
            return (double)obj.GetValue(VerticalOffsetProperty);
        }

        public static void SetVerticalOffset(DependencyObject obj, double value)
        {
            obj.SetValue(VerticalOffsetProperty, value);
        }

        private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
            }
        }
    }
}
