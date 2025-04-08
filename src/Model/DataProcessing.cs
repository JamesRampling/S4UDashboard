using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using S4UDashboard.Reactive;

namespace S4UDashboard.Model;

public enum SortMode
{
    Unsorted,

    Name,
    Measurement,
    Mean,
    Minimum,
    Maximum,
}

public class DataProcessing
{
    private DataProcessing() { }
    public readonly static DataProcessing Instance = new();

    public static Func<DatasetModel, IComparable> GetSortFunc(SortMode mode) => mode switch
    {
        SortMode.Unsorted => throw new ArgumentException("attempted to get sort func of unsorted"),

        SortMode.Name => d => d.AnnotatedData.AnnotatedName ?? "",
        SortMode.Measurement => d => d.SensorData.MeasurementIdentifier,
        SortMode.Mean => d => d.CalculatedData.Mean,
        SortMode.Minimum => d => d.CalculatedData.Minimum,
        SortMode.Maximum => d => d.CalculatedData.Maximum,

        _ => throw new ArgumentException("invalid enum variant"),
    };

    public Dictionary<ILocation, ReactiveCell<DatasetModel>> Datasets = [];
    public ReactiveCell<SortMode> Mode { get; } = new(SortMode.Unsorted);

    public ImmutableList<DatasetModel> Sorted =>
        [.. Datasets.Values.Select(c => c.Value).OrderBy(GetSortFunc(Mode.Value) ?? throw new InvalidOperationException())];

    public ReactiveCell<DatasetModel> LoadDataset(ILocation target)
    {
        if (Datasets.TryGetValue(target, out var existing)) return existing;

        using var reader = new BinaryReader(target.OpenReadStream());
        var dataset = reader.Read(Serializers.DatasetDeserializer);

        Datasets[target] = new(dataset);
        return Datasets[target];
    }

    public ILocation AddSampleDataset(AnnotatedDataModel annotatedData, SensorDataModel sensorData)
    {
        var calculated = CalculateAuxilliaryData(sensorData);
        var location = new UnnamedLocation();

        Datasets[location] = new(new DatasetModel
        {
            AnnotatedData = annotatedData,
            SensorData = sensorData,
            CalculatedData = calculated,
        });

        return location;
    }

    public void SaveDataset(ILocation target) =>
        SaveDataset(Datasets[target].Value, target.OpenWriteStream());

    public void SaveDatasetAs(ILocation source, ILocation destination)
    {
        if (Datasets.ContainsKey(destination))
            throw new InvalidOperationException("destination is already open");

        var dataset = Datasets[source];
        SaveDataset(dataset.Value, destination.OpenWriteStream());

        Datasets.Remove(source);
        Datasets[destination] = dataset;
    }

    private static void SaveDataset(DatasetModel model, Stream output)
    {
        using var writer = new BinaryWriter(output);
        Serializers.DatasetSerializer(writer, model);
    }

    /// <summary>
    /// Given a <c>SensorDataModel</c> constructs and returns a <c>CalculatedDataModel</c> based
    /// on the samples contained in <c>sensorData</c>.
    /// </summary>
    public static CalculatedDataModel CalculateAuxilliaryData(SensorDataModel sensorData)
    {
        double min = double.PositiveInfinity, max = double.NegativeInfinity, sum = 0.0;

        foreach (var sample in sensorData.Samples.EnumerateFlat())
        {
            if (sample < min) min = sample;
            if (sample > max) max = sample;
            sum += sample;
        }

        return new CalculatedDataModel
        {
            Mean = sum / sensorData.Samples.Length,
            Minimum = min,
            Maximum = max,
        };
    }

    /// <summary>
    /// Performs a binary search on <c>sorted</c>, comparing <c>needle</c> to each element
    /// after having been mapped by <c>selector</c>.
    /// </summary>
    public static int FindByInSorted<T, S>(IReadOnlyList<T> sorted, Func<T, S> selector, S needle) where S : IComparable<S>
    {
        int lowerBound = 0, upperBound = sorted.Count;

        while (lowerBound < upperBound)
        {
            var middleIdx = (lowerBound + upperBound) / 2;
            var current = selector(sorted[middleIdx]);

            switch (needle.CompareTo(current))
            {
                case 0:
                    return middleIdx;
                case < 0:
                    upperBound = middleIdx;
                    break;
                case > 0:
                    lowerBound = middleIdx + 1;
                    break;
            }
        }

        return -1;
    }
}
