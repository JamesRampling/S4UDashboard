using System;
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
            inner = value;

            EffectManager.Trigger(this, nameof(Value));
            EffectManager.PauseTracking(() => PropertyChanged?.Invoke(this, new(nameof(Value))));
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
            _value = update();

            EffectManager.Trigger(this, nameof(Value));
            EffectManager.PauseTracking(() => PropertyChanged?.Invoke(this, new(nameof(Value))));
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
