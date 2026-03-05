using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore.Integrations.Gmcm;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Diagnostics;
using EggsInWild.Infrastructure.Integrations.Gmcm;

namespace EggsInWild.Infrastructure.Integrations.Gmcm.Pages.WildAnimals;

/// <summary>
/// GMCM pages for Wild Animals configuration.
/// </summary>
public static class WildAnimalsPages
{
    /// <summary>
    /// Define all Wild Animals GMCM pages.
    /// </summary>
    public static void Define(EiwGmcmContext ctx, GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        // Load wild animals data
        Dictionary<string, Infrastructure.ModData.Models.WildAnimalJson> wildAnimalsForGmcm;
        try
        {
            var reader = new Infrastructure.ModData.SmapiJsonFileReader(ctx.Helper);
            wildAnimalsForGmcm =
                reader.ReadJsonFile<Dictionary<string, Infrastructure.ModData.Models.WildAnimalJson>>(Infrastructure.ModData.ModDataPaths.WildAnimalsList)
                ?? new Dictionary<string, Infrastructure.ModData.Models.WildAnimalJson>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            var smapiLog = new SmapiLog(ctx.Monitor);
            smapiLog.LogScoped(ScopedLogCatalog.ModWeCategory.Content, $"Failed to load {Infrastructure.ModData.ModDataPaths.WildAnimalsList} for GMCM: {ex}", LogLevel.Error);
            wildAnimalsForGmcm = new Dictionary<string, Infrastructure.ModData.Models.WildAnimalJson>(StringComparer.OrdinalIgnoreCase);
        }

        var wildAnimalKeysForGmcm = wildAnimalsForGmcm.Keys
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var locationTypeKeysForGmcm = wildAnimalsForGmcm.Values
            .SelectMany(v => v.LocationTypes?.Keys ?? Enumerable.Empty<string>())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => string.Equals(k, "Farm", StringComparison.OrdinalIgnoreCase) ? "" : k, StringComparer.OrdinalIgnoreCase)
            .ToList();

        ctx.KnownLocationTypeKeys = new HashSet<string>(locationTypeKeysForGmcm, StringComparer.OrdinalIgnoreCase);

        // Define index pages (main index + bucket pages)
        WildAnimalIndexPages.Define(ctx, layout, i18n, wildAnimalsForGmcm, wildAnimalKeysForGmcm);

        // Define detail pages (per-animal configuration)
        WildAnimalDetailPages.Define(ctx, layout, i18n, wildAnimalsForGmcm, wildAnimalKeysForGmcm, locationTypeKeysForGmcm);
    }
}

