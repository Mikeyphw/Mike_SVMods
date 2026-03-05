using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Content;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Helpers.Logging;
using Mikew.NestCore.Helpers.SaveData;
using Mikew.NestCore.Integrations.Gmcm;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using EggsInWild.Application.Animals;
using EggsInWild.Application.Facade;
using EggsInWild.Application.Progression;
using EggsInWild.Core.Enums;
using EggsInWild.Core.ShopProgression;
using EggsInWild.Infrastructure.Api;
using EggsInWild.Infrastructure.Content;
using EggsInWild.Infrastructure.Content.Editors;
using EggsInWild.Infrastructure.Commands;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Diagnostics;
using EggsInWild.Infrastructure.Game;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Definitions;
using EggsInWild.Application.DesertTomb.Runtime;
using EggsInWild.Application.DesertTomb.Generation;
using EggsInWild.Application.DesertTomb.Spawning;
using EggsInWild.Infrastructure.DesertTomb.Harmony;
using EggsInWild.Infrastructure.DesertTomb.Loot;
using EggsInWild.Infrastructure.DesertTomb.Loot.Definitions;
using EggsInWild.Infrastructure.DesertTomb.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.ModData;
using EggsInWild.Infrastructure.ModData.Models;
using EggsInWild.Infrastructure.Assets;
using EggsInWild.Infrastructure.Integrations.Gmcm;
using EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;

namespace EggsInWild;

public sealed partial class ModEntry
{

    private void OnGmcmSaved()
    {
        if (_config is null)
            return;

        var cfg = _config.Get();
        if (!NormalizeLocationTypesOverrides(cfg))
            return;

        _config.Save();
        InvalidateGameAssets();
    }

    private bool NormalizeLocationTypesOverrides(ModConfig cfg)
        => WildAnimalsEditorHelpers.NormalizeLocationTypesOverrides(cfg, _knownLocationTypeKeysForNormalization);
    private void RebuildGmcmLayout(bool force)
    {
        if (_gmcmHost is null)
        {
            UpdateDiagnosticsState();
            return;
        }

        var i18n = new I18TranslationHelper(Helper);

        // Create the GMCM context
        var ctx = new EiwGmcmContext(
            Helper,
            Monitor,
            ModManifest,
            () => _config!.Get(),
            () => _config!.Save()
        );
        ctx.OpenWildAnimalsEditor = OpenWildAnimalsEditor;
        ctx.IsWildAnimalsEditorAvailable = _wildAnimalsStardewUiHost is not null && _wildAnimalsLayoutReady;

#if WEGG_DEV
        // Pass debug services to the context
        ctx.DevJsonTuning = _devJsonTuning;
        ctx.DevMonsterDefinitions = DevMonsterDefinitions;
        ctx.MutateDevMonsterDefinitions = MutateDevMonsterDefinitions;
        ctx.NormalizeDevJsonKey = NormalizeDevJsonKey;
#endif

        // Create the menu and rebuild layout
        var menu = new EiwGmcmMenu(ctx);
        _gmcmHost.RebuildLayoutSaveAware(
            define: layout =>
            {
                menu.RebuildLayout(layout, i18n, force);
                _knownLocationTypeKeysForNormalization = ctx.KnownLocationTypeKeys;
            },
            force: force,
            rebuildOnSaveLoaded: true,
            rebuildOnReturnedToTitle: true);

        UpdateDiagnosticsState();
    }

    internal static string Translate(I18TranslationHelper i18n, string key, string fallback, params object[] tokens)
    {
        var translated = tokens.Length == 0
            ? i18n.GetString(key)
            : i18n.GetString(key, tokens);

        if (!string.IsNullOrWhiteSpace(translated) && !string.Equals(translated, key, StringComparison.OrdinalIgnoreCase))
            return translated;

        return tokens.Length == 0 ? fallback : string.Format(fallback, tokens);
    }
}
