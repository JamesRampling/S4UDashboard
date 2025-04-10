using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace S4UDashboard.Model;

public readonly struct Immutable2DArray<T> : IEnumerable<IEnumerable<T>>
{
    private readonly ImmutableArray<T> _inner;

    public readonly int Rows;
    public readonly int Columns;

    public Immutable2DArray(IEnumerable<T> source, int rows, int columns)
    {
        _inner = source.ToImmutableArray();
        Rows = rows;
        Columns = columns;

        if (_inner.Length != rows * columns)
            throw new ArgumentException("source shape did not match specified shape");
    }

    public T this[int row, int column]
    {
        get => _inner[column * Rows + row];
    }

    public int Length => _inner.Length;

    public IEnumerable<T> EnumerateFlat() => _inner.AsEnumerable();

    public IEnumerable<T> EnumerateRow(int row)
    {
        for (int column = 0; column < Columns; column++)
            yield return this[row, column];
    }
    public IEnumerable<T> EnumerateColumn(int column)
    {
        for (int row = 0; row < Rows; row++)
            yield return this[row, column];
    }

    public IEnumerator<IEnumerable<T>> GetEnumerator()
    {
        for (int row = 0; row < Rows; row++)
            yield return EnumerateRow(row);
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
