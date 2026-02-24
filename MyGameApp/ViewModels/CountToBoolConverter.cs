using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MyGameApp.ViewModels
{
    public class CountToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var invert = string.Equals(parameter?.ToString(), "invert", StringComparison.OrdinalIgnoreCase);

            var count = value switch
            {
                int i => i,
                long l => (int)l,
                _ => 0
            };

            var result = count > 0;
            return invert ? !result : result;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
