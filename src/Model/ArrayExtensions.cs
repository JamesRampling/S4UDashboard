using System.Collections.Generic;
using System.Linq;

namespace S4UDashboard.Model;

/// <summary>
/// An extension class with utilities for handling 2D arrays.
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    /// Enumerates over all of the items in a 2D array one by one.
    /// </summary>
    public static IEnumerable<T> EnumerateFlat<T>(this T[,] array)
    {
        foreach (var item in array) yield return item;
    }

    /// <summary>
    /// Enumerates over all of the items in a single row of a 2D array.
    /// </summary>
    public static IEnumerable<T> EnumerateRow<T>(this T[,] array, int row)
    {
        for (int column = 0; column < array.GetLength(0); column++)
            yield return array[column, row];
    }

    /// <summary>
    /// Enumerates over all of the items in a single column of a 2D array.
    /// </summary>
    public static IEnumerable<T> EnumerateColumn<T>(this T[,] array, int column)
    {
        for (int row = 0; row < array.GetLength(1); row++)
            yield return array[column, row];
    }

    /// <summary>
    /// Enumerates over all of the rows of a 2D array and for each yields an
    /// IEnumerable that enumerates over all of the columns in the given row.
    /// </summary>
    public static IEnumerable<IEnumerable<T>> EnumerateGrid<T>(this T[,] array)
    {
        for (int row = 0; row < array.GetLength(1); row++)
            yield return array.EnumerateRow(row);
    }

    /// <summary>
    /// Given a nested IEnumerable and a number of columns and rows, converts
    /// the IEnumerable into a 2D array where each first order enumerable is a column.
    /// </summary>
    public static T[,] To2DArray<T>(this IEnumerable<IEnumerable<T>> enumerable, int columns, int rows)
    {
        var array = new T[columns, rows];
        foreach (var (colIdx, column) in enumerable.Select((x, i) => (i, x)))
        {
            foreach (var (rowIdx, value) in column.Select((x, i) => (i, x)))
            {
                array[colIdx, rowIdx] = value;
            }
        }
        return array;
    }

    /// <summary>
    /// Given a flat IEnumerable and a number of columns and rows, converts
    /// the IEnumerable into a 2D array where each chunk of <c>rows</c> elements
    /// make a column.
    /// </summary>
    public static T[,] To2DArray<T>(this IEnumerable<T> enumerable, int columns, int rows) =>
        enumerable.Chunk(rows).To2DArray<T>(columns, rows);
}
