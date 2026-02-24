using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace MyGameApp.ViewModels
{
    public class EqualityToBoolConverter : IValueConverter
    {
        public string? Expected { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isMatch = value?.ToString() == Expected;

            if (parameter?.ToString() == "bg")
                return isMatch ? "#454545" : "Transparent";
                
            if (parameter?.ToString() == "fg")
                return isMatch ? "#FFFFFF" : "#B0B0B0";

            return isMatch;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}