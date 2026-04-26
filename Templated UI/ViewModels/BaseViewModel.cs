using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;

namespace Eternal.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _statusMessage = "Ready";

        protected async Task ExecuteBusyActionAsync(Func<Task> action, string? busyMessage = null)
        {
            if (IsBusy) return;

            string originalStatus = StatusMessage;
            IsBusy = true;
            if (busyMessage != null) StatusMessage = busyMessage;

            try
            {
                await action();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                // In Step 4, we will also send this to the ToastService
            }
            finally
            {
                IsBusy = false;
                if (busyMessage != null) StatusMessage = "Complete";
            }
        }
    }
}
