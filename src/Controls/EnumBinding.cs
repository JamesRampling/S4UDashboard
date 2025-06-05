using System;
using System.Collections.Generic;

using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace S4UDashboard.Controls;

/// <summary>A markup extension for providing enum variants to bindings.</summary>
public class EnumBinding(Type type) : MarkupExtension
{
    public override CompiledBindingExtension ProvideValue(IServiceProvider serviceProvider) =>
        new()
        {
            Mode = BindingMode.OneTime,
            DataType = typeof(IEnumerable<>).MakeGenericType(type),
            Source = Enum.GetValues(type),
        };
}
