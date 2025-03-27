using System;
using System.Diagnostics;

using Avalonia.Controls;

using S4UDashboard.Reactive;

namespace S4UDashboard.ViewModels;

public class MainViewModel : ViewModelBase
{
    public Window? MainWindow { get; }

    public ReactiveList<FileTabViewModel> OpenFiles { get; } = [new()];
    public ComputedCell<bool> AnyOpenFiles { get; }
    public ReactiveCell<int> SelectedTabIndex { get; } = new(-1);

    public MainViewModel(Window? mainWindow)
    {
        if ((MainWindow = mainWindow) != null) MainWindow.Closing += HandleClosing;

        AnyOpenFiles = new(() => OpenFiles.Count != 0);
    }
    public MainViewModel() : this(null) { }

    public void MakeNew() => OpenFiles.Add(new());

    private void IfAnyTabs(Action action)
    {
        if (AnyOpenFiles.Value) action();
    }

    public void SelectTab(int index) => IfAnyTabs(() => SelectedTabIndex.Value = Math.Clamp(index, 0, OpenFiles.Count - 1));
    public void GoNextTab() => SelectTab(SelectedTabIndex.Value + 1);
    public void GoPrevTab() => SelectTab(SelectedTabIndex.Value - 1);
    public void CloseSelectedTab() => IfAnyTabs(() =>
    {
        var initial = SelectedTabIndex.Value;
        OpenFiles.RemoveAt(initial);
        SelectTab(initial);
    });

    // Acts as though the window was requested to close.
    public void QuitApp() => MainWindow?.Close();

    // Handles the event when the window was requested to close.
    private void HandleClosing(object? o, WindowClosingEventArgs e)
    {
        e.Cancel = true;

        if (TryExit())
        {
            MainWindow!.Closing -= HandleClosing;
            MainWindow!.Close();
        }
    }

    // Handles actions that should be performed before the window is closed.
    // Returns whether or not the window should actually be closed, which
    // may not be true if the user cancelled the close in a dialog.
    private bool TryExit()
    {
        return true;
    }
}
