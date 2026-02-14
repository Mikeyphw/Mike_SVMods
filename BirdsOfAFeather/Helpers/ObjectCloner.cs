using System;
using System.Reflection;
using Newtonsoft.Json;

namespace Mikew.BirdsOfAFeather.Helpers;

/// <summary>
/// Provides deep and shallow clone helpers.
/// </summary>
public static class ObjectCloner
{
    /// <summary>
    /// Clone an object either deeply (via JSON roundtrip) or shallowly (via <c>MemberwiseClone</c>).
    /// </summary>
    public static T? Clone<T>(T? obj, CloneType cloneType)
    {
        return cloneType switch
        {
            CloneType.Deep => DeepCopy(obj),
            CloneType.Shallow => ShallowCopy(obj),
            _ => throw new ArgumentOutOfRangeException(nameof(cloneType), cloneType, "Invalid CloneType"),
        };
    }

    /// <summary>
    /// Deep clone using JSON serialization (slow, but safe for DTO-like types).
    /// </summary>
    private static T? DeepCopy<T>(T? obj)
    {
        if (obj is null)
        {
            return default;
        }

        var json = JsonConvert.SerializeObject(obj);
        return JsonConvert.DeserializeObject<T>(json);
    }

    /// <summary>
    /// Shallow clone using <c>MemberwiseClone</c>.
    /// For value types and strings, the original value is returned.
    /// </summary>
    private static T? ShallowCopy<T>(T? obj)
    {
        if (obj is null)
        {
            return default;
        }

        var type = typeof(T);

        if (type.IsValueType || obj is string)
        {
            return obj;
        }

        var memberwiseClone = obj.GetType().GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
        if (memberwiseClone is null)
        {
            throw new InvalidOperationException($"Cannot shallow clone type '{obj.GetType().FullName}' (MemberwiseClone not found).");
        }

        return (T)memberwiseClone.Invoke(obj, null)!;
    }

    public enum CloneType
    {
        Deep,
        Shallow,
    }
}
