using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Models;
using Eternal.Services.System;
using Microsoft.Win32;
using System.Windows;

namespace Eternal.ViewModels.Modules
{
    public partial class RegistryViewModel : BaseViewModel
    {
        private readonly IRegistryService _registryService;
        private readonly IToastService _toastService;

        [ObservableProperty] private string _searchPath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        [ObservableProperty] private RegistryKeyInfo? _currentKey;
        [ObservableProperty] private string _keyDescription = string.Empty;
        [ObservableProperty] private RegistryProvenance _currentProvenance = new();
        [ObservableProperty] private ObservableCollection<RegistryTweakDefinition> _commonTweaks = new();
        [ObservableProperty] private ObservableCollection<RegistryUndoEntry> _undoVault = new();
        [ObservableProperty] private ObservableCollection<RegistryWatchEntry> _watchlist = new();

        public RegistryViewModel(IRegistryService registryService, IToastService toastService)
        {
            _registryService = registryService;
            _toastService = toastService;
            InitializeWatchlist();
            
            LoadRegistryCommand = new AsyncRelayCommand(LoadRegistryAsync);
        }

        private void InitializeWatchlist()
        {
            Watchlist.Add(new RegistryWatchEntry { Name = "User Startup", Hive = "HKCU", KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", ValueName = "", BaselineValue = "" });
            Watchlist.Add(new RegistryWatchEntry { Name = "Shell Context", Hive = "HKCU", KeyPath = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", ValueName = "", BaselineValue = null! });
        }

        public IAsyncRelayCommand LoadRegistryCommand { get; }

        public async Task LoadRegistryAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var parts = SearchPath.Split(new[] { '\\' }, 2);
                if (parts.Length < 1) return;

                string hive = parts[0];
                string path = parts.Length > 1 ? parts[1] : string.Empty;

                CurrentKey = await _registryService.GetKeyAsync(hive, path);
                if (CurrentKey != null)
                {
                    KeyDescription = await _registryService.GetKeyDescriptionAsync(path);
                    CurrentProvenance = await _registryService.GetProvenanceAsync(hive, path);
                }
                else
                {
                    _toastService.ShowError("Key not found or access denied.");
                }

                if (CommonTweaks.Count == 0)
                {
                    var tweaks = await _registryService.GetCommonTweaksAsync();
                    foreach (var t in tweaks) CommonTweaks.Add(t);
                }

                await RefreshWatchlist();
            }, "Querying Registry Hive...");
        }

        [RelayCommand]
        private async Task RefreshWatchlist()
        {
            await _registryService.CheckWatchlistDriftAsync(Watchlist.ToList());
        }

        [RelayCommand]
        private async Task ApplyTweak(RegistryOption option)
        {
            if (option?.Parent == null) return;
            var tweak = option.Parent;

            await ExecuteBusyActionAsync(async () =>
            {
                var currentValues = await _registryService.GetValuesAsync(tweak.Hive, tweak.KeyPath);
                var existingValue = currentValues.FirstOrDefault(v => v.Name == tweak.ValueName);

                UndoVault.Insert(0, new RegistryUndoEntry
                {
                    Hive = tweak.Hive,
                    KeyPath = tweak.KeyPath,
                    ValueName = tweak.ValueName,
                    OriginalValue = existingValue?.Value ?? null!,
                    Kind = tweak.Kind,
                    Description = tweak.Name
                });

                bool success = await _registryService.SetValueAsync(tweak.Hive, tweak.KeyPath, tweak.ValueName, option.Value, tweak.Kind);
                if (success) _toastService.ShowSuccess($"Applied: {tweak.Name}");
                else _toastService.ShowError($"Failed to apply {tweak.Name}");
                
                await LoadRegistryAsync();
            }, "Applying Intelligence Tweak...");
        }

        [RelayCommand]
        private async Task UndoChange(RegistryUndoEntry entry)
        {
            await ExecuteBusyActionAsync(async () =>
            {
                bool success = await _registryService.SetValueAsync(entry.Hive, entry.KeyPath, entry.ValueName, entry.OriginalValue, entry.Kind);
                if (success)
                {
                    UndoVault.Remove(entry);
                    _toastService.ShowInfo($"Reverted: {entry.Description}");
                    await LoadRegistryAsync();
                }
            }, "Rolling Back Change...");
        }

        [RelayCommand]
        private async Task ModifyValue(RegistryValueInfo valueInfo)
        {
            if (CurrentKey == null) return;

            string hive = GetHiveString(CurrentKey.Hive);
            string path = CurrentKey.FullPath.Split(new[] { '\\' }, 2)[1];

            var editWin = new Eternal.Views.Helpers.RegistryEditWindow(valueInfo);
            editWin.Owner = System.Windows.Application.Current.MainWindow;
            
            if (editWin.ShowDialog() == true)
            {
                var newValue = editWin.NewValue;

                await ExecuteBusyActionAsync(async () =>
                {
                    UndoVault.Insert(0, new RegistryUndoEntry
                    {
                        Hive = hive,
                        KeyPath = path,
                        ValueName = valueInfo.Name,
                        OriginalValue = valueInfo.Value,
                        Kind = valueInfo.Kind,
                        Description = $"Manual: {valueInfo.Name}"
                    });

                    bool success = await _registryService.SetValueAsync(hive, path, valueInfo.Name, newValue, valueInfo.Kind);
                    if (success) _toastService.ShowSuccess($"Modified: {valueInfo.Name}");
                    else _toastService.ShowError("Modification failed. Access denied.");
                    
                    await LoadRegistryAsync();
                }, "Committing Registry Change...");
            }
        }

        [RelayCommand]
        private async Task NavigateToSubKey(string subKeyName)
        {
            SearchPath = $"{SearchPath}\\{subKeyName}";
            await LoadRegistryAsync();
        }

        [RelayCommand]
        private async Task NavigateUp()
        {
            var lastSlash = SearchPath.LastIndexOf('\\');
            if (lastSlash > 0)
            {
                SearchPath = SearchPath.Substring(0, lastSlash);
                await LoadRegistryAsync();
            }
        }

        private string GetHiveString(RegistryHiveType hive)
        {
            return hive switch
            {
                RegistryHiveType.ClassesRoot => "HKEY_CLASSES_ROOT",
                RegistryHiveType.CurrentUser => "HKEY_CURRENT_USER",
                RegistryHiveType.LocalMachine => "HKEY_LOCAL_MACHINE",
                RegistryHiveType.Users => "HKEY_USERS",
                RegistryHiveType.CurrentConfig => "HKEY_CURRENT_CONFIG",
                _ => "HKEY_LOCAL_MACHINE"
            };
        }
    }
}
