using System.Collections.ObjectModel;
using System.ComponentModel;

namespace S4UDashboard.Reactive;

public class RefCollection<T> : IReactiveValue<ObservableCollection<T>>
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<T> Value => _inner;

    private ObservableCollection<T> _inner;

    public RefCollection(ObservableCollection<T> inner)
    {
        inner.CollectionChanged += (o, e) => TriggerEffects();
        _inner = inner;
    }

    public void TriggerEffects() => PropertyChanged?.Invoke(this, new(nameof(Value)));
}
