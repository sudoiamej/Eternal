using System;
using System.Windows;
using System.Windows.Media;

namespace Eternal.Services.System
{
    public class WindowsScalingService : IScalingService
    {
        private double _dpiScale = 1.0;
        private double _uiScale = 1.0;
        private double _fontScale = 1.0;

        public double DpiScale => _dpiScale;
        public double UiScale => _uiScale;
        public double FontScale => _fontScale;

        // Effective UI scale combines system DPI and user window scale to determine layout metrics
        public double EffectiveUiScale => _uiScale / _dpiScale;

        // Effective font scale is the user font scale modifier
        public double EffectiveFontScale => _fontScale;

        public event EventHandler? ScalingChanged;

        public void Initialize(double dpiScale)
        {
            _dpiScale = dpiScale > 0 ? dpiScale : 1.0;
            TriggerScalingChanged();
        }

        public void UpdateDpiScale(double dpiScale)
        {
            if (Math.Abs(_dpiScale - dpiScale) > 0.001 && dpiScale > 0)
            {
                _dpiScale = dpiScale;
                TriggerScalingChanged();
            }
        }

        public void UpdateScales(double uiScale, double fontScale)
        {
            bool changed = false;
            if (Math.Abs(_uiScale - uiScale) > 0.001 && uiScale >= 1.0)
            {
                _uiScale = uiScale;
                changed = true;
            }
            if (Math.Abs(_fontScale - fontScale) > 0.001 && fontScale >= 1.0)
            {
                _fontScale = fontScale;
                changed = true;
            }

            if (changed)
            {
                TriggerScalingChanged();
            }
        }

        private void TriggerScalingChanged()
        {
            // Update global resources for FontSizes
            var app = global::System.Windows.Application.Current;
            if (app != null)
            {
                app.Resources["GlobalFontScale"] = _fontScale;
                app.Resources["H1FontSize"] = 28 * _fontScale;
                app.Resources["H2FontSize"] = 18 * _fontScale;
                app.Resources["BodyFontSize"] = 11 * _fontScale;
                app.Resources["CaptionFontSize"] = 11 * _fontScale;
            }

            ScalingChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
