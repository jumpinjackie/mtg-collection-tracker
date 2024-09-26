using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Collections;
using System.Globalization;

namespace MtgCollectionTracker.Services;

public class CollectionNotEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ICollection c)
            return c.Count > 0;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
