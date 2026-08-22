using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MediaColor = System.Windows.Media.Color;

namespace Eternal.Views.Helpers
{
    public partial class OrbitalVisualizerWindow : Window
    {
        private readonly DispatcherTimer _animationTimer;
        private double _angle = 0;
        private readonly int _coreCount;
        private readonly PerformanceCounter? _cpuCounter;
        private readonly Random _rand = new Random();

        public OrbitalVisualizerWindow()
        {
            InitializeComponent();
            _coreCount = Environment.ProcessorCount;
            CoresCountText.Text = $"LOGICAL CORES: {_coreCount}";

            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue();
            }
            catch { }

            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();

            this.Unloaded += (s, e) => _animationTimer.Stop();
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            _angle += 0.03;
            float totalLoad = 25f;

            try
            {
                if (_cpuCounter != null)
                {
                    totalLoad = _cpuCounter.NextValue();
                }
            }
            catch { }

            AverageLoadText.Text = $"AVG LOAD: {totalLoad:F1}%";

            OrbitalCanvas.Children.Clear();

            double centerX = OrbitalCanvas.ActualWidth / 2;
            double centerY = OrbitalCanvas.ActualHeight / 2;

            if (centerX <= 0 || centerY <= 0) return;

            // Draw Core Center Reactor
            var centerNode = new Ellipse
            {
                Width = 40 + (totalLoad * 0.2),
                Height = 40 + (totalLoad * 0.2),
                Fill = new SolidColorBrush(MediaColor.FromRgb(0, 120, 212)),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = MediaColor.FromRgb(0, 120, 212),
                    BlurRadius = 20 + totalLoad * 0.3,
                    ShadowDepth = 0,
                    Opacity = 0.8
                }
            };
            Canvas.SetLeft(centerNode, centerX - (centerNode.Width / 2));
            Canvas.SetTop(centerNode, centerY - (centerNode.Height / 2));
            OrbitalCanvas.Children.Add(centerNode);

            // Draw Orbital Rings & Nodes for Core Telemetry
            int rings = Math.Max(1, _coreCount / 2);
            for (int r = 1; r <= rings; r++)
            {
                double radius = r * (Math.Min(centerX, centerY) * 0.75 / rings);

                var orbitLine = new Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2,
                    Stroke = new SolidColorBrush(MediaColor.FromArgb(40, 0, 120, 212)),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(orbitLine, centerX - radius);
                Canvas.SetTop(orbitLine, centerY - radius);
                OrbitalCanvas.Children.Add(orbitLine);

                // Add Core Node Particles
                int coresOnRing = Math.Min(4, _coreCount);
                for (int i = 0; i < coresOnRing; i++)
                {
                    double nodeAngle = _angle * (r % 2 == 0 ? 1 : -1) + (i * (2 * Math.PI / coresOnRing));
                    double nodeX = centerX + radius * Math.Cos(nodeAngle);
                    double nodeY = centerY + radius * Math.Sin(nodeAngle);

                    double nodeSize = 12 + (_rand.NextDouble() * 4);
                    byte greenVal = (byte)Math.Min(255, 100 + (totalLoad * 1.5));
                    byte redVal = (byte)Math.Min(255, totalLoad * 2.2);

                    var coreParticle = new Ellipse
                    {
                        Width = nodeSize,
                        Height = nodeSize,
                        Fill = new SolidColorBrush(MediaColor.FromRgb(redVal, greenVal, 220)),
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = MediaColor.FromRgb(redVal, greenVal, 255),
                            BlurRadius = 10,
                            ShadowDepth = 0,
                            Opacity = 0.9
                        }
                    };

                    Canvas.SetLeft(coreParticle, nodeX - (nodeSize / 2));
                    Canvas.SetTop(coreParticle, nodeY - (nodeSize / 2));
                    OrbitalCanvas.Children.Add(coreParticle);
                }
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
