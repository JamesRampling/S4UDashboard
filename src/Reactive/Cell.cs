using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace S4UDashboard.Reactive;

public class ReactiveCell<T>(T inner) : INotifyPropertyChanged
{
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

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class ComputedCell<T> : INotifyPropertyChanged
{
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

    public T Value
    {
        get
        {
            EffectManager.Track(this, nameof(Value));
            return _value;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
