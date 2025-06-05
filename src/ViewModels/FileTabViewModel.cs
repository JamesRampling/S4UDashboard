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

/// <summary>The viewmodel for a tab.</summary>
public class FileTabViewModel
{
    /// <summary>The location this tab represents.</summary>
    public ReactiveCell<ILocation> Location { get; }

    /// <summary>The data this tab represents.</summary>
    public ReactiveCell<DatasetModel> Dataset { get; }

    /// <summary>Whether or not this tab has unsaved changes.</summary>
    public ReactiveCell<bool> Dirty { get; } = new(false);

    /// <summary>The value recorded in the name textbox.</summary>
    public ReactiveCell<string> NameField { get; }

    /// <summary>The value recorded in the lower bound numeric control.</summary>
    public ReactiveCell<double?> LowerField { get; }

    /// <summary>The value recorded in the upper bound numeric control.</summary>
    public ReactiveCell<double?> UpperField { get; }

    /// <summary>Whether or not cells should be colourised.</summary>
    public ReactiveCell<bool> VisualiseCells { get; } = new(false);

    /// <summary>The tab header computed based off of the dataset name and file location.</summary>
    public ComputedCell<string> Header { get; }

    /// <summary>The datagrid source for the dataset sensor &amp; samples.</summary>
    public FlatTreeDataGridSource<IEnumerable<double>> GridSource { get; }

    /// <summary>The command responsible for handling when the name field is updated.</summary>
    public ReactiveCommand UpdateAnnotatedName { get; }

    /// <summary>The command responsible for handling when the name field is cleared.</summary>
    public ReactiveCommand ClearAnnotatedName { get; }

    /// <summary>Creates a tab view model.</summary>
    /// <param name="dataset">The dataset this tab will display.</param>
    /// <param name="location">The location this tab represents.</param>
    public FileTabViewModel(ReactiveCell<DatasetModel> dataset, ILocation location)
    {
        Location = new(location);
        Dataset = dataset;

        NameField = new(dataset.Value.AnnotatedData.AnnotatedName ?? "");
        LowerField = new(dataset.Value.AnnotatedData.LowerThreshold);
        UpperField = new(dataset.Value.AnnotatedData.UpperThreshold);

        Header = new(() => Dataset.Value.AnnotatedData.AnnotatedName ?? Location.Value.LocationHint);
        GridSource = new(Dataset.Value.SensorData.Samples.EnumerateGrid());
        GridSource.Columns.AddRange(Dataset.Value.SensorData.SensorNames.Select(
            (s, i) => new TextColumn<IEnumerable<double>, double>(s, row => row.ElementAt(i))
        ));

        UpdateAnnotatedName = new(
            () => NameField.Value.Trim() != Dataset.Value.AnnotatedData.AnnotatedName
               && NameField.Value.Trim() != "",
            _ => UpdateData(d => d with { AnnotatedName = NameField.Value.Trim() }, true));
        ClearAnnotatedName = new(
            () => Dataset.Value.AnnotatedData.AnnotatedName != null,
            _ => UpdateData(d => d with { AnnotatedName = null }, true));

        EffectManager.Watch(
            () => LowerField.Value,
            l =>
            {
                UpdateData(d => d with { LowerThreshold = l });
                VisualiseCells.Value = true;
            });
        EffectManager.Watch(
            () => UpperField.Value,
            u =>
            {
                UpdateData(d => d with { UpperThreshold = u });
                VisualiseCells.Value = true;
            });

        EffectManager.Watch(
            () => Dataset.Value,
            _ => Dirty.Value = true);
    }

    /// <summary>Creates a tab view model from just a location, if it can be loaded from.</summary>
    /// <param name="location">The location to create the tab view model from.</param>
    public static FileTabViewModel? FromLocation(ILocation location)
    {
        ReactiveCell<DatasetModel>? dataset = default;
        if (!DataProcessing.Instance.LoadDataset(location, ref dataset))
            return null;
        return new(dataset, location);
    }

    /// <summary>Saves the dataset to the current location.</summary>
    public async Task<bool> SaveCurrent()
    {
        if (!Location.Value.IsPhysical) return await SaveAs();

        if (!DataProcessing.Instance.SaveDataset(Location.Value))
            return false;
        Dirty.Value = false;

        return true;
    }

    /// <summary>Saves the dataset to a new location.</summary>
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

    /// <summary>Updates the dataset with new annotated data.</summary>
    /// <param name="update">The function to update the annotated data with.</param>
    /// <param name="notifyName">Whether or not to trigger a name notification for tab sorting.</param>
    private void UpdateData(Func<AnnotatedDataModel, AnnotatedDataModel> update, bool notifyName = false)
    {
        var dataset = Dataset.Value;
        Dataset.Value = dataset with { AnnotatedData = update(dataset.AnnotatedData) };

        // hack to mitigate names being the only mutable, sortable property
        if (notifyName) EffectManager.Trigger(this, "NameNotify");
    }

    /// <summary>The file picker type for Sensing4U dataset binary files.</summary>
    public static readonly FilePickerFileType SDSFileType = new("Sensing4U Dataset (*.sds)")
    {
        Patterns = ["*.sds"],
        MimeTypes = ["application/s4udashboard"],
    };
}
