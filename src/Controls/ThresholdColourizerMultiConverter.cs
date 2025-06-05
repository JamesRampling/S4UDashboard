using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace S4UDashboard.Controls;

/// <summary>
/// A multi value converter that takes the content of a cell, the lower and upper bounds,
/// and the visualise thresholds toggle and calculates the corresponding colour brush.
/// </summary>
public class ThresholdColourizerMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values?.Count != 4 || !targetType.IsAssignableFrom(typeof(ImmutableSolidColorBrush)))
            throw new NotSupportedException();

        if (values[0] is not string cell ||
            (values[1] is not double && values[1] is not UnsetValueType && values[1] is not null) ||
            (values[2] is not double && values[2] is not UnsetValueType && values[2] is not null) ||
            values[3] is not bool vf)
            throw new NotSupportedException();

        if (!double.TryParse(cell, out var value)) throw new NotSupportedException();

        if (!vf) return Brushes.Transparent;
        if (values[1] is double lower && value <= lower) return Brushes.Blue;
        if (values[2] is double upper && value >= upper) return Brushes.Red;
        return Brushes.Green;
    }
}
