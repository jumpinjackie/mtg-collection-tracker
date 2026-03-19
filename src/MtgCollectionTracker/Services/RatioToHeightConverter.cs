using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace MtgCollectionTracker.Services;

/// <summary>Converts a ratio (0.0–1.0) to a pixel height by multiplying by the parameter (max height).</summary>
public class RatioToHeightConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double ratio = value is double d ? d : 0.0;
        double maxHeight = 200.0;

        if (parameter is string paramStr)
            double.TryParse(paramStr, NumberStyles.Any, CultureInfo.InvariantCulture, out maxHeight);
        else if (parameter is double paramDouble)
            maxHeight = paramDouble;

        return Math.Max(1.0, ratio * maxHeight);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
