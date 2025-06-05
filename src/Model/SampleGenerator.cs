using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace S4UDashboard.Model;

/// <summary>A utility class to generate sample datasets.
/// <para>
/// Uses profiles that allow parametrising the names of sensors, the types of
/// measurements they make, and the noise function for the samples.
/// </para>
/// </summary>
public static class SampleGenerator
{
    /// <summary>The random number generator instance for the sample generator.</summary>
    private static readonly Random Rng = new();

    /// <summary>The default generator profile for sample data.</summary>
    public static readonly GeneratorProfile DefaultProfile = new()
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

    /// <summary>
    /// Generates a sensor data component for the specified generator profile with the requested
    /// number of sensors and samples per sensor.
    /// </summary>
    /// <param name="profile">The generator profile to generate from.</param>
    /// <param name="sensorsCount">The number of sensors (or columns) to generate.</param>
    /// <param name="samplesPerSensor">The number of samples (or rows) to generate per sensor.</param>
    public static SensorDataModel GenerateSensorData(GeneratorProfile profile, int sensorsCount, int samplesPerSensor)
    {
        // The noise generation in this function isn't strictly /good/, but it's mostly suitable for our purposes.

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

        var samples = sensors.To2DArray(sensorsCount, samplesPerSensor);

        return new SensorDataModel
        {
            MeasurementIdentifier = sensorProfile.MeasurementIdentifier,
            SensorNames = [.. names.Order()],
            Samples = samples,
        };
    }

    /// <summary>
    /// Generates a column of samples.
    /// </summary>
    /// <param name="profile">The sensor profile to generate from.</param>
    /// <param name="samplesPerSensor">The number of samples (or rows) to generate.</param>
    /// <param name="transform">A transformation function to offset the noise coordinate.</param>
    private static IEnumerable<double> GenerateColumn(
        SensorProfile profile,
        int samplesPerSensor,
        Func<double, double> transform
    ) => Enumerable
        .Range(0, samplesPerSensor)
        .Select(x => FractionalBrownianMotion(transform(x), profile.NoiseProfile));

    /// <summary>
    /// Generates a single sample with fractional brownian motion.
    /// </summary>
    /// <param name="x">The coordinate of the sample to generate.</param>
    /// <param name="profile">The FBM profile to generate from.</param>
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

    /// <summary>
    /// Picks an element out of a list at random.
    /// </summary>
    /// <param name="list">The list to pick from.</param>
    private static T Pick<T>(this IReadOnlyList<T> list) =>
        list[Rng.Next(0, list.Count)];

    /// <summary>
    /// Picks an element out of a list at random and removes it from the list.
    /// </summary>
    /// <param name="list">The list to pick from.</param>
    private static T PickAndRemove<T>(this IList<T> list)
    {
        var idx = Rng.Next(0, list.Count);
        var result = list[idx];
        list.RemoveAt(idx);
        return result;
    }
}

/// <summary>A sample data generator profile.
/// <para>
/// Contains a list of potential sensor names, and a list of potential sensor profiles.
/// </para>
/// </summary>
public readonly record struct GeneratorProfile(
    /// <summary>An array of possible sensor names for the profile.</summary>
    ImmutableArray<string> SensorNames,

    /// <summary>An array of possible sensor profiles for the generator profile.</summary>
    ImmutableArray<SensorProfile> SensorProfiles
);

/// <summary>Represents an arbitrary sensor as a unit of measurement and a noise profile.</summary>
public readonly record struct SensorProfile(
    /// <summary>A string representing the kind of measurement this sensor records.</summary>
    string MeasurementIdentifier,

    /// <summary>The FBM profile of the sensor.</summary>
    FBMProfile NoiseProfile
);

/// <summary>A noise profile for Fractional Brownian Motion.</summary>
public readonly record struct FBMProfile(
    /// <summary>The numbers of octaves of noise to layer.</summary>
    int Octaves,

    /// <summary>The rate at which frequency changes as a multiplier per octave.</summary>
    double Lacunarity,

    /// <summary>The rate at which amplitude changes as a multiplier per octave.</summary>
    double Gain,

    /// <summary>The initial amplitude of the noise.</summary>
    double InitialAmplitude,

    /// <summary>The initial frequency of the noise.</summary>
    double InitialFrequency,

    /// <summary>A one dimensional noise function to generate each octave of noise.</summary>
    Func<double, double> NoiseFn
);
