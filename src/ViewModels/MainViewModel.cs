using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using S4UDashboard.Model;
using S4UDashboard.Reactive;
using S4UDashboard.Views;

using Tabalonia.Controls;

namespace S4UDashboard.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ReactiveCell<int> SelectedTabIndex { get; } = new(-1);
    public ComputedCell<FileTabViewModel?> SelectedTab { get; }

    public ReactiveList<FileTabViewModel> TabList { get; } = [];

    public ReactiveCommand QuitApp { get; } = new(
        () => ServiceProvider.GetService<MainWindow>() is not null,
        _ => ServiceProvider.ExpectService<MainWindow>().Close());

    public ReactiveCommand GoNextTab { get; }
    public ReactiveCommand GoPrevTab { get; }
    public ReactiveCommand CloseSelectedTab { get; }

    public ReactiveCommand OpenFileDialog { get; }
    public ReactiveCommand SaveCurrent { get; }
    public ReactiveCommand SaveAsDialog { get; }
    public ReactiveCommand SaveAll { get; }

    public MainViewModel()
    {
        var window = ServiceProvider.GetService<MainWindow>();
        if (window != null) window.Closing += HandleClosing;

        SelectedTab = new(() => TabList.ElementAtOrDefault(SelectedTabIndex.Value));

        bool AnyTabOpen() => SelectedTab.Value is not null;
        void SelectTab(int index) => SelectedTabIndex.Value = Math.Clamp(index, 0, TabList.Count - 1);

        GoNextTab = new(AnyTabOpen, _ => SelectTab(SelectedTabIndex.Value + 1));
        GoPrevTab = new(AnyTabOpen, _ => SelectTab(SelectedTabIndex.Value - 1));
        CloseSelectedTab = new(AnyTabOpen, _ =>
        {
            var initial = SelectedTabIndex.Value;
            if (TryCloseTab(SelectedTab.Value!) && TabList.Count > 0)
                SelectTab(initial);
        });

        OpenFileDialog = new(() => true, async _ =>
        {
            var storage = ServiceProvider.ExpectService<IStorageProvider>();

            var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open File",
                AllowMultiple = true,
                FileTypeFilter = [FileTabViewModel.SDSFileType],
            });

            if (files == null) return;

            var locations = files.Select(f => new FileLocation(f.Path));
            var fileLocations = TabList
                .Select(vm => vm.Location.Value)
                .Where(loc => loc is FileLocation)
                .Select(loc => (FileLocation)loc);
            var unique = locations.Except(fileLocations);

            if (locations.Count() == 1 && !unique.Any())
                SelectTab(TabList.FindIndex(
                    vm => vm.Location.Value is FileLocation floc && floc == locations.Single()));
            else if (unique.Any())
            {
                TabList.AddRange(unique.Select(loc => FileTabViewModel.FromLocation(loc)));
                SelectTab(TabList.Count);
            }
        });

        SaveCurrent = new(
            () => AnyTabOpen() && SelectedTab.Value!.Location.Value.IsPhysical,
            _ => SelectedTab.Value?.SaveCurrent());

        SaveAsDialog = new(AnyTabOpen, _ => SelectedTab.Value?.SaveAs());

        SaveAll = new(() => TabList.Where(f => f.Dirty.Value).Any(), _ =>
        {
            foreach (var tab in TabList.Where(f => f.Dirty.Value)) tab.SaveCurrent();
        });
    }

    public void GenerateSample()
    {
        var loc = DataProcessing.Instance.AddSampleDataset(
            new AnnotatedDataModel
            {
                AnnotatedName = "Foo",
            },
            new SensorDataModel
            {
                MeasurementIdentifier = "temperature",
                SensorNames = ["bedroom", "office", "kitchen"],
                SampleTimes = [new(2025, 4, 1), new(2025, 4, 2), new(2025, 4, 3)],
                Samples = new([26, 22, 28, 21, 23, 21, 29, 31, 32], 3, 3),
            });
        TabList.Add(FileTabViewModel.FromLocation(loc));
    }

    private bool TryCloseTab(FileTabViewModel tab)
    {
        // TODO: confirm dialog
        //if (tab.Dirty.Value) return false;

        var idx = TabList.IndexOf(tab);
        var front = idx != -1;

        if (front) TabList.RemoveAt(idx);
        var back = DataProcessing.Instance.Datasets.Remove(tab.Location.Value);

        Trace.Assert(front == back, "frontend & backend state differed");
        return front;
    }

    public bool TryCloseTabCommand(object o) =>
        o is DragTabItem item && item.Header is FileTabViewModel tab && TryCloseTab(tab);

    // Handles the event when the window was requested to close.
    private void HandleClosing(object? o, WindowClosingEventArgs e)
    {
        e.Cancel = true;

        if (TryExit())
        {
            var window = ServiceProvider.ExpectService<MainWindow>();
            window.Closing -= HandleClosing;
            window.Close();
        }
    }

    // Handles actions that should be performed before the window is closed.
    // Returns whether or not the window should actually be closed, which
    // may not be true if the user cancelled the close in a dialog.
    private bool TryExit()
    {
        // TODO: confirm dialog
        //if (TabList.Where(t => t.Dirty.Value).Any()) return false;

        return true;
    }
}
