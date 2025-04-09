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

    public static Func<DatasetModel, IComparable> GetSortSelector(SortMode mode) => mode switch
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

    public int SearchDatasets(SortMode mode, string needle)
    {
        var selector = GetSortSelector(mode);
        var sorted = Datasets
            .Select(p => selector(p.Value.Value).ToString()
                ?? throw new InvalidOperationException("failed to get string for value"))
            .Order().ToImmutableList();

        return FindInSorted(sorted, needle);
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

    private static DatasetModel? ReadDataset(ILocation target)
    {
        try
        {
            using var reader = new BinaryReader(target.OpenReadStream());
            return reader.Read(Serializers.DatasetDeserializer);
        }
        catch (Exception)
        {
            ServiceProvider.ExpectService<AlertService>().Alert(
                "Failed to load",
                $"There was an issue loading {target.LocationHint}",
                "Please ensure the file is valid and readable.");
            return null;
        }
    }

    private static bool WriteDataset(DatasetModel model, ILocation target)
    {
        try
        {
            using var writer = new BinaryWriter(target.OpenWriteStream());
            Serializers.DatasetSerializer(writer, model);
            return true;
        }
        catch (Exception)
        {
            ServiceProvider.ExpectService<AlertService>().Alert(
                "Failed to save",
                $"There was an issue saving {target.LocationHint}",
                "Please make sure the file location is writable.");
            return false;
        }
    }

    public ReactiveCell<DatasetModel>? LoadDataset(ILocation target)
    {
        if (Datasets.TryGetValue(target, out var existing)) return existing;

        var ds = ReadDataset(target);
        if (!ds.HasValue) return null;

        Datasets[target] = new(ds.Value);
        return Datasets[target];
    }

    public bool SaveDataset(ILocation target) => WriteDataset(Datasets[target].Value, target);
    public bool SaveDatasetAs(ILocation source, ILocation destination)
    {
        if (Datasets.ContainsKey(destination))
        {
            ServiceProvider.ExpectService<AlertService>().Alert(
                "Cannot overwrite",
                "There is already an open file at that location",
                "Close the conflicting file before attempting to overwrite.");
            return false;
        }

        var dataset = Datasets[source];
        if (!WriteDataset(dataset.Value, destination)) return false;

        Datasets.Remove(source);
        Datasets[destination] = dataset;
        return true;
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
    /// Performs a binary search on <c>sorted</c>, searching for <c>needle</c>.
    /// </summary>
    private static int FindInSorted<T>(IReadOnlyList<T> sorted, T needle) where T : IComparable<T>
    {
        int lowerBound = 0, upperBound = sorted.Count;

        while (lowerBound < upperBound)
        {
            var middleIdx = (lowerBound + upperBound) / 2;
            var current = sorted[middleIdx];

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
