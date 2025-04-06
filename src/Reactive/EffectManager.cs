using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    private readonly static Stack<Action?> EffectStack = [];

    /// <summary>Subscribes the active effect to the specified dependency.
    /// <para>
    /// If called when an effect is active and not null, this will subscribe that effect to the
    /// specified dependency. The effect will then be executed when the dependency is triggered.
    /// </para>
    /// </summary>
    public static void Track(object target, string key)
    {
        if (EffectStack.TryPeek(out var activeEffect) && activeEffect != null)
        {
            var properties = Subscriptions.GetOrCreateValue(target);
            if (!properties.ContainsKey(key)) properties[key] = [];
            properties[key].Add(activeEffect);
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
            EffectStack.Push(Effect);
            update();
            EffectStack.Pop();
        }
        Effect();
    }

    /// <summary>
    /// Watches a source and executes an effect when it is triggered.
    /// </summary>
    public static void Watch<T>(Func<T> source, Action<T> effect)
    {
        EffectStack.Push(() => effect(source()));
        source();
        EffectStack.Pop();
    }

    /// <summary>Watches an array of sources an executes an effect when they are triggered.
    /// <para>
    /// The sources and effect are untyped delegates, and as such the caller must take care that
    /// the parameters of the effect match the values returned by the sources.
    /// </para>
    /// </summary>
    public static void Watch(Delegate[] sources, Delegate effect)
    {
        void Effect()
        {
            object?[] parameters = sources.Select(s => s.DynamicInvoke()).ToArray();
            effect.DynamicInvoke(parameters);
        }

        EffectStack.Push(Effect);
        foreach (var source in sources) source.DynamicInvoke();
        EffectStack.Pop();
    }

    /// <summary>Makes it such that there is no active effect at the start of <c>scope</c>.
    /// <para>
    /// This is used for preventing cycles when a reactive object may interact with something
    /// that may access that object (or a dependent object) but should not enact a dependency
    /// on the object (since this would cause a cycle).
    /// </para>
    /// </summary>
    public static void GapEffectTracking(Action scope)
    {
        EffectStack.Push(null);
        scope();
        EffectStack.Pop();
    }

    /// <summary>
    /// Causes a trigger on an object that implements <c>INotifyPropertyChanged</c>
    /// when the event is raised.
    /// </summary>
    public static void ShimPropertyChanged(INotifyPropertyChanged source) =>
        source.PropertyChanged += (o, e) =>
        {
            if (o is null || e.PropertyName is null) return;
            Trigger(o, e.PropertyName);
        };
}
