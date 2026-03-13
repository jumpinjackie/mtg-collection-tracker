using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MtgCollectionTracker.Services;

public class PhaseIndexToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int currentPhaseIndex &&
            int.TryParse(parameter?.ToString(), out var phaseIndex) &&
            currentPhaseIndex == phaseIndex)
        {
            return Brushes.DarkOrange;
        }

        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }
}
