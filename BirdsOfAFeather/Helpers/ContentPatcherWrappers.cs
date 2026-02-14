using System;
using System.Reflection;

namespace Mikew.BirdsOfAFeather.Helpers;

/// <summary>
/// Reflection wrappers for Content Patcher managed objects.
/// </summary>
public sealed class ManagedConditions
{
    private readonly object _inner;
    private readonly MethodInfo _updateContext;
    private readonly PropertyInfo _isMatch;

    public ManagedConditions(object inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        var t = inner.GetType();

        _updateContext = t.GetMethod("UpdateContext", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Content Patcher conditions type '{t.FullName}' does not expose UpdateContext().");

        _isMatch = t.GetProperty("IsMatch", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Content Patcher conditions type '{t.FullName}' does not expose IsMatch.");
    }

    public void UpdateContext()
    {
        _updateContext.Invoke(_inner, null);
    }

    public bool IsMatch => (bool)(_isMatch.GetValue(_inner) ?? false);
}

public sealed class ManagedTokenString
{
    private readonly object _inner;
    private readonly MethodInfo _updateContext;
    private readonly PropertyInfo _value;

    public ManagedTokenString(object inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        var t = inner.GetType();

        _updateContext = t.GetMethod("UpdateContext", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Content Patcher token string type '{t.FullName}' does not expose UpdateContext().");

        _value = t.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Content Patcher token string type '{t.FullName}' does not expose Value.");
    }

    public void UpdateContext()
    {
        _updateContext.Invoke(_inner, null);
    }

    public string? Value => _value.GetValue(_inner) as string;
}
