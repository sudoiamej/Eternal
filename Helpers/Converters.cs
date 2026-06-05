using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Eternal.Models;

namespace Eternal.Helpers
{
    public class ValueToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        public bool TreatZeroAsCollapsed { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = true;

            if (value is bool b)
            {
                isVisible = b;
            }
            else if (value is int i)
            {
                isVisible = i > 0 || !TreatZeroAsCollapsed;
            }
            else if (value == null)
            {
                isVisible = false;
            }

            if (Invert || (parameter as string) == "Inverse")
            {
                isVisible = !isVisible;
            }

            return isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string param && param == "Camera")
            {
                return value is bool b && b ? "Stop Feed" : "Start Feed";
            }
            if (value is bool val) return val ? "Enabled" : "Disabled";
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SizeFormatterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                string[] units = { "B", "KB", "MB", "GB", "TB" };
                double doubleBytes = bytes;
                int unitIndex = 0;
                while (doubleBytes >= 1024 && unitIndex < units.Length - 1)
                {
                    doubleBytes /= 1024;
                    unitIndex++;
                }
                return $"{doubleBytes:F1} {units[unitIndex]}";
            }
            return value?.ToString() ?? "0 B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class IntToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int i) return i > 0;
            return false;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class MultiBooleanToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool visible = true;
            foreach (var val in values)
            {
                if (val is bool b) visible = visible && b;
            }
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NumericValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                string numericPart = new string(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(str, c => char.IsDigit(c) || c == '.')));
                if (double.TryParse(numericPart, out double result))
                    return result;
            }
            return 0.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class TrustLevelToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TrustLevel level)
            {
                return level switch
                {
                    TrustLevel.Safe => System.Windows.Application.Current.Resources["SuccessBrush"],
                    TrustLevel.Warning => System.Windows.Application.Current.Resources["WarningBrush"],
                    TrustLevel.Critical => System.Windows.Application.Current.Resources["CriticalBrush"],
                    _ => System.Windows.Application.Current.Resources["TextSecondaryBrush"]
                };
            }
            return System.Windows.Application.Current.Resources["TextSecondaryBrush"];
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class SeverityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Services.System.ToastSeverity severity)
            {
                return severity switch
                {
                    Services.System.ToastSeverity.Success => System.Windows.Application.Current.Resources["SuccessBrush"],
                    Services.System.ToastSeverity.Warning => System.Windows.Application.Current.Resources["WarningBrush"],
                    Services.System.ToastSeverity.Error => System.Windows.Application.Current.Resources["CriticalBrush"],
                    _ => System.Windows.Application.Current.Resources["InfoBrush"]
                };
            }
            return System.Windows.Application.Current.Resources["InfoBrush"];
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class TrustLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TrustLevel level)
            {
                return level switch
                {
                    TrustLevel.Safe => System.Windows.Application.Current.Resources["SuccessColor"],
                    TrustLevel.Warning => System.Windows.Application.Current.Resources["WarningColor"],
                    TrustLevel.Critical => System.Windows.Application.Current.Resources["CriticalColor"],
                    _ => System.Windows.Application.Current.Resources["TextSecondaryColor"]
                };
            }
            return System.Windows.Application.Current.Resources["TextSecondaryColor"];
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BooleanToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == DependencyProperty.UnsetValue) return FontAwesome.WPF.FontAwesomeIcon.Question;

            if (parameter is string param)
            {
                var icons = param.Split(':');
                if (icons.Length == 2)
                {
                    bool isTrue = value is bool b && b;
                    string iconName = isTrue ? icons[0] : icons[1];
                    if (Enum.TryParse(typeof(FontAwesome.WPF.FontAwesomeIcon), iconName, out var icon))
                    {
                        return icon!;
                    }
                }
            }
            return FontAwesome.WPF.FontAwesomeIcon.Question;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NumericComparisonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val && parameter is string param)
            {
                if (param.StartsWith(">") && double.TryParse(param.Substring(1), out double thresh))
                    return val > thresh;
            }
            return false;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class CategoryToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string param)
            {
                if (param == "Integrity")
                {
                    bool healthy = value is bool b && b;
                    return healthy ? FontAwesome.WPF.FontAwesomeIcon.CheckCircleOutline : FontAwesome.WPF.FontAwesomeIcon.ExclamationCircle;
                }
                if (param == "IntegrityBrush")
                {
                    bool healthy = value is bool b && b;
                    return healthy ? System.Windows.Application.Current.Resources["SuccessBrush"] : System.Windows.Application.Current.Resources["CriticalBrush"];
                }
                if (param == "Toast")
                {
                    if (value is Services.System.ToastSeverity severity)
                    {
                        return severity switch
                        {
                            Services.System.ToastSeverity.Success => FontAwesome.WPF.FontAwesomeIcon.CheckCircle,
                            Services.System.ToastSeverity.Warning => FontAwesome.WPF.FontAwesomeIcon.ExclamationTriangle,
                            Services.System.ToastSeverity.Error => FontAwesome.WPF.FontAwesomeIcon.TimesCircle,
                            _ => FontAwesome.WPF.FontAwesomeIcon.InfoCircle
                        };
                    }
                    return FontAwesome.WPF.FontAwesomeIcon.InfoCircle;
                }
                if (param == "Camera")
                {
                    return value is bool b && b ? FontAwesome.WPF.FontAwesomeIcon.StopCircle : FontAwesome.WPF.FontAwesomeIcon.PlayCircle;
                }
            }

            if (value is ProcessCategory cat)
            {
                return cat switch
                {
                    ProcessCategory.Apps => FontAwesome.WPF.FontAwesomeIcon.WindowRestore,
                    ProcessCategory.Background => FontAwesome.WPF.FontAwesomeIcon.Cog,
                    ProcessCategory.Windows => FontAwesome.WPF.FontAwesomeIcon.Windows,
                    _ => FontAwesome.WPF.FontAwesomeIcon.Question
                };
            }
            return FontAwesome.WPF.FontAwesomeIcon.Question;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ProportionalHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double val = 0;
            if (value is double d) val = d;
            else if (value is int i) val = i;
            else if (value is float f) val = f;
            else if (value is long l) val = l;

            if (double.TryParse(parameter as string, out double p))
            {
                // If the parameter is a small multiplier (e.g. 0.1 for Display scaling)
                if (p < 0.5) return val * p;
                
                // Otherwise assume it's a max height for a bar chart (e.g. 80 for Network)
                // Assuming val is 0-100 or similar, scale to height. 
                return Math.Max(2.0, Math.Min(p, val * (p / 50.0))); 
            }
            return 2.0; // Min visible bar
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class EqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;
            if (values[0] == null && values[1] == null) return true;
            if (values[0] == null || values[1] == null) return false;
            return values[0].Equals(values[1]);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class SpeedTestProgressWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double pct = 0;
            if (value is int i) pct = i;
            else if (value is double d) pct = d;
            
            double maxWidth = 250.0;
            if (parameter != null && double.TryParse(parameter.ToString(), out double customWidth))
            {
                maxWidth = customWidth;
            }
            
            double width = (pct / 100.0) * maxWidth;
            return Math.Max(0.0, Math.Min(maxWidth, width));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ProportionalPartitionWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return 150.0;
            
            try
            {
                double partSize = System.Convert.ToDouble(values[0]);
                double diskSize = System.Convert.ToDouble(values[1]);
                
                if (diskSize <= 0) return 150.0;
                
                double pct = partSize / diskSize;
                
                double totalMapWidth = 720.0;
                if (parameter != null && double.TryParse(parameter.ToString(), out double customWidth))
                {
                    totalMapWidth = customWidth;
                }
                
                return Math.Max(95.0, pct * totalMapWidth);
            }
            catch
            {
                return 150.0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
