using System;
using System.Collections.Generic;
using System.Linq;
using RoosterAddon.Application.Catalogs;
using RoosterAddon.Application.Gmcm.Descriptors;
using RoosterAddon.Framework.Config;
using StardewModdingAPI;

namespace RoosterAddon.Application.Gmcm;

internal static class GmcmDescriptorSanityChecker
{
    public static void Run(IMonitor monitor, ITranslationHelper i18n)
    {
        if (monitor is null) throw new ArgumentNullException(nameof(monitor));
        if (i18n is null) throw new ArgumentNullException(nameof(i18n));

        var (descriptors, pages) = Collect();

        var optionIds = descriptors
            .Where(d => d.Kind == GmcmDescriptorKind.Option)
            .Select(d => d.Id)
            .ToList();

        var dupes = optionIds
            .GroupBy(x => x)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (dupes.Count > 0)
            monitor.Log("GMCM descriptor duplicate option IDs: " + string.Join(", ", dupes), LogLevel.Warn);

        var requiredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var d in descriptors)
        {
            switch (d)
            {
                case GmcmSectionDescriptor<RoosterAddonConfig> s:
                    requiredKeys.Add($"config.section.{s.SectionId}.text");
                    if (s.Tooltip)
                        requiredKeys.Add($"config.section.{s.SectionId}.description");
                    break;

                case GmcmPageLinkDescriptor<RoosterAddonConfig> l:
                    requiredKeys.Add($"config.pagelink.{l.PageId}.name");
                    if (l.Tooltip)
                        requiredKeys.Add($"config.pagelink.{l.PageId}.description");
                    break;

                case GmcmOptionDescriptor<RoosterAddonConfig> o:
                    requiredKeys.Add($"config.{o.OptionId}.name");
                    if (o.Tooltip)
                        requiredKeys.Add($"config.{o.OptionId}.description");
                    break;
            }
        }

        foreach (var p in pages)
            requiredKeys.Add($"config.page.{p}.name");

        var missing = requiredKeys
            .Where(k => !i18n.Get(k).HasValue())
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (missing.Count == 0)
        {
            monitor.Log($"GMCM descriptor sanity check OK ({requiredKeys.Count} translation keys).", LogLevel.Trace);
            return;
        }

        monitor.Log($"GMCM descriptor sanity check: missing {missing.Count} i18n keys (showing up to 50).", LogLevel.Warn);
        foreach (var k in missing.Take(50))
            monitor.Log("  missing: " + k, LogLevel.Warn);
    }

    private static (List<IGmcmDescriptor<RoosterAddonConfig>> descriptors, List<string> pages) Collect()
    {
        var descriptors = new List<IGmcmDescriptor<RoosterAddonConfig>>();

        descriptors.AddRange(RoosterGmcmDescriptorRegistry.RootGeneral());
        descriptors.AddRange(RoosterGmcmDescriptorRegistry.RootRoostersLinks());

        var cfg = RoosterAddonConfig.CreateDefault();
        var catalogs = CatalogDefaults.CreateDefaults();

        var pages = RoosterGmcmDescriptorRegistry.Pages(() => cfg, () => catalogs);
        var pageIds = new List<string>();

        foreach (var p in pages)
        {
            pageIds.Add(p.PageId);
            descriptors.AddRange(p.Content);
        }

        return (descriptors, pageIds);
    }
}
