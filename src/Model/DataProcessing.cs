using System;
using System.Collections.Generic;
using System.Linq;

namespace S4UDashboard.Model;

public class DataProcessing
{
    private DataProcessing() { }
    public readonly static DataProcessing Instance = new();

    /// <summary>
    /// Given a <c>SensorDataModel</c> constructs and returns a <c>CalculatedDataModel</c> based
    /// on the samples contained in <c>sensorData</c>.
    /// </summary>
    public CalculatedDataModel CalculateAuxilliaryData(SensorDataModel sensorData)
    {
        double min = double.PositiveInfinity, max = double.NegativeInfinity, sum = 0.0;

        foreach (var sample in sensorData.Samples.EnumerateFlat())
        {
            if (sample < min) min = sample;
            if (sample > max) max = sample;
            sum += sample;
        }

        int len = sensorData.SensorNames.Length * sensorData.SampleTimes.Length;
        return new CalculatedDataModel
        {
            Mean = sum / len,
            Minimum = min,
            Maximum = max,
        };
    }

    /// <summary>
    /// Performs a binary search on <c>sorted</c>, comparing <c>needle</c> to each element
    /// after having been mapped by <c>selector</c>.
    /// </summary>
    public int FindByInSorted<T, S>(IList<T> sorted, Func<T, S> selector, S needle) where S : IComparable<S>
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
