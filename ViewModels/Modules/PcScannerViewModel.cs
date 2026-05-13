using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Eternal.Models;
using Eternal.Services.System;
using Eternal.ViewModels;

namespace Eternal.ViewModels.Modules
{
    public partial class PcScannerViewModel : BaseViewModel
    {
        private readonly IPcScannerService _scannerService;
        private readonly MainViewModel _main;
        private readonly ILoggingService _loggingService;

        [ObservableProperty] private int _scanProgress;
        [ObservableProperty] private ScannerSortOption _currentSortOption = ScannerSortOption.Default;
        
        private List<ScannerIssue> _allDiscoveredIssues = new();
        public ObservableCollection<ScannerIssue> RequiredIssues { get; } = new();
        public ObservableCollection<ScannerIssue> RecommendedIssues { get; } = new();
        public ObservableCollection<ScannerIssue> OptionalIssues { get; } = new();
        public ObservableCollection<ScannerIssue> SortedIssues { get; } = new();

        [ObservableProperty] private int _requiredCount;
        [ObservableProperty] private int _recommendedCount;
        [ObservableProperty] private int _optionalCount;

        public PcScannerViewModel(IPcScannerService scannerService, MainViewModel main, ILoggingService loggingService)
        {
            _scannerService = scannerService;
            _main = main;
            _loggingService = loggingService;
            StatusMessage = "Ready to scan system health.";
        }

        partial void OnCurrentSortOptionChanged(ScannerSortOption value) => SortIssues();

        [RelayCommand]
        private async Task StartScanAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            ScanProgress = 0;
            StatusMessage = "Initializing system health check...";
            _loggingService.Log("PC Scanner: Starting comprehensive system scan.");

            _allDiscoveredIssues.Clear();
            RequiredIssues.Clear();
            RecommendedIssues.Clear();
            OptionalIssues.Clear();
            SortedIssues.Clear();
            UpdateCounts();

            try
            {
                var progress = new Progress<int>(p => 
                {
                    ScanProgress = p;
                    if (p < 30) StatusMessage = "Analyzing storage partitions...";
                    else if (p < 60) StatusMessage = "Auditing process security...";
                    else if (p < 85) StatusMessage = "Monitoring system performance...";
                    else StatusMessage = "Finalizing diagnostic report...";
                });

                _allDiscoveredIssues = await _scannerService.RunFullScanAsync(progress);
                SortIssues();

                UpdateCounts();
                StatusMessage = _allDiscoveredIssues.Any() ? "Scan complete. Issues identified." : "Scan complete. No issues found.";
                _loggingService.Log($"PC Scanner: Scan finished. Found {_allDiscoveredIssues.Count} issues.");
            }
            catch (Exception ex)
            {
                StatusMessage = "An error occurred during scanning.";
                _loggingService.Log($"PC Scanner Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                ScanProgress = 100;
            }
        }

        private void SortIssues()
        {
            RequiredIssues.Clear();
            RecommendedIssues.Clear();
            OptionalIssues.Clear();
            SortedIssues.Clear();

            if (CurrentSortOption == ScannerSortOption.Default)
            {
                foreach (var issue in _allDiscoveredIssues)
                {
                    switch (issue.Severity)
                    {
                        case IssueSeverity.Required: RequiredIssues.Add(issue); break;
                        case IssueSeverity.Recommended: RecommendedIssues.Add(issue); break;
                        case IssueSeverity.Optional: OptionalIssues.Add(issue); break;
                    }
                }
            }
            else
            {
                var sorted = CurrentSortOption switch
                {
                    ScannerSortOption.Level => _allDiscoveredIssues.OrderBy(i => i.Severity).ThenBy(i => i.Title),
                    ScannerSortOption.EasyToHard => _allDiscoveredIssues.OrderBy(i => i.ActionType).ThenBy(i => i.Severity),
                    ScannerSortOption.Alphabetical => _allDiscoveredIssues.OrderBy(i => i.Title),
                    ScannerSortOption.SafeToDangerous => _allDiscoveredIssues.OrderByDescending(i => i.Severity).ThenBy(i => i.Title),
                    _ => _allDiscoveredIssues.OrderBy(i => i.Title)
                };

                foreach (var issue in sorted) SortedIssues.Add(issue);
            }
        }

        [RelayCommand]
        private async Task FixIssueAsync(ScannerIssue issue)
        {
            if (issue.ActionType == ScannerActionType.AutoFix)
            {
                StatusMessage = $"Applying fix: {issue.Title}...";
                bool success = await _scannerService.ExecuteFixAsync(issue);
                
                if (success)
                {
                    _allDiscoveredIssues.Remove(issue);
                    RemoveIssue(issue);
                    UpdateCounts();
                    StatusMessage = "Fix applied successfully.";
                    _loggingService.Log($"PC Scanner: Auto-fixed '{issue.Title}'.");
                }
                else
                {
                    StatusMessage = "Failed to apply fix automatically.";
                }
            }
            else if (issue.ActionType == ScannerActionType.ManualNavigation)
            {
                _loggingService.Log($"PC Scanner: Redirecting to '{issue.ActionTarget}' for manual resolution of '{issue.Title}'.");
                // Navigate to the target module
                _main.NavigateCommand.Execute(issue.ActionTarget);
            }
        }

        private void RemoveIssue(ScannerIssue issue)
        {
            if (RequiredIssues.Contains(issue)) RequiredIssues.Remove(issue);
            if (RecommendedIssues.Contains(issue)) RecommendedIssues.Remove(issue);
            if (OptionalIssues.Contains(issue)) OptionalIssues.Remove(issue);
            if (SortedIssues.Contains(issue)) SortedIssues.Remove(issue);
        }

        private void UpdateCounts()
        {
            RequiredCount = RequiredIssues.Count;
            RecommendedCount = RecommendedIssues.Count;
            OptionalCount = OptionalIssues.Count;
        }
    }
}
