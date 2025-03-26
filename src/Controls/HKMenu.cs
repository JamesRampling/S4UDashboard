using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace S4UDashboard.Controls;

// Avalonia's Menu control has a bug (Issue #2441) where MenuItems' HotKeys aren't
// registered until the menu is interacted with. This works around it by adding the
// hotkeys directly to the window when the control is attached. It also uses InputGesture
// as the source of the hotkey's keybind, so that it only has to be specified only once.
public class HKMenu : Menu
{
    // Retain normal Menu styling.
    protected override Type StyleKeyOverride => typeof(Menu);

    private ImmutableHashSet<KeyBinding>? _keyBindings;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var window = this.FindAncestorOfType<Window>();
        if (window == null) return;

        _keyBindings = TraverseItems(this.LogicalChildren.OfType<MenuItem>())
            .Where(i => i.InputGesture != null && i.Command != null)
            .Select(i => new KeyBinding()
            {
                Gesture = i.InputGesture!,
                Command = i.Command!,
            }
            ).ToImmutableHashSet();

        window.KeyBindings.AddRange(_keyBindings);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_keyBindings == null) return;

        var window = this.FindAncestorOfType<Window>();
        if (window == null) return;

        window.KeyBindings.RemoveAll(kb => _keyBindings.Contains(kb));
        _keyBindings = null;
    }

    private static IEnumerable<MenuItem> TraverseItems(IEnumerable<MenuItem> items)
    {
        foreach (var item in items)
        {
            yield return item;

            var children = item.GetLogicalChildren().OfType<MenuItem>();
            foreach (var ancestor in TraverseItems(children)) yield return ancestor;
        }
    }
}
