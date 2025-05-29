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

/// <summary>
/// A variant of <c>Menu</c> that registers each item's input gesture as window keybindings
/// when it is attached to the visual tree.
/// </summary>
public class HKMenu : Menu
{
    // Retain normal Menu styling.
    protected override Type StyleKeyOverride => typeof(Menu);

    /// <summary>The list of registered hotkeys.</summary>
    private ImmutableHashSet<KeyBinding>? _keyBindings;

    /// <summary>Performs the registration of input gestures as window hotkeys.</summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var window = this.FindAncestorOfType<Window>();
        if (window == null) return;

        _keyBindings = this.GetLogicalDescendants().OfType<MenuItem>()
            .Where(i => i.InputGesture != null && i.Command != null)
            .Select(i => new KeyBinding()
            {
                Gesture = i.InputGesture!,
                Command = i.Command!,
                /* Despite not being nullable CommandParameter is fine being null. */
                CommandParameter = i.CommandParameter!,
            }).ToImmutableHashSet();

        window.KeyBindings.AddRange(_keyBindings);
    }

    /// <summary>Removes all previously registered hotkeys from the window.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (_keyBindings == null) return;

        var window = this.FindAncestorOfType<Window>();
        if (window == null) return;

        window.KeyBindings.RemoveAll(kb => _keyBindings.Contains(kb));
        _keyBindings = null;
    }
}
