using System;
using StardewModdingAPI;

namespace LetMeHatchThisEgg.Infrastructure.Cloning;

/// <summary>
/// Default evaluator for content pack strings.
/// Content Patcher token evaluation is intentionally disabled; token strings are returned unchanged.
/// </summary>
public sealed class ContentPackStringEvaluator : IContentStringEvaluator
{
    private readonly IMonitor _monitor;

    private bool _warnedTokenEvaluationDisabled;

    public ContentPackStringEvaluator(IMonitor monitor)
    {
        _monitor = monitor;
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

        if (!_warnedTokenEvaluationDisabled)
        {
            _warnedTokenEvaluationDisabled = true;
            _monitor.Log(
                "Content Patcher token evaluation is disabled in Let Me Hatch This Egg; leaving token strings unchanged.",
                LogLevel.Trace
            );
        }
        return value;
    }
}
