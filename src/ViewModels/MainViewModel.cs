using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using MsBox.Avalonia;
using MsBox.Avalonia.Models;

using S4UDashboard.Model;
using S4UDashboard.Reactive;
using S4UDashboard.Views;

using Tabalonia.Controls;

namespace S4UDashboard.ViewModels;

public class MainViewModel : ViewModelBase
{
    private bool _closingTab = false;
    private bool _closingWindow = false;

    public ReactiveCell<int> SelectedTabIndex { get; } = new(-1);
    public ReactiveCell<FileTabViewModel?> SelectedTab { get; } = new(null);

    public ObservableCollection<FileTabViewModel> TabList { get; } = [];

    public ReactiveCell<SortMode> TabsSortMode { get; } = new(SortMode.Unsorted);
    public ComputedCell<bool> TabsAreSorted { get; }

    public ReactiveCommand QuitApp { get; } = new(
        () => ServiceProvider.GetService<MainWindow>() is not null,
        _ => ServiceProvider.ExpectService<MainWindow>().Close());

    public void CloseTabCommand(object o)
    {
        if (o is not DragTabItem item || item.Header is not FileTabViewModel tab) return;
        CloseTab(tab);
    }

    public ReactiveCommand GoNextTab { get; }
    public ReactiveCommand GoPrevTab { get; }
    public ReactiveCommand CloseSelectedTab { get; }

    public ReactiveCommand SaveCurrent { get; }
    public ReactiveCommand SaveAsDialog { get; }
    public ReactiveCommand SaveAll { get; }

    public MainViewModel()
    {
        var window = ServiceProvider.GetService<MainWindow>();
        if (window != null) window.Closing += HandleClosing;

        TabsAreSorted = new(() => TabsSortMode.Value != SortMode.Unsorted);

        bool AnyTabOpen() => SelectedTab.Value is not null;

        GoNextTab = new(AnyTabOpen, _ => SelectTab(SelectedTabIndex.Value + 1));
        GoPrevTab = new(AnyTabOpen, _ => SelectTab(SelectedTabIndex.Value - 1));
        CloseSelectedTab = new(AnyTabOpen, _ => CloseTab(SelectedTab.Value!));

        SaveCurrent = new(AnyTabOpen, async _ => await SelectedTab.Value!.SaveCurrent());
        SaveAsDialog = new(AnyTabOpen, async _ => await SelectedTab.Value!.SaveAs());

        EffectManager.ShimPropertyChanged(TabList);
        SaveAll = new(() =>
        {
            EffectManager.Track(TabList, "Item[]");
            return TabList.Where(f => f.Dirty.Value).Any();
        }, async _ =>
        {
            foreach (var tab in TabList.Where(f => f.Dirty.Value))
                await tab.SaveCurrent();
        });
    }

    private void SelectTab(int index) => SelectedTabIndex.Value = Math.Clamp(index, 0, TabList.Count - 1);
    private async void CloseTab(FileTabViewModel tab)
    {
        var initial = SelectedTabIndex.Value;
        var closed = await HandleClosingTab(tab);
        if (TabList.Count > 0 && closed >= 0)
            SelectTab(initial - (closed < initial ? 1 : 0));
    }

    public async void OpenFileDialog()
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
            .OfType<FileLocation>();
        var unique = locations.Except(fileLocations);

        if (locations.Count() == 1 && !unique.Any())
        {
            var idx = 0;
            var loc = locations.Single();

            foreach (var vm in TabList)
            {
                if (vm.Location.Value is FileLocation floc && floc == loc) break;
                idx++;
            }

            SelectTab(idx);
        }
        else if (unique.Any())
        {
            foreach (var vm in unique.Select(loc => FileTabViewModel.FromLocation(loc))) TabList.Add(vm);
            SelectTab(TabList.Count);
        }
    }

    private static int _sampleCount = 1;
    public void GenerateSample()
    {
        var loc = DataProcessing.Instance.AddSampleDataset(
            new AnnotatedDataModel
            {
                AnnotatedName = $"Sample #{_sampleCount++}",
            },
            SampleGenerator.GenerateSensorData(SampleGenerator.DefaultProfile, 10, 60)
        );

        TabList.Add(FileTabViewModel.FromLocation(loc));
        SelectTab(TabList.Count);
    }

    private async Task<int> HandleClosingTab(FileTabViewModel tab)
    {
        if (!await TryCloseTab(tab)) return -1;

        var idx = TabList.IndexOf(tab);
        var front = idx != -1;

        if (front) TabList.RemoveAt(idx);
        var back = DataProcessing.Instance.Datasets.Remove(tab.Location.Value);

        Trace.Assert(front == back, "frontend & backend state differed");
        return front ? idx : -1;
    }

    private async Task<bool> TryCloseTab(FileTabViewModel tab)
    {
        if (_closingTab || _closingWindow) return false;
        if (!tab.Dirty.Value) return true;
        _closingTab = true;

        var box = MessageBoxManager.GetMessageBoxCustom(new MsBox.Avalonia.Dto.MessageBoxCustomParams
        {
            ButtonDefinitions = [
                new ButtonDefinition { Name = "Cancel", IsCancel = true, IsDefault = true },
                new ButtonDefinition { Name = "Don't Save" },
                new ButtonDefinition { Name = "Save" },
            ],
            ContentTitle = "Close?",
            ContentHeader = $"Do you want to save the changes you made to {tab.Header.Value}?",
            ContentMessage = "Your changes will be lost if you don't save them.",
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            Topmost = true,
        });

        var result = await box.ShowAsync() switch
        {
            "Cancel" => false,
            "Don't Save" => true,
            "Save" => await tab.SaveCurrent(),
            _ => throw new Exception("invalid message box result"),
        };

        _closingTab = false;
        return result;
    }

    // Handles the event when the window was requested to close.
    private async void HandleClosing(object? o, WindowClosingEventArgs e)
    {
        e.Cancel = true;

        if (await TryExit())
        {
            var window = ServiceProvider.ExpectService<MainWindow>();
            window.Closing -= HandleClosing;
            window.Close();
        }
    }

    // Handles actions that should be performed before the window is closed.
    // Returns whether or not the window should actually be closed, which
    // may not be true if the user cancelled the close in a dialog.
    private async Task<bool> TryExit()
    {
        if (_closingWindow || _closingTab) return false;
        if (!TabList.Where(t => t.Dirty.Value).Any()) return true;
        _closingWindow = true;

        var box = MessageBoxManager.GetMessageBoxCustom(new MsBox.Avalonia.Dto.MessageBoxCustomParams
        {
            ButtonDefinitions = [
                new ButtonDefinition { Name = "Cancel", IsCancel = true, IsDefault = true },
                new ButtonDefinition { Name = "Discard Changes" },
            ],
            ContentTitle = "Quit?",
            ContentHeader = "There are unsaved changes!",
            ContentMessage = "If you quit now these changes will be discarded.",
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            Topmost = true,
        });

        var result = await box.ShowAsync() switch
        {
            "Cancel" => false,
            "Discard Changes" => true,
            _ => throw new Exception("invalid message box result"),
        };

        _closingWindow = false;
        return result;
    }
}
