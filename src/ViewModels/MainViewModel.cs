using System;
using System.IO;

using Avalonia.Controls;
using Avalonia.Platform.Storage;

using S4UDashboard.Model;
using S4UDashboard.Reactive;

namespace S4UDashboard.ViewModels;

public class MainViewModel : ViewModelBase
{
    public Window? MainWindow { get; }

    public ReactiveList<FileTabViewModel> OpenFiles { get; } = [];
    public ReactiveCell<int> SelectedTabIndex { get; } = new(-1);

    public MainViewModel(Window? mainWindow)
    {
        if ((MainWindow = mainWindow) != null) MainWindow.Closing += HandleClosing;
    }
    public MainViewModel() : this(null) { }

    public static readonly FilePickerFileType SDSFileType = new("Sensing4U Dataset")
    {
        Patterns = ["*.sds"],
        MimeTypes = ["application/s4udashboard"],
    };

    public async void OpenFileDialog()
    {
        if (MainWindow == null) return;

        var files = await MainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open File",
            AllowMultiple = true,
            FileTypeFilter = [SDSFileType],
        });

        if (files == null) return;

        foreach (var file in files)
        {
            using var reader = new BinaryReader(await file.OpenReadAsync());
            var dataset = reader.Read(Serializers.DatasetDeserializer(file.Path));
            OpenFiles.Add(new(dataset));
        }
        SelectTab(OpenFiles.Count);
    }

    public async void SaveCurrent()
    {
        if (MainWindow == null) return;
        if (SelectedTabIndex.Value < 0) return;

        var current = OpenFiles[SelectedTabIndex.Value];
        var file = await MainWindow.StorageProvider.TryGetFileFromPathAsync(current.Dataset.Value.FilePath);

        if (file == null) return;

        SaveDataset(current.Dataset.Value, await file.OpenWriteAsync());
    }

    public async void SaveAsDialog()
    {
        if (MainWindow == null) return;
        if (SelectedTabIndex.Value < 0) return;

        var file = await MainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save As",
            DefaultExtension = "sds",
            FileTypeChoices = [SDSFileType],
            ShowOverwritePrompt = true,
        });

        if (file == null) return;

        var current = OpenFiles[SelectedTabIndex.Value];
        SaveDataset(current.Dataset.Value, await file.OpenWriteAsync());
    }

    private static void SaveDataset(DatasetModel dataset, Stream output)
    {
        using var writer = new BinaryWriter(output);
        Serializers.DatasetSerializer(writer, dataset);
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
            FilePath = new("file:///tmp/foo"),
            AnnotatedData = new AnnotatedDataModel
            {
                AnnotatedName = "Foo",
            },
            CalculatedData = DataProcessing.Instance.CalculateAuxilliaryData(sensors),
            SensorData = sensors,
        }));
    }

    private void IfAnyTabs(Action action)
    {
        if (SelectedTabIndex.Value >= 0) action();
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
