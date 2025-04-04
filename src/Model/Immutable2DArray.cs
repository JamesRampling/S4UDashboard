using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace S4UDashboard.Model;

public readonly struct Immutable2DArray<T>
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
        get => _inner[row * Columns + column];
    }

    public IEnumerable<T> EnumerateFlat() => _inner.AsEnumerable();
}
