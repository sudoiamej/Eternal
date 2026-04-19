using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

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
}
