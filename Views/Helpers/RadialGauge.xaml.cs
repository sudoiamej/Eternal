using System;
using System.Windows;

namespace Eternal.Views.Helpers
{
    public partial class RadialGauge : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(RadialGauge), new PropertyMetadata(0.0, OnValueChanged));

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(System.Windows.Media.Brush), typeof(RadialGauge), new PropertyMetadata(System.Windows.Media.Brushes.White));

        public static readonly DependencyProperty StrokeBackgroundProperty =
            DependencyProperty.Register("StrokeBackground", typeof(System.Windows.Media.Brush), typeof(RadialGauge), new PropertyMetadata(System.Windows.Media.Brushes.Gray));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(RadialGauge), new PropertyMetadata(10.0));

        public static readonly DependencyProperty GlowColorProperty =
            DependencyProperty.Register("GlowColor", typeof(System.Windows.Media.Color), typeof(RadialGauge), new PropertyMetadata(System.Windows.Media.Colors.White));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public System.Windows.Media.Brush Stroke
        {
            get => (System.Windows.Media.Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public System.Windows.Media.Brush StrokeBackground
        {
            get => (System.Windows.Media.Brush)GetValue(StrokeBackgroundProperty);
            set => SetValue(StrokeBackgroundProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public System.Windows.Media.Color GlowColor
        {
            get => (System.Windows.Media.Color)GetValue(GlowColorProperty);
            set => SetValue(GlowColorProperty, value);
        }

        public RadialGauge()
        {
            InitializeComponent();
            this.Loaded += (s, e) => UpdateGauge();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RadialGauge gauge)
            {
                gauge.UpdateGauge();
            }
        }

        private void UpdateGauge()
        {
            double percentage = Math.Clamp(Value, 0, 100);
            double angle = (percentage / 100.0) * 360;
            
            // WPF ArcSegment Math
            double radius = 90;
            System.Windows.Point center = new System.Windows.Point(100, 100);
            
            // Starting point is top (12 o'clock)
            double startAngle = -90;
            double endAngle = startAngle + angle;

            double endAngleRad = Math.PI * endAngle / 180.0;
            
            double x = center.X + radius * Math.Cos(endAngleRad);
            double y = center.Y + radius * Math.Sin(endAngleRad);

            ArcSegment.Point = new System.Windows.Point(x, y);
            ArcSegment.IsLargeArc = angle > 180;
            
            if (percentage >= 100)
            {
                // To avoid the arc disappearing when it's exactly 360 degrees
                ArcSegment.Point = new System.Windows.Point(center.X + radius * Math.Cos(Math.PI * (startAngle - 0.01) / 180.0), 
                                          center.Y + radius * Math.Sin(Math.PI * (startAngle - 0.01) / 180.0));
            }
        }
    }
}
