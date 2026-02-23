using System;
using System.Collections.Generic;
using HarmonyLib;
using Mikew.BirdsOfAFeather.Helpers.Config;
using Mikew.BirdsOfAFeather.Integrations.Gmcm;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using WildEggsHatching.Infrastructure.Api;
using WildEggsHatching.Infrastructure.Config;

namespace WildEggsHatching;

public sealed class ModEntry : Mod
{
    private SmapiConfigProvider<ModConfig>? _config;
    private GmcmConfigHost<ModConfig>? _gmcmHost;
    private WildEggsApi? _api;
    private Harmony? _harmony;

    public override void Entry(IModHelper helper)
    {
        _config = new SmapiConfigProvider<ModConfig>(helper, Monitor);
        _config.ConfigUpdated += (_, __) => SyncApiBirthTextMode();

        EnsureNoLocalBoafDuplicates(helper);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.DayStarted += OnDayStarted;

        InstallHarmonyBootstrap();
    }

    public override object? GetApi()
    {
        if (_config is null)
            return null;

        _api ??= new WildEggsApi(() => _config.Get());
        SyncApiBirthTextMode();
        return _api;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        if (_config is null)
            return;

        _gmcmHost = GmcmConfigHost<ModConfig>.TryCreate(
            helper: Helper,
            manifest: ModManifest,
            configProvider: _config
        );

        RebuildGmcmLayout(force: true);
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        SyncApiBirthTextMode();
    }

    private void RebuildGmcmLayout(bool force)
    {
        if (_gmcmHost is null)
            return;

        _gmcmHost.RebuildLayout(layout =>
        {
            layout.Root(root =>
            {
                root.Bool(cfg => cfg.ProgressionMode, optionId: "general.progressionMode", tooltip: true);
                root.Text(cfg => cfg.BirthText, optionId: "general.birthText", tooltip: true);
                root.Bool(cfg => cfg.DesertTombModuleEnabled, optionId: "desertTomb.enabled", tooltip: true);
                root.Bool(cfg => cfg.HatchDinoEggs, optionId: "hatching.hatchDinoEggs", tooltip: true);
                root.Bool(cfg => cfg.DinoEggsExclusiveHatch, optionId: "hatching.dinoExclusiveHatch", tooltip: true);
            });
        }, force: force);
    }

    private void SyncApiBirthTextMode()
    {
        if (_config is null || _api is null)
            return;

        _api.SetBirthTextMode(_config.Get().BirthText);
    }

    private void InstallHarmonyBootstrap()
    {
        if (_harmony is not null)
            return;

        _harmony = new Harmony($"{Constants.ModId}.harmony");
    }

    private static void EnsureNoLocalBoafDuplicates(IModHelper helper)
    {
        var localBoaf = System.IO.Path.Combine(helper.DirectoryPath, "BirdsOfAFeather.dll");
        if (System.IO.File.Exists(localBoaf))
            throw new InvalidOperationException("Local BirdsOfAFeather.dll detected under WildEggsHatching. Remove it and rely on the installed BOAF mod.");
    }
}
