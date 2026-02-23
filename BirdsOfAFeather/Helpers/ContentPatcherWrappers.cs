using System;
using System.Collections.Generic;
using System.Reflection;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

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

public static class ContentPatcherReflection
{
    public static bool TryParseConditions(
        object? api,
        IManifest manifest,
        IDictionary<string, string?> rawConditions,
        ISemanticVersion formatVersion,
        out object? parsedConditions
    )
    {
        parsedConditions = null;
        if (api is null)
            return false;

        var methods = api.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
        foreach (var method in methods)
        {
            if (!string.Equals(method.Name, "ParseConditions", StringComparison.Ordinal))
                continue;

            var parameters = method.GetParameters();
            if (parameters.Length is < 3 or > 4)
                continue;

            if (!TryBuildParseConditionsArgs(parameters, manifest, rawConditions, formatVersion, out var args))
                continue;

            parsedConditions = method.Invoke(api, args);
            return parsedConditions is not null;
        }

        return false;
    }

    private static bool TryBuildParseConditionsArgs(
        ParameterInfo[] parameters,
        IManifest manifest,
        IDictionary<string, string?> rawConditions,
        ISemanticVersion formatVersion,
        out object?[] args
    )
    {
        args = new object?[parameters.Length];
        var usedManifest = false;
        var usedConditions = false;
        var usedFormat = false;

        for (var i = 0; i < parameters.Length; i++)
        {
            var type = parameters[i].ParameterType;

            if (!usedManifest && type.IsAssignableFrom(manifest.GetType()))
            {
                args[i] = manifest;
                usedManifest = true;
                continue;
            }

            if (!usedFormat && type.IsAssignableFrom(formatVersion.GetType()))
            {
                args[i] = formatVersion;
                usedFormat = true;
                continue;
            }

            if (!usedConditions && IsStringDictionaryType(type))
            {
                args[i] = rawConditions;
                usedConditions = true;
                continue;
            }

            if (type == typeof(string[]))
            {
                args[i] = Array.Empty<string>();
                continue;
            }

            if (type == typeof(object))
            {
                args[i] = rawConditions;
                usedConditions = true;
                continue;
            }

            return false;
        }

        return usedManifest && usedConditions && usedFormat;
    }

    private static bool IsStringDictionaryType(Type type)
    {
        if (!type.IsGenericType)
            return false;

        var genericType = type.GetGenericTypeDefinition();
        if (genericType != typeof(IDictionary<,>) && genericType != typeof(IReadOnlyDictionary<,>))
            return false;

        var args = type.GetGenericArguments();
        return args.Length == 2 && args[0] == typeof(string) && args[1] == typeof(string);
    }
}
