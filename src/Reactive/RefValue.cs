using System.ComponentModel;

namespace S4UDashboard.Reactive;

public class RefValue<T>(T inner) : IReactiveValue<T>
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public T Value
    {
        get => inner;
        set
        {
            inner = value;
            TriggerEffects();
        }
    }

    public void TriggerEffects() => PropertyChanged?.Invoke(this, new(nameof(Value)));
}
