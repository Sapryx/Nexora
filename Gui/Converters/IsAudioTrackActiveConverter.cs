using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Gui.Converters;

public class IsAudioTrackActiveConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is not bool isActive)
        {
            return null;
        }

        return isActive ? 1f : 0f;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
