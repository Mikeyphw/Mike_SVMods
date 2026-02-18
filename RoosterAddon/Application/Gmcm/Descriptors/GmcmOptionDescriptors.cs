using System;
using System.Linq.Expressions;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;

namespace RoosterAddon.Application.Gmcm.Descriptors;

/// <summary>
/// Typed option descriptors targeting BOAF's expression-based bindings.
/// Option id is translated via <c>config.&lt;optionId&gt;.(name|description)</c>.
/// </summary>
internal static class GmcmOptionDescriptors
{
    public sealed record Bool<TConfig>(Expression<Func<TConfig, bool>> Member, string OptionId, bool Tooltip = false) : IGmcmElementDescriptor<TConfig>
    {
        public void Apply(GmcmPageBuilder<TConfig> builder)
            => builder.Bool(Member, optionId: OptionId, tooltip: Tooltip);
    }

    public sealed record Int<TConfig>(
        Expression<Func<TConfig, int>> Member,
        string OptionId,
        bool Tooltip = false,
        int? Min = null,
        int? Max = null,
        int? Interval = null) : IGmcmElementDescriptor<TConfig>
    {
        public void Apply(GmcmPageBuilder<TConfig> builder)
            => builder.Int(Member, optionId: OptionId, tooltip: Tooltip, min: Min, max: Max, interval: Interval);
    }

    public sealed record Float<TConfig>(
        Expression<Func<TConfig, float>> Member,
        string OptionId,
        bool Tooltip = false,
        float? Min = null,
        float? Max = null,
        float? Interval = null) : IGmcmElementDescriptor<TConfig>
    {
        public void Apply(GmcmPageBuilder<TConfig> builder)
            => builder.Float(Member, optionId: OptionId, tooltip: Tooltip, min: Min, max: Max, interval: Interval);
    }

    public sealed record EnumOption<TConfig, TEnum>(Expression<Func<TConfig, TEnum>> Member, string OptionId, bool Tooltip = false)
        : IGmcmElementDescriptor<TConfig>
        where TEnum : struct, System.Enum
    {
        public void Apply(GmcmPageBuilder<TConfig> builder)
            => builder.Enum(Member, optionId: OptionId, tooltip: Tooltip);
    }
}
