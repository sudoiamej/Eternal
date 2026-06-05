using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Eternal.Views.Helpers
{
    public class TransitioningContentControl : ContentControl
    {
        private ContentPresenter? _mainPresenter;

        public TransitioningContentControl()
        {
            DefaultStyleKey = typeof(TransitioningContentControl);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _mainPresenter = GetTemplateChild("PART_MainPresenter") as ContentPresenter;
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            if (_mainPresenter == null || newContent == null) return;

            bool disableAnimations = 
                Environment.GetCommandLineArgs().Any(arg => arg.Equals("--no-animation", StringComparison.OrdinalIgnoreCase) || arg.Equals("--disable-animations", StringComparison.OrdinalIgnoreCase)) ||
                Environment.GetEnvironmentVariable("DISABLE_ANIMATIONS") == "true" ||
                Environment.GetEnvironmentVariable("ANTIGRAVITY") == "true";

            if (disableAnimations)
            {
                _mainPresenter.Opacity = 1;
                _mainPresenter.RenderTransform = new TranslateTransform(0, 0);
                return;
            }

            // Simple, stable, and high-performance animation
            // This prevents "ghosting" by only animating the incoming view
            
            DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            DoubleAnimation slideUp = new DoubleAnimation(15, 0, TimeSpan.FromSeconds(0.4))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            TranslateTransform tt = new TranslateTransform(0, 15);
            _mainPresenter.RenderTransform = tt;
            _mainPresenter.Opacity = 0;

            _mainPresenter.BeginAnimation(OpacityProperty, fadeIn);
            tt.BeginAnimation(TranslateTransform.YProperty, slideUp);
        }
    }
}
