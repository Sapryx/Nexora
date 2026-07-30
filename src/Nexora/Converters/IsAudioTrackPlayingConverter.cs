using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Nexora.Converters;

public class IsAudioTrackPlayingConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is not bool isPlaying)
        {
            return null;
        }

        return isPlaying ? "||" : ">";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}