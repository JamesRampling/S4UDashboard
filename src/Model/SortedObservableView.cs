using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace S4UDashboard.Model;

/// <summary>An observable list that can be sorted.</summary>
public partial class SortedObservableView<T>(List<T> source) : INotifyPropertyChanged, INotifyCollectionChanged
{
    /// <summary>Static instance of event args to optimise object reuse.</summary>
    private static readonly NotifyCollectionChangedEventArgs ResetArgs = new(NotifyCollectionChangedAction.Reset);

    /// <summary>A list of indices that represents the sorted order.</summary>
    private ImmutableList<int>? _order;

    /// <summary>The function used to select the property of the items to be sorted.</summary>
    private Func<T, IComparable>? _selector;

    /// <summary>A list of indices that represents the sorted order, or null if there is no selector.</summary>
    public IReadOnlyList<int>? Order
    {
        get
        {
            if (Selector == null) return null;

            _order ??= source
                .Select((v, i) => (i, v))
                .OrderBy(t => Selector(t.v))
                .Select(t => t.i)
                .ToImmutableList();

            return _order;
        }
    }

    /// <summary>The function used to select the property of the items to be sorted.</summary>
    public Func<T, IComparable>? Selector
    {
        get => _selector;
        set
        {
            _selector = value;
            _order = null;
            CollectionChanged?.Invoke(this, ResetArgs);
        }
    }

    /// <summary>Imposes the sort order of the selector onto the backing list.</summary>
    public void Impose()
    {
        if (Selector == null) return;
        var ordered = source.OrderBy(t => Selector(t)).ToImmutableList();

        _order = null;
        _selector = null;
        foreach (var (i, v) in ordered.Select((v, i) => (i, v))) source[i] = v;
    }

    /// <summary>Gets the element at the index, according to the sorted order.</summary>
    public T this[int index] => source[Order != null ? Order[index] : index];

    /// <summary>The number of elements in the list.</summary>
    public int Count => source.Count;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;
}

// implementation of an interface; does not require doc comments
partial class SortedObservableView<T> : IList<T>
{
    public bool IsReadOnly => false;

    T IList<T>.this[int index]
    {
        get => this[index];
        set
        {
            source[Order != null ? Order[index] : index] = value;
            _order = null;

            PropertyChanged?.Invoke(this, new("Item[]"));
            CollectionChanged?.Invoke(this, ResetArgs);
        }
    }

    public void Add(T item)
    {
        source.Add(item);
        _order = null;

        PropertyChanged?.Invoke(this, new(nameof(Count)));
        PropertyChanged?.Invoke(this, new("Item[]"));
        CollectionChanged?.Invoke(this, ResetArgs);
    }

    public void Clear()
    {
        source.Clear();
        _order = null;

        PropertyChanged?.Invoke(this, new(nameof(Count)));
        PropertyChanged?.Invoke(this, new("Item[]"));
        CollectionChanged?.Invoke(this, ResetArgs);
    }

    public bool Contains(T item) => source.Contains(item);
    public void CopyTo(T[] array, int arrayIndex) => source.CopyTo(array, arrayIndex);

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < source.Count; i++)
            yield return this[i];
    }

    public int IndexOf(T item)
    {
        for (int i = 0; i < source.Count; i++)
            if (EqualityComparer<T>.Default.Equals(this[i], item)) return i;
        return -1;
    }

    public void Insert(int index, T item)
    {
        source.Insert(index, item);
        _order = null;

        PropertyChanged?.Invoke(this, new(nameof(Count)));
        PropertyChanged?.Invoke(this, new("Item[]"));
        CollectionChanged?.Invoke(this, ResetArgs);
    }

    public bool Remove(T item)
    {
        if (!source.Remove(item)) return false;
        _order = null;

        PropertyChanged?.Invoke(this, new(nameof(Count)));
        PropertyChanged?.Invoke(this, new("Item[]"));
        CollectionChanged?.Invoke(this, ResetArgs);
        return true;
    }

    public void RemoveAt(int index)
    {
        source.RemoveAt(index);
        _order = null;

        PropertyChanged?.Invoke(this, new(nameof(Count)));
        PropertyChanged?.Invoke(this, new("Item[]"));
        CollectionChanged?.Invoke(this, ResetArgs);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// implementation of an interface; does not require doc comments
partial class SortedObservableView<T> : IList
{
    public bool IsFixedSize => false;
    public bool IsSynchronized => ((IList)source).IsSynchronized;
    public object SyncRoot => ((IList)source).SyncRoot;

    object? IList.this[int index]
    {
        get => this[index];
        set => ((IList<T>)this)[index] = (T)value!;
    }

    public int Add(object? value)
    {
        var idx = source.Count;
        Add((T)value!);

        if (Order == null) return idx;
        else
        {
            for (int i = 0; i < Order.Count; i++)
                if (Order[i] == idx) return i;
            return -1;
        }
    }

    public bool Contains(object? value) => Contains((T)value!);
    public void CopyTo(Array array, int index) => ((IList)source).CopyTo(array, index);
    public int IndexOf(object? value) => IndexOf((T)value!);
    public void Insert(int index, object? value) => Insert(index, (T)value!);
    public void Remove(object? value) => Remove((T)value!);
}
