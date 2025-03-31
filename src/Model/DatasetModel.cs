using System;
using System.Collections.Immutable;
using System.IO;

namespace S4UDashboard.Model;

/// <summary>Represents a dataset.
/// <para>
/// Only <c>AnnotatedData</c> and <c>SensorData</c> should be serialised.
/// </para>
/// </summary>
public readonly record struct DatasetModel
{
    public required string FilePath { get; init; }
    public string FileName => Path.GetFileNameWithoutExtension(FilePath);

    public required AnnotatedDataModel AnnotatedData { get; init; }
    public required CalculatedDataModel CalculatedData { get; init; }
    public required SensorDataModel SensorData { get; init; }
}

/// <summary>
/// Represents the components of a dataset which may be updated by the program.
/// </summary>
public readonly record struct AnnotatedDataModel
{
    public string? AnnotatedName { get; init; }
    public double? LowerThreshold { get; init; }
    public double? UpperThreshold { get; init; }
}

/// <summary>
/// Represents the components of a dataset which can be derived from the sensor data.
/// </summary>
public readonly record struct CalculatedDataModel
{
    public required double Mean { get; init; }
    public required double Minimum { get; init; }
    public required double Maximum { get; init; }
}

/// <summary>
/// Represents the components of a dataset which should never be updated by the program.
/// </summary>
public readonly record struct SensorDataModel
{
    public required string MeasurementIdentifier { get; init; }

    public required ImmutableArray<string> SensorNames { get; init; }
    public required ImmutableArray<DateTime> SampleTimes { get; init; }

    public required ImmutableArray<ImmutableArray<double>> Samples { get; init; }
}
