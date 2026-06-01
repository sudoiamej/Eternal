using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;

namespace Eternal.ViewModels
{
    public partial class BaseViewModel : ObservableObject, IMemoryOptimizable
    {
        [ObservableProperty] private string _title = string.Empty;
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _statusMessage = "Ready";
        [ObservableProperty] private bool _isFocusModeActive;

        public virtual void Activate() { }
        public virtual void Deactivate() 
        {
            // By default, when a view is deactivated, we check if we should release memory
            ReleaseMemory();
        }

        public virtual void ReleaseMemory() { }

        protected async Task ExecuteBusyActionAsync(Func<Task> action, string? busyMessage = null)
        {
            if (IsBusy || IsLoading) return;

            string originalStatus = StatusMessage;
            IsBusy = true;
            IsLoading = true;
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
                IsLoading = false;
                if (busyMessage != null) StatusMessage = "Complete";
            }
        }
    }
}
