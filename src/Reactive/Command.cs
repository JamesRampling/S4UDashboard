using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace S4UDashboard.Reactive;

public class ReactiveCommand : ICommand
{
    private Action MakeEffect(Func<bool> canExecute) => () =>
    {
        var current = canExecute();
        if (current == _last) return;

        _last = current;
        EffectManager.Trigger(this, nameof(CanExecute));
        EffectManager.GapEffectTracking(() => CanExecuteChanged?.Invoke(this, new()));
    };

    public ReactiveCommand(Func<bool> canExecute, Action<object?> execute)
    {
        _execute = execute;
        EffectManager.WatchEffect(MakeEffect(canExecute));
    }

    public ReactiveCommand(Func<bool> canExecute, Func<object?, Task> execute)
    {
        _execute = execute;
        EffectManager.WatchEffect(MakeEffect(canExecute));
    }

    private bool _last;
    private readonly Delegate _execute;

    public bool CanExecute(object? parameter)
    {
        EffectManager.Track(this, nameof(CanExecute));
        return _last;
    }

    public void Execute(object? parameter) => _execute.DynamicInvoke(parameter);

    public event EventHandler? CanExecuteChanged;
}
