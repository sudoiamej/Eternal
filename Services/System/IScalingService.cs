using System;

namespace Eternal.Services.System
{
    public interface IScalingService
    {
        double DpiScale { get; }
        double UiScale { get; }
        double FontScale { get; }
        double EffectiveUiScale { get; }
        double EffectiveFontScale { get; }

        event EventHandler? ScalingChanged;

        void Initialize(double dpiScale);
        void UpdateDpiScale(double dpiScale);
        void UpdateScales(double uiScale, double fontScale);
    }
}
