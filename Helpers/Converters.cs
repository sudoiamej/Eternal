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
            if (value is bool b) return b ? "Enabled" : "Disabled";
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
            if (value is double val && double.TryParse(parameter as string, out double max))
            {
                // Assuming val is 0-100 or similar, scale to height. 
                // Adjust factor based on typical Mbps expectations.
                return Math.Min(max, val * (max / 50.0)); 
            }
            return 2.0; // Min visible bar
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
