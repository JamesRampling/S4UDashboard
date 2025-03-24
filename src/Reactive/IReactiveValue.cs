using System.ComponentModel;

namespace S4UDashboard.Reactive;

public interface IReactiveValue<T> : INotifyPropertyChanged
{
    public T Value { get; }
    public void TriggerEffects();
}
