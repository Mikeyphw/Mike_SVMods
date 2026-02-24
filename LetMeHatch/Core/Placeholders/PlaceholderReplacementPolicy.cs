using System.Collections.Generic;

namespace LetMeHatch.Core.Placeholders;

/// <summary>Pure placeholder replacement rules (no SMAPI, no Content Patcher, no i18n evaluation).</summary>
public static class PlaceholderReplacementPolicy
{
    /// <summary>Return <c>true</c> if <paramref name="current"/> equals the placeholder sentinel.</summary>
    public static bool IsPlaceholder<T>(T current, T placeholder)
    {
        return EqualityComparer<T>.Default.Equals(current, placeholder);
    }

    /// <summary>Replace placeholder values with the original value; otherwise keep the current value.</summary>
    public static T ReplaceIfPlaceholder<T>(T current, T original, T placeholder)
    {
        return IsPlaceholder(current, placeholder) ? original : current;
    }
}
