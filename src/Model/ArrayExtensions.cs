using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace S4UDashboard.Model;

public static class ArrayExtensions
{
    public static IEnumerable<T> EnumerateFlat<T>(this T[,] array)
    {
        foreach (var item in array) yield return item;
    }

    public static IEnumerable<T> EnumerateRow<T>(this T[,] array, int row)
    {
        for (int column = 0; column < array.GetLength(0); column++)
            yield return array[column, row];
    }
    public static IEnumerable<T> EnumerateColumn<T>(this T[,] array, int column)
    {
        for (int row = 0; row < array.GetLength(1); row++)
            yield return array[column, row];
    }

    public static IEnumerable<IEnumerable<T>> EnumerateGrid<T>(this T[,] array)
    {
        for (int row = 0; row < array.GetLength(1); row++)
            yield return array.EnumerateRow(row);
    }

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

    public static T[,] To2DArray<T>(this IEnumerable<T> enumerable, int columns, int rows) =>
        enumerable.Chunk(rows).To2DArray<T>(columns, rows);
}
