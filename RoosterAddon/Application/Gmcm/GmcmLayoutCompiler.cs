using System;
using System.Reflection;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;
using RoosterAddon.Application.Gmcm.Descriptors;
using RoosterAddon.Framework.Config;

namespace RoosterAddon.Application.Gmcm;

internal static class GmcmLayoutCompiler
{
    private static readonly MethodInfo EnumMethod =
        typeof(GmcmPageBuilder<RoosterAddonConfig>)
            .GetMethod(nameof(GmcmPageBuilder<RoosterAddonConfig>.Enum))!;

    public static void Apply(GmcmPageBuilder<RoosterAddonConfig> page, params GmcmSectionDescriptor[] sections)
    {
        foreach (var section in sections)
        {
            page.Section(section.SectionId, tooltip: section.Tooltip);

            foreach (var entry in section.Entries)
            {
                switch (entry)
                {
                    case GmcmPageLinkDescriptor link:
                        page.Link(link.PageId, tooltip: link.Tooltip);
                        break;

                    case GmcmBoolOptionDescriptor b:
                        page.Bool(b.Member, optionId: b.OptionId, tooltip: b.Tooltip);
                        break;

                    case GmcmIntOptionDescriptor i:
                        page.Int(
                            i.Member,
                            optionId: i.OptionId,
                            tooltip: i.Tooltip,
                            min: i.Min,
                            max: i.Max,
                            interval: i.Interval
                        );
                        break;

                    case GmcmFloatOptionDescriptor f:
                        page.Float(
                            f.Member,
                            optionId: f.OptionId,
                            tooltip: f.Tooltip,
                            min: f.Min,
                            max: f.Max,
                            interval: f.Interval
                        );
                        break;

                    
                    case GmcmBoolFuncOptionDescriptor b:
                        page.Bool(b.OptionId, b.GetValue, b.SetValue, tooltip: b.Tooltip);
                        break;

                    case GmcmIntFuncOptionDescriptor i:
                        page.Int(
                            i.OptionId,
                            i.GetValue,
                            i.SetValue,
                            tooltip: i.Tooltip,
                            min: i.Min,
                            max: i.Max,
                            interval: i.Interval
                        );
                        break;

                    case GmcmFloatFuncOptionDescriptor f:
                        page.Float(
                            f.OptionId,
                            f.GetValue,
                            f.SetValue,
                            tooltip: f.Tooltip,
                            min: f.Min,
                            max: f.Max,
                            interval: f.Interval
                        );
                        break;

                    case GmcmTextFuncOptionDescriptor t:
                        page.Text(
                            t.OptionId,
                            t.GetValue,
                            t.SetValue,
                            tooltip: t.Tooltip,
                            allowedValues: t.AllowedValues,
                            formatAllowedValue: t.FormatAllowedValue
                        );
                        break;

case IGmcmEnumOptionDescriptor e:
                        AddEnum(page, e);
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported GMCM descriptor entry: {entry.GetType().FullName}");
                }
            }
        }
    }

    private static void AddEnum(GmcmPageBuilder<RoosterAddonConfig> page, IGmcmEnumOptionDescriptor e)
    {
        var m = EnumMethod.MakeGenericMethod(e.EnumType);
        _ = m.Invoke(page, new object?[] { e.Member, e.OptionId, e.Tooltip });
    }
}
