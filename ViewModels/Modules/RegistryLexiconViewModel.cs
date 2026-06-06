using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Eternal.Services.System;

namespace Eternal.ViewModels.Modules
{
    public partial class RegistryLexiconViewModel : BaseViewModel
    {
        private readonly IRegistryLexiconService _lexiconService;
        private readonly IToastService _toastService;

        [ObservableProperty] private ObservableCollection<LexiconItemDelta> _deltas = new();
        [ObservableProperty] private int _driftCount = 0;

        public RegistryLexiconViewModel(IRegistryLexiconService lexiconService, IToastService toastService)
        {
            _lexiconService = lexiconService;
            _toastService = toastService;
            Title = "Registry Lexicon Audit";
        }

        [RelayCommand]
        public async Task LoadAuditAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var list = await _lexiconService.AnalyzeSystemDriftAsync();
                Deltas.Clear();
                foreach (var item in list)
                {
                    Deltas.Add(item);
                }
                DriftCount = Deltas.Count(d => d.IsDrifted);
            }, "Analyzing Registry DNA baseline...");
        }

        [RelayCommand]
        public async Task RealignSystemAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                var drifted = Deltas.Where(d => d.IsDrifted).ToList();
                if (drifted.Count == 0)
                {
                    _toastService.ShowInfo("No system drift detected. Realignment not required.");
                    return;
                }

                await _lexiconService.RealignSystemAsync(drifted);
                await LoadAuditAsync();
                _toastService.ShowSuccess("Registry realignment successful. Baseline restored.");
            }, "Realigning system configurations...");
        }
    }
}
