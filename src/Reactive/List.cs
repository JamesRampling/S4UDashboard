using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace S4UDashboard.Reactive;

public partial class ReactiveList<T> : INotifyPropertyChanged, INotifyCollectionChanged
{
    public ReactiveList() => _inner = [];
    public ReactiveList(IEnumerable<T> initial) => _inner = new(initial);

    protected List<T> _inner;
    public List<T> Raw => _inner;

    protected void TrackBacking() => EffectManager.Track(this, "Item[]");
    protected void TriggerBacking(NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Replace &&
            e.Action != NotifyCollectionChangedAction.Move)
        {
            EffectManager.Trigger(this, nameof(Count));
            EffectManager.PauseTracking(() => PropertyChanged?.Invoke(this, new(nameof(Count))));
        }

        EffectManager.Trigger(this, "Item[]");
        EffectManager.PauseTracking(() => CollectionChanged?.Invoke(this, e));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
}

partial class ReactiveList<T> : IList<T>
{
    public int Count
    {
        get
        {
            EffectManager.Track(this, nameof(Count));
            return _inner.Count;
        }
    }

    public T this[int index]
    {
        get
        {
            TrackBacking();
            return _inner[index];
        }
        set
        {
            var old = _inner[index];
            var current = _inner[index] = value;

            TriggerBacking(new(NotifyCollectionChangedAction.Replace, old, current, index));
        }
    }

    public int IndexOf(T item)
    {
        TrackBacking();
        return _inner.IndexOf(item);
    }

    public void Add(T item)
    {
        _inner.Add(item);
        TriggerBacking(new(NotifyCollectionChangedAction.Add, item, _inner.Count - 1));
    }

    public void Insert(int index, T item)
    {
        _inner.Insert(index, item);
        TriggerBacking(new(NotifyCollectionChangedAction.Add, item, index));
    }

    public bool Remove(T item)
    {
        if (!_inner.Remove(item)) return false;
        TriggerBacking(new(NotifyCollectionChangedAction.Remove, item, _inner.Count));

        return true;
    }

    public void RemoveAt(int index)
    {
        var removed = _inner[index];
        _inner.RemoveAt(index);
        TriggerBacking(new(NotifyCollectionChangedAction.Remove, removed, index));
    }

    public void Clear()
    {
        _inner.Clear();
        TriggerBacking(new(NotifyCollectionChangedAction.Reset));
    }

    public bool Contains(T item)
    {
        TrackBacking();
        return _inner.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        TrackBacking();
        _inner.CopyTo(array, arrayIndex);
    }

    public IEnumerator<T> GetEnumerator()
    {
        TrackBacking();
        return _inner.GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

partial class ReactiveList<T> : IList
{
    protected IList InnerList => _inner;

    object? IList.this[int index]
    {
        get
        {
            TrackBacking();
            return InnerList[index];
        }
        set
        {
            var old = InnerList[index];
            var current = InnerList[index] = value;

            TriggerBacking(new(NotifyCollectionChangedAction.Replace, old, current, index));
        }
    }

    public bool IsReadOnly => InnerList.IsReadOnly;
    public bool IsFixedSize => InnerList.IsFixedSize;
    public bool IsSynchronized => InnerList.IsSynchronized;
    public object SyncRoot => InnerList.SyncRoot;

    public int Add(object? value)
    {
        int idx;
        if ((idx = InnerList.Add(value)) == -1) return -1;
        TriggerBacking(new(NotifyCollectionChangedAction.Add, value, idx));

        return idx;
    }

    public bool Contains(object? value)
    {
        TrackBacking();
        return InnerList.Contains(value);
    }

    public void CopyTo(Array array, int index)
    {
        TrackBacking();
        InnerList.CopyTo(array, index);
    }

    public int IndexOf(object? value)
    {
        TrackBacking();
        return InnerList.IndexOf(value);
    }

    public void Insert(int index, object? value)
    {
        InnerList.Insert(index, value);
        TriggerBacking(new(NotifyCollectionChangedAction.Add, value, index));
    }

    public void Remove(object? value)
    {
        InnerList.Remove(value);
        TriggerBacking(new(NotifyCollectionChangedAction.Remove, value, _inner.Count));
    }
}
