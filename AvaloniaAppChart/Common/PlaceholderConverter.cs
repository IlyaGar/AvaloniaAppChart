using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace AvaloniaAppChart.Common
{
    public class PlaceholderConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value?.ToString()) ? "input" : value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
