using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Eternal.Views.Helpers
{
    public partial class CustomNotificationWindow : Window
    {
        public enum NotificationType
        {
            Info,
            Success,
            Warning,
            Error,
            Question
        }

        public CustomNotificationWindow()
        {
            InitializeComponent();
        }

        public static bool? Show(string message, string title = "NOTIFICATION", NotificationType type = NotificationType.Info, bool showCancel = false, Window owner = null)
        {
            var win = new CustomNotificationWindow();
            if (owner != null)
                win.Owner = owner;
            else if (System.Windows.Application.Current != null && System.Windows.Application.Current.MainWindow != null && System.Windows.Application.Current.MainWindow.IsVisible)
                win.Owner = System.Windows.Application.Current.MainWindow;

            win.MessageText.Text = message;
            win.TitleText.Text = title.ToUpper();

            // Resolve explicit WPF Media Brushes to avoid ambiguous Drawing namespace reference
            var accent = System.Windows.Application.Current?.Resources["AccentBrush"] as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.White;
            var success = System.Windows.Application.Current?.Resources["SuccessBrush"] as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Green;
            var warning = System.Windows.Application.Current?.Resources["WarningBrush"] as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Orange;
            var critical = System.Windows.Application.Current?.Resources["CriticalBrush"] as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Red;
            var info = System.Windows.Application.Current?.Resources["InfoBrush"] as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Blue;

            switch (type)
            {
                case NotificationType.Success:
                    win.TypeIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.CheckCircle;
                    win.TypeIcon.Foreground = success;
                    break;
                case NotificationType.Warning:
                    win.TypeIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.ExclamationTriangle;
                    win.TypeIcon.Foreground = warning;
                    break;
                case NotificationType.Error:
                    win.TypeIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.TimesCircle;
                    win.TypeIcon.Foreground = critical;
                    break;
                case NotificationType.Question:
                    win.TypeIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.QuestionCircle;
                    win.TypeIcon.Foreground = info;
                    break;
                case NotificationType.Info:
                default:
                    win.TypeIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.InfoCircle;
                    win.TypeIcon.Foreground = accent;
                    break;
            }

            if (showCancel || type == NotificationType.Question)
            {
                win.CancelBtn.Visibility = Visibility.Visible;
                System.Windows.Controls.Grid.SetColumnSpan(win.ConfirmBtn, 1);
            }
            else
            {
                win.CancelBtn.Visibility = Visibility.Collapsed;
                System.Windows.Controls.Grid.SetColumn(win.ConfirmBtn, 0);
                System.Windows.Controls.Grid.SetColumnSpan(win.ConfirmBtn, 2);
                win.ConfirmBtn.Margin = new Thickness(0);
            }

            return win.ShowDialog();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
