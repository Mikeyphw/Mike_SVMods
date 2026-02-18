using System;
using System.Linq.Expressions;
using RoosterAddon.Framework.Config;

namespace RoosterAddon.Application.Gmcm.Descriptors;

internal interface IGmcmEntryDescriptor;

internal sealed record GmcmPageLinkDescriptor(string PageId, bool Tooltip = false) : IGmcmEntryDescriptor;

internal abstract record GmcmOptionDescriptor(string OptionId, bool Tooltip) : IGmcmEntryDescriptor;

internal sealed record GmcmBoolOptionDescriptor(
    string OptionId,
    Expression<Func<RoosterAddonConfig, bool>> Member,
    bool Tooltip = false) : GmcmOptionDescriptor(OptionId, Tooltip);

internal sealed record GmcmIntOptionDescriptor(
    string OptionId,
    Expression<Func<RoosterAddonConfig, int>> Member,
    int? Min = null,
    int? Max = null,
    int? Interval = null,
    bool Tooltip = false) : GmcmOptionDescriptor(OptionId, Tooltip);

internal sealed record GmcmFloatOptionDescriptor(
    string OptionId,
    Expression<Func<RoosterAddonConfig, float>> Member,
    float? Min = null,
    float? Max = null,
    float? Interval = null,
    bool Tooltip = false) : GmcmOptionDescriptor(OptionId, Tooltip);


internal sealed record GmcmBoolFuncOptionDescriptor(
    string OptionId,
    Func<bool> GetValue,
    Action<bool> SetValue,
    bool Tooltip = false) : GmcmOptionDescriptor(OptionId, Tooltip);

internal sealed record GmcmIntFuncOptionDescriptor(
    string OptionId,
    Func<int> GetValue,
    Action<int> SetValue,
    int? Min = null,
    int? Max = null,
    int? Interval = null,
    bool Tooltip = false) : GmcmOptionDescriptor(OptionId, Tooltip);

internal sealed record GmcmFloatFuncOptionDescriptor(
    string OptionId,
    Func<float> GetValue,
    Action<float> SetValue,
    float? Min = null,
    float? Max = null,
    float? Interval = null,
    bool Tooltip = false) : GmcmOptionDescriptor(OptionId, Tooltip);

internal sealed record GmcmTextFuncOptionDescriptor(
    string OptionId,
    Func<string> GetValue,
    Action<string> SetValue,
    string[]? AllowedValues = null,
    bool FormatAllowedValue = false,
    bool Tooltip = false) : GmcmOptionDescriptor(OptionId, Tooltip);

internal interface IGmcmEnumOptionDescriptor : IGmcmEntryDescriptor
{
    string OptionId { get; }
    bool Tooltip { get; }
    Type EnumType { get; }
    LambdaExpression Member { get; }
}

internal sealed record GmcmEnumOptionDescriptor<TEnum>(
    string OptionId,
    Expression<Func<RoosterAddonConfig, TEnum>> Member,
    bool Tooltip = false)
    : GmcmOptionDescriptor(OptionId, Tooltip), IGmcmEnumOptionDescriptor
    where TEnum : struct, Enum
{
    Type IGmcmEnumOptionDescriptor.EnumType => typeof(TEnum);
    LambdaExpression IGmcmEnumOptionDescriptor.Member => Member;
}
