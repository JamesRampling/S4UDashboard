using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace S4UDashboard.Reactive;

/// <summary>Manages an effects based reactivity system.
/// <para>
/// This interface is heavily <i>inspired</i> by Vue 3's fundamental reactivity primitive,
/// which tracks access to reactive dependencies and executes subscribed effects when that dependency
/// is triggered (usually when the property is mutated).
/// </para>
/// </summary>
/// <seealso href="https://vuejs.org/guide/extras/reactivity-in-depth.html"/>
public static class EffectManager
{
    private readonly static ConditionalWeakTable<object, Dictionary<string, HashSet<Action>>> Subscriptions = [];
    private static Action? _activeEffect;
    private static bool _pauseTracking = false;

    /// <summary>Subscribes the active effect to the specified dependency.
    /// <para>
    /// If called when an effect is active and tracking is not paused, this will subscribe that effect
    /// to the specified dependency. The effect will then be executed when the dependency is triggered.
    /// </para>
    /// </summary>
    public static void Track(object target, string key)
    {
        if (_activeEffect != null && !_pauseTracking)
        {
            var properties = Subscriptions.GetOrCreateValue(target);
            if (!properties.ContainsKey(key)) properties[key] = [];
            properties[key].Add(_activeEffect);
        }
    }

    /// <summary>Triggers all effects subscribed to the specified dependency.
    /// <para>
    /// Executes all effects that were previously subscribed to the specified dependency.
    /// Further dependencies may be subscribed to within the effect invocation if tracking is not paused.
    /// </para>
    /// </summary>
    public static void Trigger(object target, string key)
    {
        if (!Subscriptions.TryGetValue(target, out var properties)) return;
        if (!properties.TryGetValue(key, out var effects)) return;

        foreach (var effect in effects) effect.Invoke();
    }

    /// <summary>Executes an effect immediately and subscribes it to all accessed dependencies.
    /// <para>
    /// Sets the active effect to the passed function and then executes it. This lets the <c>EffectManager</c>
    /// track the function's dependencies and subscribe to them, re-running the function when they are triggered.
    /// </para>
    /// </summary>
    public static void WatchEffect(Action update)
    {
        void Effect()
        {
            var old = _activeEffect;
            _activeEffect = Effect;
            update();
            _activeEffect = old;
        }
        Effect();
    }

    /// <summary>Pauses subscription tracking for the duration of <c>scope</c>.
    /// <para>
    /// This is useful when interacting with systems that may access tracked properties
    /// but should not be treated as dependencies (e.g. supporting <c>INotifyProperyChanged</c>).
    /// </para>
    /// </summary>
    public static void PauseTracking(Action scope)
    {
        _pauseTracking = true;
        scope();
        _pauseTracking = false;
    }
}
