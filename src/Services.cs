using System;
using System.Collections.Generic;

namespace S4UDashboard;

public static class ServiceProvider
{
    private readonly static Dictionary<Type, object> Services = [];

    public static void AddService<T>(T service) where T : class
    {
        if (Services.ContainsKey(typeof(T)))
            throw new Exception("service of this type was already provided");

        Services.Add(typeof(T), service);
    }

    public static T? GetService<T>() where T : class =>
        !Services.TryGetValue(typeof(T), out var result) ? null : (T)result;

    public static T ExpectService<T>() where T : class =>
        !Services.TryGetValue(typeof(T), out var result)
            ? throw new Exception($"Expected to have {typeof(T).Name} service available")
            : (T)result;
}
