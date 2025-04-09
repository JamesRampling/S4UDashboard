using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace S4UDashboard.Model;

public partial class SortedObservableView<T>(List<T> source) : INotifyPropertyChanged, INotifyCollectionChanged
{
    private static readonly NotifyCollectionChangedEventArgs ResetArgs =
        new(NotifyCollectionChangedAction.Reset);
    private ImmutableList<int>? _order;
    private Func<T, IComparable>? _selector;

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

    public void Impose()
    {
        if (Selector == null) return;
        var ordered = source.OrderBy(t => Selector(t)).ToImmutableList();

        _order = null;
        _selector = null;
        foreach (var (i, v) in ordered.Select((v, i) => (i, v))) source[i] = v;
    }

    public T this[int index] => source[Order != null ? Order[index] : index];
    public int Count => source.Count;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;
}

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
