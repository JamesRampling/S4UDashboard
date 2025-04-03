using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace S4UDashboard.Model;

public static class Serializers
{
    /* This is "S4UD" in ASCII (little endian) */
    public readonly static int MagicSignature = 0x44_55_34_53;
    /* The current version of the serialisation format */
    public readonly static int LatestFormatVersion = -1;

    private readonly static Func<BinaryReader, string> ReadString = (r) => r.ReadString();
    private readonly static Action<BinaryWriter, string> WriteString = (w, i) => w.Write(i);
    private readonly static Func<BinaryReader, double> ReadDouble = (r) => r.ReadDouble();
    private readonly static Action<BinaryWriter, double> WriteDouble = (w, i) => w.Write(i);
    private readonly static Func<BinaryReader, DateTime> ReadDateTime = (r) => DateTime.FromBinary(r.ReadInt64());
    private readonly static Action<BinaryWriter, DateTime> WriteDateTime = (w, i) => w.Write(i.ToBinary());

    public readonly static Func<BinaryReader, AnnotatedDataModel> AnnotatedDataDeserializer = (r) => new AnnotatedDataModel
    {
        AnnotatedName = r.ReadOptional(ReadString),
        LowerThreshold = r.ReadOptionalValue(ReadDouble),
        UpperThreshold = r.ReadOptionalValue(ReadDouble),
    };

    public readonly static Action<BinaryWriter, AnnotatedDataModel> AnnotatedDataSerializer = (w, i) =>
    {
        w.WriteOptional(WriteString, i.AnnotatedName);
        w.WriteOptionalValue(WriteDouble, i.LowerThreshold);
        w.WriteOptionalValue(WriteDouble, i.UpperThreshold);
    };

    public readonly static Func<BinaryReader, SensorDataModel> SensorDataDeserializer = (r) =>
    {
        var measurementIdentifier = r.ReadString();
        var sensorNames = r.ReadEnumerable(ReadString).ToImmutableArray();
        var sampleTimes = r.ReadEnumerable(ReadDateTime).ToImmutableArray();
        var samples = r.ReadRawEnumerable(ReadDouble, sensorNames.Length * sampleTimes.Length);

        return new SensorDataModel
        {
            MeasurementIdentifier = measurementIdentifier,
            SensorNames = sensorNames,
            SampleTimes = sampleTimes,
            Samples = new(samples, sensorNames.Length, sampleTimes.Length),
        };
    };

    public readonly static Action<BinaryWriter, SensorDataModel> SensorDataSerializer = (w, i) =>
    {
        w.Write(i.MeasurementIdentifier);
        w.WriteEnumerable(WriteString, i.SensorNames);
        w.WriteEnumerable(WriteDateTime, i.SampleTimes);
        w.WriteRawEnumerable(WriteDouble, i.Samples.EnumerateFlat());
    };

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

    public readonly static Action<BinaryWriter, DatasetModel> DatasetSerializer = (w, i) =>
    {
        w.Write(MagicSignature);
        w.Write(LatestFormatVersion);

        w.Write(AnnotatedDataSerializer, i.AnnotatedData);
        w.Write(SensorDataSerializer, i.SensorData);
    };
}

public static class Serialization
{
    public static void Write<T>(
        this BinaryWriter writer,
        Action<BinaryWriter, T> serializer,
        T value
    ) => serializer(writer, value);

    public static T Read<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer
    ) => deserializer(reader);

    public static void WriteOptional<T>(
        this BinaryWriter writer,
        Action<BinaryWriter, T> serializer,
        T? value
    ) where T : class
    {
        writer.Write(value != null);
        if (value != null) serializer(writer, value);
    }

    public static T? ReadOptional<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer
    ) where T : class => reader.ReadBoolean() ? deserializer(reader) : null;

    public static void WriteOptionalValue<T>(
        this BinaryWriter writer,
        Action<BinaryWriter, T> serializer,
        T? value
    ) where T : struct
    {
        writer.Write(value.HasValue);
        if (value.HasValue) serializer(writer, value.Value);
    }

    public static T? ReadOptionalValue<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer
    ) where T : struct => reader.ReadBoolean() ? deserializer(reader) : null;

    public static void WriteEnumerable<T>(
        this BinaryWriter writer,
        Action<BinaryWriter, T> serializer,
        IEnumerable<T> series
    )
    {
        writer.Write(series.Count());
        foreach (var item in series) serializer(writer, item);
    }

    public static IEnumerable<T> ReadEnumerable<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer
    )
    {
        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++) yield return deserializer(reader);
    }

    public static void WriteRawEnumerable<T>(
        this BinaryWriter writer,
        Action<BinaryWriter, T> serializer,
        IEnumerable<T> series
    )
    {
        foreach (var item in series) serializer(writer, item);
    }

    public static IEnumerable<T> ReadRawEnumerable<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer,
        int length
    )
    {
        for (var i = 0; i < length; i++) yield return deserializer(reader);
    }
}
