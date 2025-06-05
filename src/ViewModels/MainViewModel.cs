using System;
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

/// <summary>The view model for the main view.</summary>
public class MainViewModel
{
    /// <summary>The index of the currently selected tab.</summary>
    public ReactiveCell<int> SelectedTabIndex { get; } = new(-1);

    /// <summary>A reference to the currently selected tab view model.</summary>
    public ReactiveCell<FileTabViewModel?> SelectedTab { get; } = new(null);

    /// <summary>A sortable collection of currently loaded tabs.</summary>
    public SortedObservableView<FileTabViewModel> TabList { get; } = new([]);

    /// <summary>The current tab sort mode.</summary>
    public ReactiveCell<SortMode> TabsSortMode { get; } = new(SortMode.Unsorted);

    /// <summary>Whether or not the tab sort mode is not unsorted.</summary>
    public ComputedCell<bool> TabsAreSorted { get; }

    /// <summary>The current search text in the search textbox.</summary>
    public ReactiveCell<string> SearchText { get; } = new("");

    /// <summary>The command responsible for handling the User Manual menu button.</summary>
    public ReactiveCommand OpenWiki { get; } = new(() => true, _ =>
        ServiceProvider.ExpectService<ILauncher>()
            .LaunchUriAsync(new("https://jamesrampling.github.io/s4ud/")));

    /// <summary>The command responsible for handling the About menu button.</summary>
    public ReactiveCommand AboutAlert { get; } = new(() => true, _ =>
        ServiceProvider.ExpectService<AlertService>().Alert(
            "About",
            "Sensing4U Dashboard",
            "Version 1.0.0\nCreated by CITE Managed Systems for Sensing4U"));

    /// <summary>The command responsible for handling the Quit menu button.</summary>
    public ReactiveCommand QuitApp { get; } = new(
        () => ServiceProvider.GetService<MainWindow>() is not null,
        _ => ServiceProvider.ExpectService<MainWindow>().Close());

    /// <summary>The command responsible for handling the Next Tab menu button.</summary>
    public ReactiveCommand GoNextTab { get; }

    /// <summary>The command responsible for handling the Previous Tab menu button.</summary>
    public ReactiveCommand GoPrevTab { get; }

    /// <summary>The command responsible for handling the Close Tab menu button.</summary>
    public ReactiveCommand CloseSelectedTab { get; }

    /// <summary>The command responsible for handling closing a tab through the X button on each tab.</summary>
    public ReactiveCommand CloseTabCommand { get; }

    /// <summary>The command responsible for handling the Save menu button.</summary>
    public ReactiveCommand SaveCurrent { get; }

    /// <summary>The command responsible for handling the Save As menu button.</summary>
    public ReactiveCommand SaveAsDialog { get; }

    /// <summary>The command responsible for handling the Save All menu button.</summary>
    public ReactiveCommand SaveAll { get; }

    /// <summary>The command responsible for handling the search button.</summary>
    public ReactiveCommand SearchTabs { get; }

    /// <summary>Initialises the main viewmodel.</summary>
    public MainViewModel()
    {
        var window = ServiceProvider.GetService<MainWindow>();
        if (window != null) window.Closing += HandleClosing;

        TabsAreSorted = new(() => TabsSortMode.Value != SortMode.Unsorted);
        EffectManager.Watch(() => TabsSortMode.Value, mode =>
        {
            if (mode == SortMode.Unsorted) TabList.Impose();
            else
            {
                var sf = DataProcessing.GetSortSelector(mode);
                TabList.Selector = (vm) => sf(vm.Dataset.Value);
            }
        });

        bool AnyTabOpen() => SelectedTab.Value is not null;
        // CloseTab can currently cause front/backend desync when tabs are sorted.
        bool UnsortedTabsOpen() => AnyTabOpen() && !TabsAreSorted.Value;

        GoNextTab = new(
            () => SelectedTabIndex.Value >= 0 && SelectedTabIndex.Value < TabList.Count - 1,
            _ => SelectTab(SelectedTabIndex.Value + 1));
        GoPrevTab = new(() => SelectedTabIndex.Value > 0, _ => SelectTab(SelectedTabIndex.Value - 1));

        CloseTabCommand = new(UnsortedTabsOpen, o =>
        {
            if (o is not DragTabItem item || item.Header is not FileTabViewModel tab) return;
            CloseTab(tab);
        });
        CloseSelectedTab = new(UnsortedTabsOpen, _ => CloseTab(SelectedTab.Value!));

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

        SearchTabs = new(() => TabsAreSorted.Value && SearchText.Value.Trim() != "", _ =>
        {
            var idx = DataProcessing.Instance.SearchDatasets(TabsSortMode.Value, SearchText.Value.Trim());

            if (idx == -1)
            {
                ServiceProvider.ExpectService<AlertService>().Alert(
                    "Search",
                    "No results found",
                    "No open files matched that criteria.");
            }
            else SelectTab(idx);
        });
    }

