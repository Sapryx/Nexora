using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Nexora.Converters;

public class IconNameToSourceConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is not string name || string.IsNullOrWhiteSpace(name))
            return null;

        return $"avares://Nexora/Assets/Icons/{name}.svg";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter,
        CultureInfo culture)
        => throw new NotSupportedException();
}
