using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using S4UDashboard.Model;
using S4UDashboard.Reactive;
using S4UDashboard.Views;

namespace S4UDashboard.ViewModels;

public class MainViewModel : ViewModelBase
{
    public ReactiveList<FileTabViewModel> OpenFiles { get; } = [];
    public ReactiveCell<int> SelectedTabIndex { get; } = new(-1);

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

        bool AnyTabOpen() => SelectedTabIndex.Value >= 0;
        void SelectTab(int index) => SelectedTabIndex.Value = Math.Clamp(index, 0, OpenFiles.Count - 1);

        GoNextTab = new(AnyTabOpen, _ => SelectTab(SelectedTabIndex.Value + 1));
        GoPrevTab = new(AnyTabOpen, _ => SelectTab(SelectedTabIndex.Value + 1));
        CloseSelectedTab = new(AnyTabOpen, _ =>
        {
            var initial = SelectedTabIndex.Value;
            OpenFiles.RemoveAt(initial);
            if (OpenFiles.Count > 0) SelectTab(initial);
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

            OpenFiles.AddRange(await Task.WhenAll(
                files.Select(file => FileTabViewModel.FromFile(file))));
            SelectTab(OpenFiles.Count);
        });

        SaveCurrent = new(AnyTabOpen, _ => OpenFiles[SelectedTabIndex.Value].SaveCurrent());
        SaveAsDialog = new(AnyTabOpen, _ => OpenFiles[SelectedTabIndex.Value].SaveAs());
        SaveAll = new(() => OpenFiles.Where(f => f.Dirty.Value).Any(), _ =>
        {
            foreach (var tab in OpenFiles.Where(f => f.Dirty.Value)) tab.SaveCurrent();
        });
    }

    public void MakeNew()
    {
        var sensors = new SensorDataModel
        {
            MeasurementIdentifier = "temperature",
            SensorNames = ["bedroom", "office", "kitchen"],
            SampleTimes = [new(2025, 4, 1), new(2025, 4, 2), new(2025, 4, 3)],
            Samples = new([26, 22, 28, 21, 23, 21, 29, 31, 32], 3, 3),
        };

        OpenFiles.Add(new(new DatasetModel
        {
            AnnotatedData = new AnnotatedDataModel
            {
                AnnotatedName = "Foo",
            },
            CalculatedData = DataProcessing.Instance.CalculateAuxilliaryData(sensors),
            SensorData = sensors,
        }, new("file:///tmp/foo")));
    }

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
        return true;
    }
}
