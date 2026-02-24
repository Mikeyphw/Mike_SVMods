using System;
using StardewModdingAPI;
using Mikew.NestCore.Helpers.Logging;
using LogScopedHelper = Mikew.NestCore.Helpers.Logging.LogScopedHelper;
using static Mikew.NestCore.Helpers.Logging.ScopedLogCatalog;

namespace LetMeHatch.Infrastructure.Cloning;

/// <summary>
/// Default evaluator for content pack strings.
/// Content Patcher token evaluation is intentionally disabled; token strings are returned unchanged.
/// </summary>
public sealed class ContentPackStringEvaluator : IContentStringEvaluator
{
    private readonly ILog _log;

    private bool _warnedTokenEvaluationDisabled;

    public ContentPackStringEvaluator(ILog log)
    {
        _log = log;
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
            LogScopedHelper.Log(_log, ScopedLogCatalog.ModLthteCategory.ContentPipeline,
                "Content Patcher token evaluation is disabled in Let Me Hatch; leaving token strings unchanged.",
                LogLevel.Trace
            );
        }
        return value;
    }
}
