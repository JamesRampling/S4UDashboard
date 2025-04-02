using System;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Platform.Storage;

using S4UDashboard.Model;
using S4UDashboard.Reactive;

namespace S4UDashboard.ViewModels;

public class FileTabViewModel : ViewModelBase
{
    public ReactiveCell<Uri> Location { get; }
    public ReactiveCell<DatasetModel> Dataset { get; }

    public ReactiveCell<bool> Dirty { get; } = new(false);
    public ReactiveCell<string> NameField { get; }
    public ReactiveCell<double?> LowerField { get; }
    public ReactiveCell<double?> UpperField { get; }

    public ComputedCell<string> Header { get; }

    public ReactiveCommand UpdateAnnotatedName { get; }
    public ReactiveCommand ClearAnnotatedName { get; }

    public FileTabViewModel(DatasetModel dataset, Uri location)
    {
        Location = new(location);
        Dataset = new(dataset);

        NameField = new(dataset.AnnotatedData.AnnotatedName ?? "");
        LowerField = new(dataset.AnnotatedData.LowerThreshold);
        UpperField = new(dataset.AnnotatedData.UpperThreshold);

        Header = new(() => Dataset.Value.AnnotatedData.AnnotatedName ?? Location.Value.AbsolutePath);

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

    public async static Task<FileTabViewModel> FromFile(IStorageFile file)
    {
        using var reader = new BinaryReader(await file.OpenReadAsync());
        var dataset = reader.Read(Serializers.DatasetDeserializer);
        return new(dataset, file.Path);
    }

    public void SaveTo(Stream output)
    {
        using var writer = new BinaryWriter(output);
        Serializers.DatasetSerializer(writer, Dataset.Value);
    }

    public async void SaveCurrent()
    {
        var storage = ServiceProvider.ExpectService<IStorageProvider>();

        var file = await storage.TryGetFileFromPathAsync(Location.Value);
        if (file == null) return;

        SaveTo(await file.OpenWriteAsync());
        Dirty.Value = false;
    }

    public async void SaveAs()
    {
        var storage = ServiceProvider.ExpectService<IStorageProvider>();
        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save As",
            DefaultExtension = "sds",
            FileTypeChoices = [SDSFileType],
            ShowOverwritePrompt = true,
        });

        if (file == null) return;

        SaveTo(await file.OpenWriteAsync());
        Location.Value = file.Path;
        Dirty.Value = false;
    }

    private void UpdateData(Func<AnnotatedDataModel, AnnotatedDataModel> update)
    {
        var dataset = Dataset.Value;
        Dataset.Value = dataset with { AnnotatedData = update(dataset.AnnotatedData) };
    }

    public static readonly FilePickerFileType SDSFileType = new("Sensing4U Dataset")
    {
        Patterns = ["*.sds"],
        MimeTypes = ["application/s4udashboard"],
    };
}
