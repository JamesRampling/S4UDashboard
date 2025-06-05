using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;

using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Models;

namespace S4UDashboard;

/// <summary>A helper class to show popup alerts.</summary>
public class AlertService
{
    /// <summary>Whether or not an alert is already on screen.</summary>
    private bool _busy = false;

    /// <summary>Creates a popup with custom buttons.</summary>
    /// <param name="title">The title of the popup.</param>
    /// <param name="header">The header of the popup.</param>
    /// <param name="message">The message of the popup.</param>
    /// <param name="buttons">The buttons of the popup.</param>
    private static IMsBox<string> MakePopup(
        string? title,
        string? header,
        string? message,
        IEnumerable<ButtonDefinition> buttons
    )
    {
        var config = new MsBox.Avalonia.Dto.MessageBoxCustomParams
        {
            ButtonDefinitions = buttons,
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInCenter = true,
            CanResize = false,
            Topmost = true,
        };

        if (title is not null) config.ContentTitle = title;
        if (header is not null) config.ContentHeader = header;
        if (message is not null) config.ContentMessage = message;

        return MessageBoxManager.GetMessageBoxCustom(config);
    }

    /// <summary>Creates a popup with custom buttons and a cancel button.</summary>
    /// <param name="title">The title of the popup.</param>
    /// <param name="header">The header of the popup.</param>
    /// <param name="message">The message of the popup.</param>
    /// <param name="buttons">The buttons of the popup.</param>
    public async Task<string?> PopupWithCancel(string? title, string? header, string? message, IEnumerable<string> buttons)
    {
        if (_busy) return null;

        var box = MakePopup(title, header, message, [
            new ButtonDefinition { Name = "Cancel", IsCancel = true, IsDefault = true },
            .. buttons.Select(s => new ButtonDefinition { Name = s }),
        ]);

        _busy = true;
        var result = await box.ShowAsync();
        _busy = false;

        return result;
    }

    /// <summary>Creates an alert with only a close button.</summary>
    /// <param name="title">The title of the popup.</param>
    /// <param name="header">The header of the popup.</param>
    /// <param name="message">The message of the popup.</param>
    public async void Alert(string? title, string? header, string? message)
    {
        if (_busy) return;

        var box = MakePopup(title, header, message, [
            new ButtonDefinition { Name = "Close", IsDefault = true },
        ]);

        _busy = true;
        await box.ShowAsync();
        _busy = false;
    }
}
