using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Integrations.Gmcm;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Integrations.Gmcm;
using EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;

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
        var catalog = ctx.WildAnimalsEditor.LoadCatalog();
        var wildAnimalsForGmcm = catalog.Definitions.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
        var wildAnimalKeysForGmcm = catalog.Animals.Select(animal => animal.Key).ToList();
        var locationTypeKeysForGmcm = catalog.LocationTypeKeys.ToList();

        ctx.KnownLocationTypeKeys = new HashSet<string>(locationTypeKeysForGmcm, StringComparer.OrdinalIgnoreCase);

        // Define index pages (main index + bucket pages)
        WildAnimalIndexPages.Define(ctx, layout, i18n, wildAnimalsForGmcm, wildAnimalKeysForGmcm);

        // Define detail pages (per-animal configuration)
        WildAnimalDetailPages.Define(ctx, layout, i18n, wildAnimalsForGmcm, wildAnimalKeysForGmcm, locationTypeKeysForGmcm);
    }
}
