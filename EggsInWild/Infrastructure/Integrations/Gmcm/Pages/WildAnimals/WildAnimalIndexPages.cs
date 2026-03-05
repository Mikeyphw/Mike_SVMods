using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Integrations.Gmcm;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.ModData.Models;

namespace EggsInWild.Infrastructure.Integrations.Gmcm.Pages.WildAnimals;

/// <summary>
/// GMCM pages for Wild Animals index and bucket pages.
/// </summary>
public static class WildAnimalIndexPages
{
    /// <summary>
    /// Define all Wild Animals index pages (main index + bucket pages).
    /// </summary>
    public static void Define(
        EiwGmcmContext ctx,
        GmcmMenuLayout<ModConfig> layout,
        I18TranslationHelper i18n,
        Dictionary<string, WildAnimalJson> wildAnimalsForGmcm,
        List<string> wildAnimalKeysForGmcm)
    {
        // Main WildAnimals index page
        DefineWildAnimalsIndexPage(ctx, layout, i18n, wildAnimalsForGmcm, wildAnimalKeysForGmcm);

        // Bucket index pages
        DefineWildAnimalBucketPages(ctx, layout, i18n, wildAnimalsForGmcm, wildAnimalKeysForGmcm);
    }

    private static void DefineWildAnimalsIndexPage(
        EiwGmcmContext ctx,
        GmcmMenuLayout<ModConfig> layout,
        I18TranslationHelper i18n,
        Dictionary<string, WildAnimalJson> wildAnimalsForGmcm,
        List<string> wildAnimalKeysForGmcm)
    {
        // Calculate bucket counts
        var wildAnimalsCountAll = wildAnimalKeysForGmcm.Count;
        var wildAnimalsCountAf = CountWildAnimalKeysBucket(wildAnimalKeysForGmcm, 'A', 'F');
        var wildAnimalsCountGl = CountWildAnimalKeysBucket(wildAnimalKeysForGmcm, 'G', 'L');
        var wildAnimalsCountMr = CountWildAnimalKeysBucket(wildAnimalKeysForGmcm, 'M', 'R');
        var wildAnimalsCountSz = CountWildAnimalKeysBucket(wildAnimalKeysForGmcm, 'S', 'Z');
        var wildAnimalsCountOther = CountWildAnimalKeysOther(wildAnimalKeysForGmcm);

        layout.Page("wildAnimals", page =>
        {
            page.ParagraphDynamic(
                () => ModEntry.Translate(i18n, "config.paragraph.wildAnimals.description.text", "Enable/disable wild animals (affects egg assignment and Animal Notes progression)."),
                id: "wildAnimals_description");

            page.ParagraphDynamic(
                () =>
                {
                    var cfg = ctx.GetConfig();
                    var enabled = 0;
                    foreach (var key in wildAnimalKeysForGmcm)
                    {
                        if (!wildAnimalsForGmcm.TryGetValue(key, out var def) || def is null)
                            continue;

                        var defaultEnabled = def.Enabled;
                        var effective = ModEntry.GetWildAnimalEffectiveEnabled(cfg, key, defaultEnabled);
                        if (effective)
                            enabled++;
                    }

                    return ModEntry.Translate(i18n, "config.paragraph.wildAnimals.summary.text", $"Enabled: {enabled}/{wildAnimalKeysForGmcm.Count}", enabled, wildAnimalKeysForGmcm.Count);
                },
                id: "wildAnimals_summary");

            if (wildAnimalKeysForGmcm.Count == 0)
            {
                page.ParagraphDynamic(
                    () => ModEntry.Translate(i18n, "config.paragraph.wildAnimals.empty.text", "No wild animals were found in the WildAnimalsList."),
                    id: "wildAnimals_empty");
                return;
            }

            page.Separator(simple: true);

            page.ParagraphDynamic(
                () => ModEntry.Translate(i18n, "config.paragraph.wildAnimals.indexGroups.note", "Pick a letter group to navigate. (GMCM pages are static; no live search.)"),
                id: "wildAnimals_indexGroups_note");

            page.LinkLabel(
                pageId: "wildAnimals_all",
                text: () => $"{ModEntry.Translate(i18n, "config.wildAnimals.index.group.all.name", "All (A–Z)")} ({wildAnimalsCountAll})",
                tooltip: () => ModEntry.Translate(i18n, "config.wildAnimals.index.group.all.description", "Show all wild animals (long list)."));

            page.LinkLabel(
                pageId: "wildAnimals_index_af",
                text: () => $"{ModEntry.Translate(i18n, "config.wildAnimals.index.group.af.name", "A–F")} ({wildAnimalsCountAf})",
                tooltip: () => ModEntry.Translate(i18n, "config.wildAnimals.index.group.af.description", "Wild animals starting with A–F."));

            page.LinkLabel(
                pageId: "wildAnimals_index_gl",
                text: () => $"{ModEntry.Translate(i18n, "config.wildAnimals.index.group.gl.name", "G–L")} ({wildAnimalsCountGl})",
                tooltip: () => ModEntry.Translate(i18n, "config.wildAnimals.index.group.gl.description", "Wild animals starting with G–L."));

            page.LinkLabel(
                pageId: "wildAnimals_index_mr",
                text: () => $"{ModEntry.Translate(i18n, "config.wildAnimals.index.group.mr.name", "M–R")} ({wildAnimalsCountMr})",
                tooltip: () => ModEntry.Translate(i18n, "config.wildAnimals.index.group.mr.description", "Wild animals starting with M–R."));

            page.LinkLabel(
                pageId: "wildAnimals_index_sz",
                text: () => $"{ModEntry.Translate(i18n, "config.wildAnimals.index.group.sz.name", "S–Z")} ({wildAnimalsCountSz})",
                tooltip: () => ModEntry.Translate(i18n, "config.wildAnimals.index.group.sz.description", "Wild animals starting with S–Z."));

            page.LinkLabel(
                pageId: "wildAnimals_index_other",
                text: () => $"{ModEntry.Translate(i18n, "config.wildAnimals.index.group.other.name", "Other")} ({wildAnimalsCountOther})",
                tooltip: () => ModEntry.Translate(i18n, "config.wildAnimals.index.group.other.description", "Wild animals not starting with A–Z."));
        });
    }

