using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

using S4UDashboard.Reactive;

namespace S4UDashboard.Model;

/// <summary>
/// Represents all of the possible properties datasets can be sorted by.
/// </summary>
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
    /// <summary>The singleton instance for <c>DataProcessing</c>.</summary>
    public readonly static DataProcessing Instance = new();
    /// <summary>The list of currently loaded datasets, associated by location.</summary>
    public readonly Dictionary<ILocation, ReactiveCell<DatasetModel>> Datasets = [];

    /// <summary>Takes a sorting mode and returns a selector to get the sort property.</summary>
    /// <param name="mode">The sort mode corresponding to the desired selector.</param>
    /// <returns>A selector function that gets the property specified by <c>mode</c>.</returns>
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

    /// <summary>
    /// Given a sorting mode and a string to search for, sorts the loaded datasets
    /// and performs a binary search to find a match.
    /// </summary>
    /// <param name="mode">The sort mode by which to order the datasets.</param>
    /// <param name="needle">The string representation of the property to find.</param>
    /// <returns>
    /// The index of the found dataset in the sorted representation, or -1 if no match was found.
    /// </returns>
    public int SearchDatasets(SortMode mode, string needle)
    {
        Trace.WriteLine($"SearchDatasets mode: {mode}; needle: {needle}");
        Trace.Indent();

        var selector = GetSortSelector(mode);
        var sorted = Datasets
            .Select(p => selector(p.Value.Value).ToString()
                ?? throw new InvalidOperationException("failed to get string for value"))
            .Order().ToImmutableList();

        var res = FindInSorted(sorted, needle);
        Trace.Unindent();
        return res;
    }

    /// <summary>
    /// Creates a dataset from annotated data and sensor data, adds it to the currently
    /// loaded datasets with a new unnamed location, and returns the new location.
    /// </summary>
    /// <param name="annotatedData">The annotated data in the new dataset.</param>
    /// <param name="sensorData">The sensor data in the new dataset.</param>
    /// <returns>The location of the newly created dataset.</returns>
    public ILocation AddSampleDataset(AnnotatedDataModel annotatedData, SensorDataModel sensorData)
    {
        Trace.WriteLine($"AddSampleDataset name: {annotatedData.AnnotatedName ?? "none"}");
        Trace.Indent();

        var calculated = CalculateAuxilliaryData(sensorData);
        var location = new UnnamedLocation();

        Datasets[location] = new(new DatasetModel
        {
            AnnotatedData = annotatedData,
            SensorData = sensorData,
            CalculatedData = calculated,
        });

        Trace.Unindent();
        return location;
    }

    /// <summary>Attempts to read a dataset from a physical location.</summary>
    /// <param name="target">The location to read the dataset from.</param>
    /// <param name="model">
    /// The variable to read the dataset into, only initialised if the return value was true.
    /// </param>
    /// <returns>True if the dataset was read successfully, false otherwise.</returns>
    private static bool ReadDataset(ILocation target, [NotNullWhen(true)] ref DatasetModel model)
    {
        Trace.WriteLine($"ReadDataset target: {target.LocationHint}");
        Trace.Indent();

        try
        {
            using var reader = new BinaryReader(target.OpenReadStream());

            model = reader.Read(Serializers.DatasetDeserializer);
            Trace.WriteLine($"successfully read dataset from {target.LocationHint}");
            return true;
        }
        catch (Exception)
        {
            Trace.WriteLine($"failed to read dataset from {target.LocationHint}");
            ServiceProvider.ExpectService<AlertService>().Alert(
                "Failed to load",
                $"There was an issue loading {target.LocationHint}",
                "Please ensure the file is valid and readable.");

            model = default;
            return false;
        }
        finally
        {
            Trace.Unindent();
        }
    }

    /// <summary>Attempts to write out a dataset to a physical location.</summary>
    /// <param name="model">The dataset to serialise and write out.</param>
    /// <param name="target">The physical location to write to.</param>
    /// <returns>True if writing was successful, false if it failed.</returns>
    private static bool WriteDataset(DatasetModel model, ILocation target)
    {
        Trace.WriteLine($"WriteDataset name: {model.AnnotatedData.AnnotatedName ?? "none"}; target: {target.LocationHint}");
        Trace.Indent();

        try
        {
            using var writer = new BinaryWriter(target.OpenWriteStream());
            Serializers.DatasetSerializer(writer, model);
            Trace.WriteLine($"successfully wrote dataset to {target.LocationHint}");
            return true;
        }
        catch (Exception)
        {
            Trace.WriteLine($"failed to write dataset to {target.LocationHint}");
            ServiceProvider.ExpectService<AlertService>().Alert(
                "Failed to save",
                $"There was an issue saving {target.LocationHint}",
                "Please make sure the file location is writable.");
            return false;
        }
        finally
        {
            Trace.Unindent();
        }
    }

    /// <summary>
    /// Loads a dataset from a location and adds it to the loaded datasets under the target location.
    /// </summary>
    /// <param name="target">The location to load the dataset from.</param>
    /// <param name="model">
    /// The variable to load a reference to the dataset into, only initialised if the return value was true.
    /// </param>
    /// <returns>True if the dataset was loaded successfully, false otherwise.</returns>
    public bool LoadDataset(ILocation target, [NotNullWhen(true)] ref ReactiveCell<DatasetModel>? model)
    {
        Trace.WriteLine($"LoadDataset target: {target.LocationHint}");
        Trace.Indent();

        try
        {
            DatasetModel raw = default;
            if (Datasets.TryGetValue(target, out model!)) return true;
            if (!ReadDataset(target, ref raw)) return false;

            model = new(raw);
            Datasets[target] = model;
            Trace.WriteLine($"succeeded loading dataset with name: {raw.AnnotatedData.AnnotatedName ?? "none"}");

            return true;
        }
        finally
        {
            Trace.Unindent();
        }
    }

    /// <summary>
    /// Saves a dataset at a given location by writing it to its associated location.
    /// </summary>
    /// <param name="target">The location associated to the dataset to be saved.</param>
    /// <returns>True if saving was successful, false if it failed.</returns>
    public bool SaveDataset(ILocation target) => WriteDataset(Datasets[target].Value, target);

    /// <summary>
    /// Saves a dataset associated to the source location to the destination location and
    /// reassigns the dataset to the destination location.
    /// </summary>
    /// <param name="source">The location associated to the dataset to save.</param>
    /// <param name="destination">The location to save &amp; reassign the dataset to.</param>
    /// <returns>True if the operation was successful, false if it failed.</returns>
    public bool SaveDatasetAs(ILocation source, ILocation destination)
    {
        Trace.WriteLine($"SaveDatasetAs source: {source.LocationHint}; destination {destination.LocationHint}");
        Trace.Indent();

        try
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
        finally
        {
            Trace.Unindent();
        }
    }

    /// <summary>Given sensor data, calculates the auxilliary data and returns it.</summary>
    /// <param name="sensorData">The sensor data from which to calculate the auxillary data.</param>
    /// <returns>The calculated data derived from the sensor data.</returns>
    public static CalculatedDataModel CalculateAuxilliaryData(SensorDataModel sensorData)
    {
        Trace.WriteLine("CalculateAuxilliaryData");
        Trace.Indent();
        double min = double.PositiveInfinity, max = double.NegativeInfinity, sum = 0.0;

        foreach (var sample in sensorData.Samples.EnumerateFlat())
        {
            if (sample < min) min = sample;
            if (sample > max) max = sample;
            sum += sample;
        }

        Trace.WriteLine($"Sum: {sum}");
        Trace.WriteLine($"Minimum: {min}");
        Trace.WriteLine($"Maximum: {max}");
        Trace.WriteLine($"Mean: {sum / sensorData.Samples.Length}");

        Trace.Unindent();
        return new CalculatedDataModel
        {
            Mean = sum / sensorData.Samples.Length,
            Minimum = min,
            Maximum = max,
        };
    }

    /// <summary>Performs a binary search on a sorted list.</summary>
    /// <typeparam name="T">The type stored in the list. Must be comparable to itself.</typeparam>
    /// <param name="sorted">The sorted list to search.</param>
    /// <param name="needle">The value to search the list for.</param>
    /// <returns>The index of the matched item in the list, or -1 if there was no match.</returns>
    private static int FindInSorted<T>(IReadOnlyList<T> sorted, T needle) where T : IComparable<T>
    {
        Trace.WriteLine($"FindInSorted needle: {needle}");
        Trace.Indent();

        int lowerBound = 0, upperBound = sorted.Count;

        while (lowerBound < upperBound)
        {
            var middleIdx = (lowerBound + upperBound) / 2;
            var current = sorted[middleIdx];

            Trace.WriteLine($"current bounds [{lowerBound};{upperBound})");
            switch (needle.CompareTo(current))
            {
                case 0:
                    Trace.Unindent();
                    return middleIdx;
                case < 0:
                    upperBound = middleIdx;
                    break;
                case > 0:
                    lowerBound = middleIdx + 1;
                    break;
            }
        }

        Trace.Unindent();
        return -1;
    }
}
