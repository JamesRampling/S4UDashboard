using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Platform.Storage;

using S4UDashboard.Model;
using S4UDashboard.Reactive;

namespace S4UDashboard.ViewModels;

public class FileTabViewModel : ViewModelBase
{
    public ReactiveCell<ILocation> Location { get; }
    public ReactiveCell<DatasetModel> Dataset { get; }

    public ReactiveCell<bool> Dirty { get; } = new(false);
    public ReactiveCell<string> NameField { get; }
    public ReactiveCell<double?> LowerField { get; }
    public ReactiveCell<double?> UpperField { get; }

    public ReactiveCell<bool> VisualiseCells { get; } = new(false);

    public ComputedCell<string> Header { get; }
    public FlatTreeDataGridSource<IEnumerable<double>> GridSource { get; }

    public ReactiveCommand UpdateAnnotatedName { get; }
    public ReactiveCommand ClearAnnotatedName { get; }

    public FileTabViewModel(ReactiveCell<DatasetModel> dataset, ILocation location)
    {
        Location = new(location);
        Dataset = dataset;

        NameField = new(dataset.Value.AnnotatedData.AnnotatedName ?? "");
        LowerField = new(dataset.Value.AnnotatedData.LowerThreshold);
        UpperField = new(dataset.Value.AnnotatedData.UpperThreshold);

        Header = new(() => Dataset.Value.AnnotatedData.AnnotatedName ?? Location.Value.LocationHint);
        GridSource = new(Dataset.Value.SensorData.Samples);
        GridSource.Columns.AddRange(Dataset.Value.SensorData.SensorNames.Select(
            (s, i) => new TextColumn<IEnumerable<double>, double>(s, row => row.ElementAt(i))
        ));

        UpdateAnnotatedName = new(
            () => NameField.Value.Trim() != Dataset.Value.AnnotatedData.AnnotatedName
               && NameField.Value.Trim() != "",
            _ => UpdateData(d => d with { AnnotatedName = NameField.Value.Trim() }));
        ClearAnnotatedName = new(
            () => Dataset.Value.AnnotatedData.AnnotatedName != null,
            _ => UpdateData(d => d with { AnnotatedName = null }));

        EffectManager.Watch(
            () => LowerField.Value,
            l => UpdateData(d => d with { LowerThreshold = l }));
        EffectManager.Watch(
            () => UpperField.Value,
            u => UpdateData(d => d with { UpperThreshold = u }));

        EffectManager.Watch(
            () => Dataset.Value,
            _ => Dirty.Value = true);
    }

    public static FileTabViewModel? FromLocation(ILocation location)
    {
        var dataset = DataProcessing.Instance.LoadDataset(location);
        if (dataset == null) return null;

        return new(dataset, location);
    }

    public async Task<bool> SaveCurrent()
    {
        if (!Location.Value.IsPhysical) return await SaveAs();

        if (!DataProcessing.Instance.SaveDataset(Location.Value))
            return false;
        Dirty.Value = false;

        return true;
    }

    public async Task<bool> SaveAs()
    {
        var storage = ServiceProvider.ExpectService<IStorageProvider>();
        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save As",
            DefaultExtension = "sds",
            FileTypeChoices = [SDSFileType],
            ShowOverwritePrompt = true,
        });
        if (file == null) return false;

        var destination = new FileLocation(file.Path);
        if (!DataProcessing.Instance.SaveDatasetAs(Location.Value, destination))
            return false;

        Location.Value = destination;
        Dirty.Value = false;

        return true;
    }

    private void UpdateData(Func<AnnotatedDataModel, AnnotatedDataModel> update)
    {
        var dataset = Dataset.Value;
        Dataset.Value = dataset with { AnnotatedData = update(dataset.AnnotatedData) };
    }

    public static readonly FilePickerFileType SDSFileType = new("Sensing4U Dataset (*.sds)")
    {
        Patterns = ["*.sds"],
        MimeTypes = ["application/s4udashboard"],
    };
}
