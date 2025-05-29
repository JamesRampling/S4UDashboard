using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace S4UDashboard.Reactive;

/// <summary>
/// Holds a value and tracks accesses and mutations to that value.
/// Triggers subscribing effects when the value is mutated.
/// </summary>
public class ReactiveCell<T>(T inner) : INotifyPropertyChanged
{
    /// <summary>
    /// The value this cell holds. Accesses and updates are tracked.
    /// </summary>
    public T Value
    {
        get
        {
            EffectManager.Track(this, nameof(Value));
            return inner;
        }
        set
        {
            if (EqualityComparer<T>.Default.Equals(inner, value)) return;
            inner = value;

            EffectManager.Trigger(this, nameof(Value));
            EffectManager.GapEffectTracking(() => PropertyChanged?.Invoke(this, new(nameof(Value))));
        }
    }

    /// <summary>A property changed event triggered when the value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>Represents a reactive computation.
/// <para>
/// When one of the dependencies of the computation is updated,
/// this reexecutes the computation and updates the value.
/// </para>
/// </summary>
public class ComputedCell<T> : INotifyPropertyChanged
{
    /// <summary>Create a reactive computation from a function.
    /// <para>
    /// Immediately executes <c>update</c> within a reactive effect.
    /// Whenever a dependency of <c>update</c> is triggered, it will rerun and
    /// if the return value differs, the computed value will be updated.
    /// </para>
    /// </summary>
    public ComputedCell(Func<T> update)
    {
        EffectManager.WatchEffect(() =>
        {
            var value = update();
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;
            _value = update();

            EffectManager.Trigger(this, nameof(Value));
            EffectManager.GapEffectTracking(() => PropertyChanged?.Invoke(this, new(nameof(Value))));
        });
    }

    private T _value = default!;

    /// <summary>The result of the computation. Accesses are tracked.</summary>
    public T Value
    {
        get
        {
            EffectManager.Track(this, nameof(Value));
            return _value;
        }
    }

    /// <summary>A property changed event triggered when the value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;
}