    /// <summary>Selects a tab at the given index, clamped to the range of tab indices.</summary>
    /// <param name="index">The index of the tab to select.</param>
    private void SelectTab(int index) => SelectedTabIndex.Value = Math.Clamp(index, 0, TabList.Count - 1);

    /// <summary>Closes the tab associated to the given viewmodel.</summary>
    /// <param name="tab">The viewmodel of the tab to close.</param>
    private async void CloseTab(FileTabViewModel tab)
    {
        var initial = SelectedTabIndex.Value;
        var closed = await HandleClosingTab(tab);
        if (TabList.Count > 0 && closed >= 0)
            SelectTab(initial - (closed < initial ? 1 : 0));
    }

    /// <summary>Creates an Open File dialog and opens the selected files.</summary>
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
            foreach (var vm in unique.Select(loc => FileTabViewModel.FromLocation(loc)))
                if (vm != null) AddTabViewModel(vm);
            if (TabsSortMode.Value == SortMode.Unsorted && TabList.Count > 0)
                SelectTab(TabList.Count);
        }
    }

    /// <summary>The number of previously created sample datasets.</summary>
    private static int _sampleCount = 1;

    /// <summary>Creates a sample dataset and adds a tab with said dataset.</summary>
    public void GenerateSample()
    {
        var loc = DataProcessing.Instance.AddSampleDataset(
            new AnnotatedDataModel
            {
                AnnotatedName = $"Sample #{_sampleCount++}",
            },
            SampleGenerator.GenerateSensorData(SampleGenerator.DefaultProfile, 10, 100)
        );

        var vm = FileTabViewModel.FromLocation(loc);
        if (vm == null) return;

        AddTabViewModel(vm);
        var idx = TabsSortMode.Value == SortMode.Unsorted ? TabList.Count : TabList.IndexOf(vm);
        SelectTab(idx);
    }

    /// <summary>Adds a tab viewmodel as a tab.</summary>
    /// <param name="vm">The viewmodel to add to the tab list.</param>
    private void AddTabViewModel(FileTabViewModel vm)
    {
        TabList.Add(vm);
        EffectManager.Watch(
            () => EffectManager.Track(vm, "NameNotify"),
            () =>
            {
                if (TabsSortMode.Value == SortMode.Name)
                    TabsSortMode.Value = SortMode.Unsorted;
            }
        );
    }

    /// <summary>Removes a tab associated to a tab viewmodel from the tab list and data processor.</summary>
    /// <param name="tab">The viewmodel associated to the dataset to remove.</param>
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

    /// <summary>Attempts to close a tab, if it is dirty, ask the user to save the tab.</summary>
    /// <param name="tab">The viewmodel associated to the tab to close.</param>
    private static async Task<bool> TryCloseTab(FileTabViewModel tab)
    {
        if (!tab.Dirty.Value) return true;

        var result = await ServiceProvider.ExpectService<AlertService>().PopupWithCancel(
            "Close?",
            $"Do you want to save the changes you made to {tab.Header.Value}?",
            "Your changes will be lost if you don't save them.",
            ["Don't Save", "Save"]
        ) switch
        {
            "Cancel" or null => false,
            "Don't Save" => true,
            "Save" => await tab.SaveCurrent(),
            _ => throw new Exception("invalid message box result"),
        };

        return result;
    }

    /// <summary>An event handler that intercepts the window closing and asks the user about unsaved files.</summary>
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

    /// <summary>A helper that checks for any unsaved files and asks the user if they want to save each of them.</summary>
    private async Task<bool> TryExit()
    {
        if (!TabList.Where(t => t.Dirty.Value).Any()) return true;

        var result = await ServiceProvider.ExpectService<AlertService>().PopupWithCancel(
            "Quit?",
            "There are unsaved changes!",
            "If you quit now these changes will be discarded.",
            ["Discard Changes"]
        ) switch
        {
            "Cancel" or null => false,
            "Discard Changes" => true,
            _ => throw new Exception("invalid message box result"),
        };

        return result;
    }
}
