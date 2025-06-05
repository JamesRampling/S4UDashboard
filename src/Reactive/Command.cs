using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace S4UDashboard.Reactive;

/// <summary>
/// An avalonia command that executes an effect to calculate whether
/// the command can execute or not.
/// </summary>
public class ReactiveCommand : ICommand
{
    /// <summary>Creates the effect used by the command to track reactivity in the canExecute function.</summary>
    /// <param name="canExecute">The function to execute within the effect to update CanExecute.</param>
    private Action MakeEffect(Func<bool> canExecute) => () =>
    {
        var current = canExecute();
        if (current == _last) return;

        _last = current;
        EffectManager.Trigger(this, nameof(CanExecute));
        EffectManager.GapEffectTracking(() => CanExecuteChanged?.Invoke(this, new()));
    };

    /// <summary>Creates a command.</summary>
    /// <param name="canExecute">The function to calculate whether or not the command can execute.</param>
    /// <param name="execute">The function to run when the command is executed.</param>
    public ReactiveCommand(Func<bool> canExecute, Action<object?> execute)
    {
        _execute = execute;
        EffectManager.WatchEffect(MakeEffect(canExecute));
    }

    /// <summary>Creates a command with an asynchronous execute action.</summary>
    /// <param name="canExecute">The function to calculate whether or not the command can execute.</param>
    /// <param name="execute">The asynchronous function to run when the command is executed.</param>
    public ReactiveCommand(Func<bool> canExecute, Func<object?, Task> execute)
    {
        _execute = execute;
        EffectManager.WatchEffect(MakeEffect(canExecute));
    }

    /// <summary>The last cached value of the canExecute effect.</summary>
    private bool _last;

    /// <summary>The function to be run upon execution.</summary>
    private readonly Delegate _execute;

    public bool CanExecute(object? parameter)
    {
        EffectManager.Track(this, nameof(CanExecute));
        return _last;
    }

    public void Execute(object? parameter) => _execute.DynamicInvoke(parameter);

    public event EventHandler? CanExecuteChanged;
}
