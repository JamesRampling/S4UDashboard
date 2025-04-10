using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace S4UDashboard.Model;

public static class SampleGenerator
{
    private static readonly Random Rng = new();

    public static readonly GeneratorProfile DefaultProfile = new GeneratorProfile
    {
        SensorNames = [
            "kitchen",
            "garden",
            "master bedroom",
            "guest room",
            "living room",
            "office",
            "dining room",
            "attic",
            "bathroom",
            "shed",
            "garage",
            "workshop",
            "veranda",
            "hall",
            "laundry room",
        ],
        SensorProfiles = [
            new SensorProfile
            {
                MeasurementIdentifier = "temperature",
                NoiseProfile = new FBMProfile
                {
                    Octaves = 4,
                    Lacunarity = 2,
                    Gain = 0.5,
                    InitialAmplitude = 20,
                    InitialFrequency = 4,
                    NoiseFn = d => (Math.Sin(d) + 1) / 2,
                },
            },
            new SensorProfile
            {
                MeasurementIdentifier = "humidity (relative)",
                NoiseProfile = new FBMProfile
                {
                    Octaves = 4,
                    Lacunarity = 2,
                    Gain = 0.5,
                    InitialAmplitude = 0.4,
                    InitialFrequency = 4,
                    NoiseFn = d => (Math.Sin(d + Math.Tau / 3) + 1) / 2,
                },
            },
        ],
    };

    // The noise generation in this function isn't strictly /good/, but it's mostly suitable for our purposes.
    public static SensorDataModel GenerateSensorData(GeneratorProfile profile, int sensorsCount, int samplesPerSensor)
    {
        if (sensorsCount > profile.SensorNames.Length)
            throw new ArgumentException("not enough sensor names in profile to satisfy request");

        var sensorProfile = profile.SensorProfiles.Pick();
        var possibleNames = profile.SensorNames.ToList();

        var names = Enumerable.Range(0, sensorsCount).Select(_ => possibleNames.PickAndRemove());

        var offset = Rng.NextDouble();
        var mult = 1 + (Rng.NextDouble() - 0.5) / 2.5;

        var sensors = Enumerable
            .Range(0, sensorsCount)
            .Select(c =>
                GenerateColumn(sensorProfile, samplesPerSensor, x => x / samplesPerSensor + c + offset)
                    .Select(v => Math.Round(v * mult * 1e3) / 1e3)
            );

        var samples = new Immutable2DArray<double>(sensors.SelectMany(x => x), samplesPerSensor, sensorsCount);

        return new SensorDataModel
        {
            MeasurementIdentifier = sensorProfile.MeasurementIdentifier,
            SensorNames = names.Order().ToImmutableArray(),
            Samples = samples,
        };
    }

    private static IEnumerable<double> GenerateColumn(
        SensorProfile profile,
        int samplesPerSensor,
        Func<double, double> transform
    ) => Enumerable
        .Range(0, samplesPerSensor)
        .Select(x => FractionalBrownianMotion(transform(x), profile.NoiseProfile));

    private static double FractionalBrownianMotion(double x, FBMProfile profile)
    {
        double t = 0;

        double ampl = profile.InitialAmplitude;
        double freq = profile.InitialFrequency;

        for (int i = 0; i < profile.Octaves; i++)
        {
            t += ampl * profile.NoiseFn(freq * x);
            freq *= profile.Lacunarity;
            ampl *= profile.Gain;
        }

        return t;
    }

    private static T Pick<T>(this IReadOnlyList<T> list) =>
        list[Rng.Next(0, list.Count)];

    private static T PickAndRemove<T>(this IList<T> list)
    {
        var idx = Rng.Next(0, list.Count);
        var result = list[idx];
        list.RemoveAt(idx);
        return result;
    }
}

public readonly record struct GeneratorProfile(
    ImmutableArray<string> SensorNames,
    ImmutableArray<SensorProfile> SensorProfiles
);

public readonly record struct SensorProfile(
    string MeasurementIdentifier,
    FBMProfile NoiseProfile
);

public readonly record struct FBMProfile(
    int Octaves,
    double Lacunarity,
    double Gain,
    double InitialAmplitude,
    double InitialFrequency,
    Func<double, double> NoiseFn
);
