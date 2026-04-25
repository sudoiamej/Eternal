using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;

namespace Eternal.Services.System
{
    public enum ToastSeverity { Info, Warning, Error, Success }

    public record ToastMessage(string Id, string Message, ToastSeverity Severity);

    public interface IToastService
    {
        ObservableCollection<ToastMessage> ActiveToasts { get; }
        void ShowInfo(string message);
        void ShowWarning(string message);
        void ShowError(string message);
        void ShowSuccess(string message);
    }

    public class ToastNotificationService : IToastService
    {
        public ObservableCollection<ToastMessage> ActiveToasts { get; } = new ObservableCollection<ToastMessage>();

        public void ShowInfo(string message) => AddToast(message, ToastSeverity.Info);
        public void ShowWarning(string message) => AddToast(message, ToastSeverity.Warning);
        public void ShowError(string message) => AddToast(message, ToastSeverity.Error);
        public void ShowSuccess(string message) => AddToast(message, ToastSeverity.Success);

        private void AddToast(string message, ToastSeverity severity)
        {
            var app = global::System.Windows.Application.Current;
            app?.Dispatcher.Invoke(() => 
            {
                // Prevent duplicate toasts for the same message
                if (ActiveToasts.Any(t => t.Message == message)) return;

                var id = Guid.NewGuid().ToString();
                var toast = new ToastMessage(id, message, severity);
                ActiveToasts.Add(toast);
                
                // Auto-dismiss timer
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (s, e) =>
                {
                    ActiveToasts.Remove(toast);
                    timer.Stop();
                };
                timer.Start();
            });
        }
    }
}
