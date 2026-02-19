using System;
using System.Linq;
using System.Reflection;
using ContentPatcher;
using StardewModdingAPI;

namespace LetMeHatchThisEgg.Infrastructure.Cloning;

/// <summary>
/// Default evaluator for content pack strings.
/// Uses reflection for Content Patcher token evaluation to avoid hard-binding to a specific API shape.
/// </summary>
public sealed class ContentPackStringEvaluator : IContentStringEvaluator
{
    private readonly IContentPatcherAPI? _contentPatcher;
    private readonly IMonitor _monitor;

    private MethodInfo? _parseTokenString;
    private bool _warnedMissingTokenParser;
    private bool _warnedTokenParserFailed;

    public ContentPackStringEvaluator(IContentPatcherAPI? contentPatcher, IMonitor monitor)
    {
        _contentPatcher = contentPatcher;
        _monitor = monitor;
        _parseTokenString = FindTokenParser(contentPatcher);
    }

    public string Translate(IContentPack contentPack, string i18nKey)
    {
        return contentPack.Translation.Get(i18nKey);
    }

    public string TranslateAndEvaluateTokens(IContentPack contentPack, string i18nKey)
    {
        var raw = Translate(contentPack, i18nKey);
        return EvaluateTokens(contentPack, raw);
    }

    public string EvaluateTokens(IContentPack contentPack, string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains("{{", StringComparison.Ordinal))
        {
            return value;
        }

        if (_contentPatcher == null || _parseTokenString == null)
        {
            if (!_warnedMissingTokenParser && _contentPatcher != null)
            {
                _warnedMissingTokenParser = true;
                _monitor.Log(
                    "Content Patcher token parser method not found on IContentPatcherAPI; leaving token strings unchanged.",
                    LogLevel.Trace
                );
            }

            return value;
        }

        try
        {
            var parameters = _parseTokenString.GetParameters();
            object?[] args = parameters.Length switch
            {
                2 => BuildTwoArgCall(parameters, contentPack, value),
                1 => BuildOneArgCall(parameters, value),
                _ => new object?[] { value }
            };

            var result = _parseTokenString.Invoke(_contentPatcher, args) as string;
            return result ?? value;
        }
        catch (Exception ex)
        {
            if (!_warnedTokenParserFailed)
            {
                _warnedTokenParserFailed = true;
                _monitor.Log(
                    $"Failed to evaluate Content Patcher tokens; leaving token strings unchanged. {ex.GetType().Name}: {ex.Message}",
                    LogLevel.Trace
                );
            }

            return value;
        }
    }

    private static MethodInfo? FindTokenParser(IContentPatcherAPI? api)
    {
        if (api == null)
        {
            return null;
        }

        var type = api.GetType();
        var candidates = type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m => m.Name is "ParseTokenString" or "ParseTokens")
            .Where(m => m.ReturnType == typeof(string))
            .ToArray();

        foreach (var method in candidates)
        {
            var p = method.GetParameters();
            if (p.Length == 1 && p[0].ParameterType == typeof(string))
            {
                return method;
            }

            if (p.Length == 2)
            {
                bool hasString = p.Any(x => x.ParameterType == typeof(string));
                bool hasPack = p.Any(x => typeof(IContentPack).IsAssignableFrom(x.ParameterType));
                if (hasString && hasPack)
                {
                    return method;
                }
            }
        }

        return null;
    }

    private static object?[] BuildOneArgCall(ParameterInfo[] parameters, string value)
    {
        return parameters[0].ParameterType == typeof(string)
            ? new object?[] { value }
            : new object?[] { value };
    }

    private static object?[] BuildTwoArgCall(ParameterInfo[] parameters, IContentPack contentPack, string value)
    {
        object? first =
            typeof(IContentPack).IsAssignableFrom(parameters[0].ParameterType) ? contentPack : value;
        object? second =
            typeof(IContentPack).IsAssignableFrom(parameters[1].ParameterType) ? contentPack : value;

        return new[] { first, second };
    }
}