    private static void DefineWildAnimalBucketPages(
        EiwGmcmContext ctx,
        GmcmMenuLayout<ModConfig> layout,
        I18TranslationHelper i18n,
        Dictionary<string, WildAnimalJson> wildAnimalsForGmcm,
        List<string> wildAnimalKeysForGmcm)
    {
        static string WildAnimalIndexBucket(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return "other";

            var c = char.ToUpperInvariant(key.Trim()[0]);
            if (c is >= 'A' and <= 'F') return "af";
            if (c is >= 'G' and <= 'L') return "gl";
            if (c is >= 'M' and <= 'R') return "mr";
            if (c is >= 'S' and <= 'Z') return "sz";
            return "other";
        }

        var wildAnimalBuckets = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["af"] = new List<string>(),
            ["gl"] = new List<string>(),
            ["mr"] = new List<string>(),
            ["sz"] = new List<string>(),
            ["other"] = new List<string>(),
        };

        foreach (var k in wildAnimalKeysForGmcm)
            wildAnimalBuckets[WildAnimalIndexBucket(k)].Add(k);

        void BuildWildAnimalIndexPage(string pageId, Func<string> title, IEnumerable<string> keys)
        {
            layout.Page(pageId, page =>
            {
                page.LinkLabel(
                    pageId: "wildAnimals",
                    text: () => ModEntry.Translate(i18n, "config.wildAnimals.index.back.name", "Back to WildAnimals index"),
                    tooltip: () => ModEntry.Translate(i18n, "config.wildAnimals.index.back.description", "Return to the WildAnimals index page."));

                page.Separator(simple: true);
                page.SectionLabel(title);

                foreach (var key in keys)
                {
                    var waPageId = ModEntry.BuildWildAnimalPageId(key);
                    page.LinkLabel(
                        pageId: waPageId,
                        text: () =>
                        {
                            var cfg = ctx.GetConfig();
                            var enabled = wildAnimalsForGmcm.TryGetValue(key, out var def) && def is not null
                                ? ModEntry.GetWildAnimalEffectiveEnabled(cfg, key, def.Enabled)
                                : true;

                            var label = enabled ? key : $"{key} (disabled)";

                            if (ctx.ExtraModAnimalKeysBulkByWildAnimalKey.TryGetValue(key, out var st) && !string.IsNullOrWhiteSpace(st.Raw))
                            {
                                var warnCount = st.Warnings.Count;
                                label += warnCount > 0 ? $" [staged, warn:{warnCount}]" : " [staged]";
                            }

                            return label;
                        },
                        tooltip: () => ModEntry.Translate(i18n, "config.wildAnimals.link.tooltip", "Edit enabled override for '{0}'.", key, key));
                }
            });
        }

        BuildWildAnimalIndexPage("wildAnimals_all", () => ModEntry.Translate(i18n, "config.wildAnimals.index.group.all.name", "All (A–Z)"), wildAnimalKeysForGmcm);
        BuildWildAnimalIndexPage("wildAnimals_index_af", () => ModEntry.Translate(i18n, "config.wildAnimals.index.group.af.name", "A–F"), wildAnimalBuckets["af"]);
        BuildWildAnimalIndexPage("wildAnimals_index_gl", () => ModEntry.Translate(i18n, "config.wildAnimals.index.group.gl.name", "G–L"), wildAnimalBuckets["gl"]);
        BuildWildAnimalIndexPage("wildAnimals_index_mr", () => ModEntry.Translate(i18n, "config.wildAnimals.index.group.mr.name", "M–R"), wildAnimalBuckets["mr"]);
        BuildWildAnimalIndexPage("wildAnimals_index_sz", () => ModEntry.Translate(i18n, "config.wildAnimals.index.group.sz.name", "S–Z"), wildAnimalBuckets["sz"]);
        BuildWildAnimalIndexPage("wildAnimals_index_other", () => ModEntry.Translate(i18n, "config.wildAnimals.index.group.other.name", "Other"), wildAnimalBuckets["other"]);
    }

    private static int CountWildAnimalKeysBucket(IReadOnlyList<string> keys, char startInclusive, char endInclusive)
    {
        var count = 0;
        foreach (var k in keys)
        {
            if (string.IsNullOrWhiteSpace(k))
                continue;

            var c = char.ToUpperInvariant(k.Trim()[0]);
            if (c >= startInclusive && c <= endInclusive)
                count++;
        }

        return count;
    }

    private static int CountWildAnimalKeysOther(IReadOnlyList<string> keys)
    {
        var count = 0;
        foreach (var k in keys)
        {
            if (string.IsNullOrWhiteSpace(k))
                continue;

            var c = char.ToUpperInvariant(k.Trim()[0]);
            if (c < 'A' || c > 'Z')
                count++;
        }

        return count;
    }
}

