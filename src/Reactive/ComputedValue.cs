using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace S4UDashboard.Reactive;

public class ComputedValue<T> : IReactiveValue<T>
{
    public static ComputedValueBuilder<T> Builder() => new();

    public event PropertyChangedEventHandler? PropertyChanged;
    public T Value
    {
        get
        {
            if (_stale)
            {
                _cache = Compute();
                _stale = false;
            }
            return _cache!;
        }
    }

    private T? _cache = default;
    private bool _stale = true;

    private readonly Delegate _callFunc;

    internal ComputedValue(Expression expr, IEnumerable<INotifyPropertyChanged> dependencies)
    {
        foreach (INotifyPropertyChanged dependency in dependencies)
        {
            dependency.PropertyChanged += (o, e) => Invalidate();
        }

        _callFunc = Expression.Lambda(expr).Compile();
    }

    public T Compute() => (T)_callFunc.DynamicInvoke()!;
    public void TriggerEffects() => PropertyChanged?.Invoke(this, new(nameof(Value)));
    public void Invalidate()
    {
        _stale = true;
        TriggerEffects();
    }
}

public readonly struct ComputedValueBuilder<R>
{
    private readonly Type[] _parameterTypes;
    private readonly Expression[] _parameterExpressions;
    private readonly INotifyPropertyChanged[] _dependencyProviders;

    public ComputedValueBuilder() : this([], [], []) { }
    private ComputedValueBuilder(
        Type[] parameterTypes,
        Expression[] parameterExpressions,
        INotifyPropertyChanged[] dependencyProviders
    )
    {
        _parameterTypes = parameterTypes;
        _parameterExpressions = parameterExpressions;
        _dependencyProviders = dependencyProviders;
    }

    public readonly ComputedValueBuilder<R> WithDependency<T>(IReactiveValue<T> value)
    {
        return new ComputedValueBuilder<R>(
            [.. _parameterTypes, typeof(T)],
            [
                .. _parameterExpressions,
                Expression.Property(
                    Expression.Constant(value),
                    nameof(IReactiveValue<T>.Value)
                ),
            ],
            [.. _dependencyProviders, value]);
    }

    public readonly ComputedValue<R> Build(Delegate func)
    {
        if (func.GetType() != Expression.GetDelegateType([.. _parameterTypes, typeof(R)]))
        {
            throw new Exception("type mismatch in ComputedValueBuilder");
        }

        var call = func.Target == null
            ? Expression.Call(func.Method, _parameterExpressions)
            : Expression.Call(Expression.Constant(func.Target), func.Method, _parameterExpressions);

        return new(call, _dependencyProviders);
    }
}
