using System;
using System.Text.RegularExpressions;

namespace LetMeHatchThisEgg.Core.Placeholders;

/// <summary>Pure string placeholder rules and i18n-tag detection (translation is Infrastructure).</summary>
public static class PlaceholderStringPolicy
{
    private static readonly Regex I18nTagPattern = new(@"{{i18n:(.*?)}}", RegexOptions.Compiled);

    /// <summary>
    /// Decide whether to use the original value and (if applicable) extract an <c>{{i18n:key}}</c> translation key.
    /// </summary>
    public static PlaceholderStringDecision Decide(string? current, string? placeholder)
    {
        if (string.Equals(current, placeholder, StringComparison.Ordinal))
        {
            return new PlaceholderStringDecision(UseOriginal: true, I18nKey: null);
        }

        if (string.IsNullOrEmpty(current))
        {
            return new PlaceholderStringDecision(UseOriginal: false, I18nKey: null);
        }

        var match = I18nTagPattern.Match(current);
        if (!match.Success)
        {
            return new PlaceholderStringDecision(UseOriginal: false, I18nKey: null);
        }

        var key = match.Groups[1].Value.Replace(" ", "", StringComparison.Ordinal);
        return new PlaceholderStringDecision(UseOriginal: false, I18nKey: key);
    }

    /// <summary>Return <c>true</c> when token evaluation should occur (i.e. the value isn't a placeholder).</summary>
    public static bool ShouldResolveTokens(string? current, string? placeholder)
    {
        return !string.Equals(current, placeholder, StringComparison.Ordinal) && !string.IsNullOrEmpty(current);
    }
}

/// <summary>Result of evaluating placeholder behavior for a string value.</summary>
public readonly record struct PlaceholderStringDecision(bool UseOriginal, string? I18nKey);
