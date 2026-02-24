using StardewModdingAPI;

namespace LetMeHatch.Infrastructure.Cloning;

/// <summary>Evaluates content-pack-provided strings (i18n and optional Content Patcher tokens).</summary>
public interface IContentStringEvaluator
{
    /// <summary>Get the translated string for the given i18n key.</summary>
    string Translate(IContentPack contentPack, string i18nKey);

    /// <summary>
    /// Evaluate Content Patcher tokens in <paramref name="value"/> if possible; otherwise return the input.
    /// </summary>
    string EvaluateTokens(IContentPack contentPack, string value);

    /// <summary>Translate the i18n key and then evaluate tokens in the result (if any).</summary>
    string TranslateAndEvaluateTokens(IContentPack contentPack, string i18nKey);
}
