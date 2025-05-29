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
    /// <summary>
    /// When values are bound to a text value, 2 doubles, and a boolean, and the output is bound as
    /// a brush, this will, if the boolean is true, parse the text as a double and check whether it is
    /// above, below, or within the 2 doubles. If the boolean is false, the brush is transparent.
    /// If the text is below the first double, the brush is blue. If the text is above the second double,
    /// the brush is red. Otherwise, the brush is green.
    /// </summary>
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
