using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace S4UDashboard.Model;

/// <summary>A utility class with various serializers &amp; deserializers.</summary>
public static class Serializers
{
    /// <summary>The magic number expected at the start of binary files.</summary>
    /* This is "S4UD" in ASCII (little endian) */
    public readonly static int MagicSignature = 0x44_55_34_53;

    /// <summary>The current version of the serialisation format.</summary>
    public readonly static int LatestFormatVersion = -2;

    /// <summary>A function that reads a string from a binary reader.</summary>
    private readonly static Func<BinaryReader, string> ReadString = (r) => r.ReadString();

    /// <summary>An action that writes a string to a binary writer.</summary>
    private readonly static Action<BinaryWriter, string> WriteString = (w, i) => w.Write(i);

    /// <summary>A function that reads a double from a binary reader.</summary>
    private readonly static Func<BinaryReader, double> ReadDouble = (r) => r.ReadDouble();

    /// <summary>An action that writes a double to a binary writer.</summary>
    private readonly static Action<BinaryWriter, double> WriteDouble = (w, i) => w.Write(i);

    /// <summary>A function that reads annotated data from a binary reader.</summary>
    public readonly static Func<BinaryReader, AnnotatedDataModel> AnnotatedDataDeserializer = (r) => new AnnotatedDataModel
    {
        AnnotatedName = r.ReadOptional(ReadString),
        LowerThreshold = r.ReadOptionalValue(ReadDouble),
        UpperThreshold = r.ReadOptionalValue(ReadDouble),
    };

    /// <summary>An action that writes annotated data to a binary writer.</summary>
    public readonly static Action<BinaryWriter, AnnotatedDataModel> AnnotatedDataSerializer = (w, i) =>
    {
        w.WriteOptional(WriteString, i.AnnotatedName);
        w.WriteOptionalValue(WriteDouble, i.LowerThreshold);
        w.WriteOptionalValue(WriteDouble, i.UpperThreshold);
    };

    /// <summary>A function that reads sensor data from a binary reader.</summary>
    public readonly static Func<BinaryReader, SensorDataModel> SensorDataDeserializer = (r) =>
    {
        var measurementIdentifier = r.ReadString();
        var sensorNames = r.ReadEnumerable(ReadString).ToImmutableArray();
        var nSamples = r.ReadInt32();
        var samples = r.ReadRawEnumerable(ReadDouble, sensorNames.Length * nSamples);

        return new SensorDataModel
        {
            MeasurementIdentifier = measurementIdentifier,
            SensorNames = sensorNames,
            Samples = samples.To2DArray(sensorNames.Length, nSamples),
        };
    };

    /// <summary>An action that writes sensor data to a binary writer.</summary>
    public readonly static Action<BinaryWriter, SensorDataModel> SensorDataSerializer = (w, i) =>
    {
        w.Write(i.MeasurementIdentifier);
        w.WriteEnumerable(WriteString, i.SensorNames);
        w.Write(i.Samples.GetLength(1));
        w.WriteRawEnumerable(WriteDouble, i.Samples.EnumerateFlat());
    };

    /// <summary>A function that reads a dataset from a binary reader.</summary>
    public readonly static Func<BinaryReader, DatasetModel> DatasetDeserializer = (r) =>
    {
        int version;

        if (r.ReadInt32() != MagicSignature) throw new Exception("magic number did not match!");
        if ((version = r.ReadInt32()) != LatestFormatVersion) throw new Exception("format version did not match!");

        var annotatedData = r.Read(AnnotatedDataDeserializer);
        var sensorData = r.Read(SensorDataDeserializer);
        var calculatedData = DataProcessing.CalculateAuxilliaryData(sensorData);

        return new DatasetModel
        {
            AnnotatedData = annotatedData,
            SensorData = sensorData,
            CalculatedData = calculatedData,
        };
    };

    /// <summary>An action that writes a dataset to a binary writer.</summary>
    public readonly static Action<BinaryWriter, DatasetModel> DatasetSerializer = (w, i) =>
    {
        w.Write(MagicSignature);
        w.Write(LatestFormatVersion);

        w.Write(AnnotatedDataSerializer, i.AnnotatedData);
        w.Write(SensorDataSerializer, i.SensorData);
    };
}

/// <summary>An extension class to assist using BinaryReader and BinaryWriter.</summary>
public static class Serialization
{
    /// <summary>Writes a generic value to a binary writer.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="serializer">The serializer to execute.</param>
    /// <param name="value">The value to serialize.</param>
    public static void Write<T>(
        this BinaryWriter writer,
        Action<BinaryWriter, T> serializer,
        T value
    ) => serializer(writer, value);

    /// <summary>Reads a generic value from a binary reader.</summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="deserializer">The deserializer to execute.</param>
    public static T Read<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer
    ) => deserializer(reader);

    /// <summary>Writes a nullable generic value to a binary writer.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="serializer">The serializer to execute.</param>
    /// <param name="value">The nullable value to serialize.</param>
    public static void WriteOptional<T>(
        this BinaryWriter writer,
        Action<BinaryWriter, T> serializer,
        T? value
    ) where T : class
    {
        writer.Write(value != null);
        if (value != null) serializer(writer, value);
    }

    /// <summary>Reads a nullable generic value from a binary reader.</summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="deserializer">The deserializer to execute.</param>
    public static T? ReadOptional<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer
    ) where T : class => reader.ReadBoolean() ? deserializer(reader) : null;

    /// <summary>Writes a nullable generic struct to a binary writer.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="serializer">The serializer to execute.</param>
    /// <param name="value">The nullable struct to serialize.</param>
    public static void WriteOptionalValue<T>(
        this BinaryWriter writer,
        Action<BinaryWriter, T> serializer,
        T? value
    ) where T : struct
    {
        writer.Write(value.HasValue);
        if (value.HasValue) serializer(writer, value.Value);
    }

    /// <summary>Reads a nullable generic struct from a binary reader.</summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="deserializer">The deserializer to execute.</param>
    public static T? ReadOptionalValue<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer
    ) where T : struct => reader.ReadBoolean() ? deserializer(reader) : null;

    /// <summary>Writes a generic enumerable to a binary writer.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="serializer">The serializer to execute for each item.</param>
    /// <param name="series">The enumerable of values to serialize.</param>
    public static void WriteEnumerable<T>(
        this BinaryWriter writer,
        Action<BinaryWriter, T> serializer,
        IEnumerable<T> series
    )
    {
        writer.Write(series.Count());
        foreach (var item in series) serializer(writer, item);
    }

    /// <summary>Reads a generic enumerable from a binary reader.</summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="deserializer">The deserializer to execute for each item.</param>
    public static IEnumerable<T> ReadEnumerable<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer
    )
    {
        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++) yield return deserializer(reader);
    }

    /// <summary>Writes a generic enumerable to a binary writer without a length.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="serializer">The serializer to execute for each item.</param>
    /// <param name="series">The enumerable of values to serialize.</param>
    public static void WriteRawEnumerable<T>(
        this BinaryWriter writer,
        Action<BinaryWriter, T> serializer,
        IEnumerable<T> series
    )
    {
        foreach (var item in series) serializer(writer, item);
    }

    /// <summary>Reads a generic enumerable with a predetermined length from a binary reader.</summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="deserializer">The deserializer to execute for each item.</param>
    /// <param name="length">The number of items to deserialize.</param>
    public static IEnumerable<T> ReadRawEnumerable<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer,
        int length
    )
    {
        for (var i = 0; i < length; i++) yield return deserializer(reader);
    }
}
