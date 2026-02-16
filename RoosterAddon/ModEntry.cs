using System;
using System.Collections.Generic;
using RoosterAddon.Constants;
using RoosterAddon.Enums;
using RoosterAddon.Framework;
using RoosterAddon.Helpers;
using Mikew.BirdsOfAFeather.Framework;
using ILetMeHatchApi = Mikew.BirdsOfAFeather.Framework.ILetMeHatchApi;
using RoosterAddon.Application;
using RoosterAddon.Infrastructure.Smapi;
using RoosterAddon.Infrastructure.SaveData;
using StardewModdingAPI;

namespace RoosterAddon;

/// <summary>The mod entry point.</summary>
internal sealed partial class ModEntry : Mod
{
    private SmapiEventRouter? _eventRouter;
    private RoosterCoordinator? _coordinator;

    /// <summary>Content Patcher API (set at game launch).</summary>
    internal static IContentPatcherAPI? ContentPatcher;

    /// <summary>Exposes coordinator-owned config for legacy partials.</summary>
    internal ModConfig ModCfg
    {
        get => EnsureCoordinator().ModCfg;
        set => EnsureCoordinator().ModCfg = value;
    }

    /// <summary>Exposes coordinator-owned rooster helper for legacy partials.</summary>
    internal RoosterControl RoosterHelper => EnsureCoordinator().RoosterHelper;

    /// <summary>Exposes coordinator-owned texture map for legacy partials.</summary>
    internal Dictionary<Textures, Dictionary<TextureDetails, string>> ModTextures => EnsureCoordinator().ModTextures;

    /// <summary>GMCM API (set at game launch).</summary>
    internal IGenericModConfigMenuAPI? configMenu
    {
        get => EnsureCoordinator().ConfigMenu;
        set => EnsureCoordinator().ConfigMenu = value;
    }

    /// <summary>LetMeHatch API (set at game launch).</summary>
    internal ILetMeHatchApi? hatchApi
    {
        get => EnsureCoordinator().HatchApi;
        set => EnsureCoordinator().HatchApi = value;
    }

    public override object GetApi() => RoosterHatchingApi.Instance;

    public override void Entry(IModHelper helper)
    {
        var saveData = new SmapiSaveDataStore(helper);
        _coordinator = new RoosterCoordinator(this, saveData);
        _coordinator.Initialize();

        _eventRouter = new SmapiEventRouter(helper, _coordinator);
        _eventRouter.Hook();
    }

    private RoosterCoordinator EnsureCoordinator()
    {
        if (_coordinator is null)
            throw new InvalidOperationException("Coordinator not initialized yet.");
        return _coordinator;
    }

    private bool GetHatchApiCloneEggsOption() => hatchApi is not null && hatchApi.GetClonedAnimalsInfo().Count > 0;

    private bool GetHatchOnlyByFertilizedOption() =>
        ModCfg.Options[OptionEntries.HatchingMode][OptionDetails.Value] != OptionValues.Original;

    public bool HasModCheck(string modID) => Helper.ModRegistry.IsLoaded(modID);

    public static bool HasMod(string modID, IModHelper helper)
    {
        if (modID.Contains("&&", StringComparison.Ordinal))
        {
            string[] mods = modID.Split("&&", StringSplitOptions.TrimEntries);
            foreach (var mod in mods)
            {
                if (!helper.ModRegistry.IsLoaded(mod))
                    return false;
            }
            return true;
        }

        if (modID.Contains("||", StringComparison.Ordinal))
        {
            string[] mods = modID.Split("||", StringSplitOptions.TrimEntries);
            foreach (var mod in mods)
            {
                if (helper.ModRegistry.IsLoaded(mod))
                    return true;
            }
            return false;
        }

        return helper.ModRegistry.IsLoaded(modID);
    }
}