using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace HospitalManagementAvolonia.Helpers
{
    /// <summary>Returns 60.0 when sidebar is collapsed, 220.0 when expanded.</summary>
    public class BoolToSidebarWidthConverter : IValueConverter
    {
        public static BoolToSidebarWidthConverter Instance { get; } = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? 60.0 : 220.0;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>Returns "Collapsed" when sidebar is collapsed, "Visible" otherwise.</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public static BoolToVisibilityConverter Instance { get; } = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? false : true;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
