using System;
using System.Windows.Input;

namespace S4UDashboard.Reactive;

public class ReactiveCommand : ICommand
{
    public ReactiveCommand(Func<bool> canExecute, Action<object?> execute)
    {
        _execute = execute;

        EffectManager.WatchEffect(() =>
        {
            var current = canExecute();
            if (current == _last) return;

            _last = current;
            EffectManager.Trigger(this, nameof(CanExecute));
            EffectManager.GapEffectTracking(() => CanExecuteChanged?.Invoke(this, new()));
        });
    }

    private bool _last;
    private readonly Action<object?> _execute;

    public bool CanExecute(object? parameter)
    {
        EffectManager.Track(this, nameof(CanExecute));
        return _last;
    }

    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged;
}
