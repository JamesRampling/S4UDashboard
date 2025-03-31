using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace S4UDashboard.Model;

public static class Serializers
{
    /* This is "S4UD" in ASCII */
    public readonly static int MagicSignature = 0x53_34_55_44;
    /* The current version of the serialisation format */
    public readonly static int LatestFormatVersion = 1;

    public readonly static Func<BinaryReader, AnnotatedDataModel> AnnotatedDataDeserializer = (r) => new AnnotatedDataModel
    {
        AnnotatedName = r.ReadOptional(r => r.ReadString()),
        LowerThreshold = r.ReadOptionalValue(r => r.ReadDouble()),
        UpperThreshold = r.ReadOptionalValue(r => r.ReadDouble()),
    };

    public readonly static Action<BinaryWriter, AnnotatedDataModel> AnnotatedDataSerializer = (w, i) =>
    {
        w.WriteOptional((w, i) => w.Write(i), i.AnnotatedName);
        w.WriteOptionalValue((w, i) => w.Write(i), i.LowerThreshold);
        w.WriteOptionalValue((w, i) => w.Write(i), i.UpperThreshold);
    };

    public readonly static Func<BinaryReader, SensorDataModel> SensorDataDeserializer = (r) => new SensorDataModel
    {
        MeasurementIdentifier = r.ReadString(),
        SensorNames = r.ReadEnumerable(r => r.ReadString()).ToImmutableArray(),
        SampleTimes = r.ReadEnumerable(r => DateTime.FromBinary(r.ReadInt64())).ToImmutableArray(),
        /* TODO: this should be replaced with a flat array */
        Samples = r.ReadEnumerable(r => r.ReadEnumerable(r => r.ReadDouble()).ToImmutableArray()).ToImmutableArray(),
    };

    public readonly static Action<BinaryWriter, SensorDataModel> SensorDataSerializer = (w, i) =>
    {
        w.Write(i.MeasurementIdentifier);
        w.WriteEnumerable((w, i) => w.Write(i), i.SensorNames);
        w.WriteEnumerable((w, i) => w.Write(i.ToBinary()), i.SampleTimes);
        w.WriteEnumerable((w, i) => w.WriteEnumerable((w, i) => w.Write(i), i), i.Samples);
    };

    public static Func<BinaryReader, DatasetModel> DatasetDeserializer = (r) =>
    {
        int version;

        if (r.ReadInt32() != MagicSignature) throw new Exception("magic number did not match!");
        if ((version = r.ReadInt32()) != LatestFormatVersion) throw new Exception("format version did not match!");

        var annotatedData = r.Read(AnnotatedDataDeserializer);
        var sensorData = r.Read(SensorDataDeserializer);
        var calculatedData = DataProcessing.Instance.CalculateAuxilliaryData(sensorData);

        return new DatasetModel
        {
            FilePath = "/foo",
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
    ) => serializer.Invoke(writer, value);

    public static T Read<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer
    ) => deserializer.Invoke(reader);

    public static void WriteOptional<T>(
        this BinaryWriter writer,
        Action<BinaryWriter, T> serializer,
        T? value
    ) where T : class
    {
        writer.Write(value != null);
        if (value != null) serializer.Invoke(writer, value);
    }

    public static T? ReadOptional<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer
    ) where T : class => reader.ReadBoolean() ? deserializer.Invoke(reader) : null;

    public static void WriteOptionalValue<T>(
        this BinaryWriter writer,
        Action<BinaryWriter, T> serializer,
        T? value
    ) where T : struct
    {
        writer.Write(value.HasValue);
        if (value.HasValue) serializer.Invoke(writer, value.Value);
    }

    public static T? ReadOptionalValue<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer
    ) where T : struct => reader.ReadBoolean() ? deserializer.Invoke(reader) : null;

    public static void WriteEnumerable<T>(
        this BinaryWriter writer,
        Action<BinaryWriter, T> serializer,
        IEnumerable<T> series
    )
    {
        writer.Write(series.Count());
        foreach (var item in series) serializer.Invoke(writer, item);
    }

    public static IEnumerable<T> ReadEnumerable<T>(
        this BinaryReader reader,
        Func<BinaryReader, T> deserializer
    )
    {
        var length = reader.ReadInt32();
        for (var i = 0; i < length; i++) yield return deserializer.Invoke(reader);
    }
}
