using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;
using Eternal.Services.System;

using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class SnapshotsViewModel : BaseViewModel
    {
        private readonly ISnapshotService _snapshotService;
        private readonly ILoggingService _loggingService;

        [ObservableProperty] private string _newSnapshotDescription = "Manual System State Baseline";
        
        public ObservableCollection<SystemSnapshot> Snapshots { get; } = new ObservableCollection<SystemSnapshot>();
        public ObservableCollection<SnapshotDiff> ComparisonResults { get; } = new ObservableCollection<SnapshotDiff>();

        [ObservableProperty] private SystemSnapshot? _selectedSnapshotA;
        [ObservableProperty] private SystemSnapshot? _selectedSnapshotB;
        [ObservableProperty] private bool _isComparing;

        public SnapshotsViewModel(ISnapshotService snapshotService, ILoggingService loggingService)
        {
            _snapshotService = snapshotService;
            _loggingService = loggingService;
        }

        [RelayCommand]
        public async Task LoadSnapshotsAsync()
        {
            IsBusy = true;
            try
            {
                var list = await _snapshotService.GetSavedSnapshotsAsync();
                Snapshots.Clear();
                foreach (var s in list) Snapshots.Add(s);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task TakeSnapshot()
        {
            IsBusy = true;
            try
            {
                var snapshot = await _snapshotService.CreateSnapshotAsync(NewSnapshotDescription);
                await _snapshotService.SaveSnapshotAsync(snapshot);
                _loggingService.Log($"Created system state snapshot: {NewSnapshotDescription}");
                await LoadSnapshotsAsync();
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task DeleteSnapshot(SystemSnapshot snapshot)
        {
            if (snapshot == null) return;
            await _snapshotService.DeleteSnapshotAsync(snapshot.Id);
            await LoadSnapshotsAsync();
        }

        [RelayCommand]
        private void CompareSelected()
        {
            if (SelectedSnapshotA == null || SelectedSnapshotB == null) return;

            IsComparing = true;
            ComparisonResults.Clear();

            var diffs = _snapshotService.CompareSnapshots(SelectedSnapshotA, SelectedSnapshotB);
            
            // Sort by modifications first, then additions, then removals
            var sorted = diffs.Where(d => d.Type != DiffType.Identical)
                             .OrderBy(d => d.Type)
                             .ThenBy(d => d.Category);

            foreach (var d in sorted) ComparisonResults.Add(d);
        }

        [RelayCommand]
        private void ClearComparison()
        {
            IsComparing = false;
            ComparisonResults.Clear();
        }
    }
}
