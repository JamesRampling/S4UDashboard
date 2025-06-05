using System;
using System.Collections.Generic;

namespace S4UDashboard;

/// <summary>A simple typed dependency injection class.</summary>
public static class ServiceProvider
{
    /// <summary>The static list of all registered services, associated by type.</summary>
    private readonly static Dictionary<Type, object> Services = [];

    /// <summary>Adds a service to the provider.</summary>
    /// <param name="service">The service to register.</param>
    public static void AddService<T>(T service) where T : class
    {
        if (Services.ContainsKey(typeof(T)))
            throw new Exception("service of this type was already provided");

        Services.Add(typeof(T), service);
    }

    /// <summary>Gets a service from the provider, or null if it is not present.</summary>
    public static T? GetService<T>() where T : class =>
        !Services.TryGetValue(typeof(T), out var result) ? null : (T)result;

    /// <summary>Gets a service from the provider, or throws if it is not present.</summary>
    public static T ExpectService<T>() where T : class =>
        !Services.TryGetValue(typeof(T), out var result)
            ? throw new Exception($"Expected to have {typeof(T).Name} service available")
            : (T)result;
}
