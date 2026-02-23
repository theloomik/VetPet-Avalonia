using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MyGameApp.ViewModels
{
    /// <summary>
    /// Конвертер для вкладок.
    /// ConverterParameter:
    ///   (немає / null) → bool (IsVisible)
    ///   "bg"           → IBrush (фон кнопки: активний = #4A4A4A, неактивний = #3A3A3A)
    ///   "fg"           → IBrush (колір тексту: активний = White, неактивний = #888888)
    ///
    /// Використання: <vm:EqualityToBoolConverter x:Key="IsTab0" Expected="0"/>
    /// </summary>
    public class EqualityToBoolConverter : IValueConverter
    {
        public int Expected { get; set; }

        private static readonly IBrush BgActive   = SolidColorBrush.Parse("#505050");
        private static readonly IBrush BgInactive = SolidColorBrush.Parse("#3A3A3A");
        private static readonly IBrush FgActive   = SolidColorBrush.Parse("#FFFFFF");
        private static readonly IBrush FgInactive = SolidColorBrush.Parse("#888888");

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool match = value is int i && i == Expected;
            var param = parameter as string;

            if (param == "bg") return match ? BgActive : BgInactive;
            if (param == "fg") return match ? FgActive : FgInactive;
            return (object)match;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
