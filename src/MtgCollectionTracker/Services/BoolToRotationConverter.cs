using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MtgCollectionTracker.Services;

/// <summary>
/// Converts a boolean value to a rotation angle (0° or 90°) for card tapping
/// </summary>
public class BoolToRotationConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isTapped)
        {
            return isTapped ? 90.0 : 0.0;
        }
        return 0.0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException("BoolToRotationConverter does not support ConvertBack");
    }
}
