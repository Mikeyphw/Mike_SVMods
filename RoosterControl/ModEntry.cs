using System;
using System.Collections.Generic;
using RoosterControl.Application;
using RoosterControl.Framework;
using RoosterControl.Framework.Config;
using RoosterControl.Helpers;
using RoosterControl.Infrastructure.SaveData;
using RoosterControl.Infrastructure.Smapi;
using StardewModdingAPI;

namespace RoosterControl;

/// <summary>The mod entry point.</summary>
internal sealed partial class ModEntry : Mod
{
    private SmapiEventRouter? _eventRouter;
    private RoosterCoordinator? _coordinator;

    /// <summary>Typed persisted config (Phase 3+).</summary>
    internal RoosterControlConfig Config
    {
        get => EnsureCoordinator().Config;
        set => EnsureCoordinator().Config = value;
    }

    /// <summary>Coordinator-owned runtime helper (legacy services still depend on it).</summary>
    internal RoosterHelper RoosterHelper => EnsureCoordinator().RoosterHelper;

    public override object GetApi() => RoosterHatchingApi.Instance;

    public override void Entry(IModHelper helper)
    {
        ValidateDependencies(helper);

        var saveData = new SmapiSaveDataStore(helper);
        _coordinator = new RoosterCoordinator(this, saveData);
        _coordinator.Initialize();

        _eventRouter = new SmapiEventRouter(helper, _coordinator);
        _eventRouter.Hook();
    }


    private void ValidateDependencies(IModHelper helper)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        var nestCore = helper.ModRegistry.Get("Mikew.nestcore");
        if (nestCore is null)
        {
            errors.Add("Missing required dependency: Mikew.nestcore (NestCore). Install it and ensure SMAPI can load it.");
        }
        else
        {
            var required = new SemanticVersion(1, 0, 1);
            if (nestCore.Manifest.Version.IsOlderThan(required))
                errors.Add($"NestCore is out of date: found {nestCore.Manifest.Version}, required >= {required}.");
        }

        if (!helper.ModRegistry.IsLoaded("Meara.Roosters"))
            warnings.Add("Optional integration not installed: Meara.Roosters. Rooster providers from that mod will be unavailable.");

        foreach (var w in warnings)
            Monitor.Log(w, LogLevel.Warn);

        if (errors.Count == 0)
            return;

        Monitor.Log("RoosterControl cannot start due to dependency issues:", LogLevel.Error);
        foreach (var e in errors)
            Monitor.Log($"- {e}", LogLevel.Error);

        throw new InvalidOperationException("Missing required dependencies. See log for details.");
    }


    private RoosterCoordinator EnsureCoordinator()
        => _coordinator ?? throw new InvalidOperationException("Coordinator not initialized yet.");

    public bool HasModCheck(string modId) => Helper.ModRegistry.IsLoaded(modId);

    public static bool HasMod(string modId, IModHelper helper)
    {
        if (modId.Contains("&&", StringComparison.Ordinal))
        {
            foreach (var part in modId.Split("&&", StringSplitOptions.TrimEntries))
            {
                if (!helper.ModRegistry.IsLoaded(part))
                    return false;
            }
            return true;
        }

        if (modId.Contains("||", StringComparison.Ordinal))
        {
            foreach (var part in modId.Split("||", StringSplitOptions.TrimEntries))
            {
                if (helper.ModRegistry.IsLoaded(part))
                    return true;
            }
            return false;
        }

        return helper.ModRegistry.IsLoaded(modId);
    }
}
