using System;
using System.Collections.Generic;
using System.Linq;
using Mikew.BirdsOfAFeather.Helpers;
using Mikew.BirdsOfAFeather.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace WildEggsHatching.Infrastructure.Assets;

/// <summary>
/// BOAF-backed asset loader. Only intercepts asset keys explicitly registered for this mod.
/// </summary>
public sealed class BoafAssetRegistry : IDisposable
{
    private readonly IModHelper _helper;
    private readonly ModAssetHandler _handler;
    private readonly HashSet<string> _gamePaths;
    private bool _registered;

    public BoafAssetRegistry(IModHelper helper, IMonitor monitor, string modId)
    {
        _helper = helper;

        _handler = new ModAssetHandler(modId, monitor);
        _handler.AddAssets(new List<AssetData>
        {
            // Mods/<modId>/<asset>
            new(typeof(WeggObjectTexture), WeggAssetFolder.objects, ModAssetHandler.FileExtension.Png, $"Mods/{modId}"),
            new(typeof(WeggBigCraftableTexture), WeggAssetFolder.bigcraftables, ModAssetHandler.FileExtension.Png, $"Mods/{modId}"),

            // Vanilla-like keys.
            new(typeof(WeggMineMap), WeggAssetFolder.mines, ModAssetHandler.FileExtension.Tmx, "Maps/Mines"),
            new(typeof(WeggMineTexture), WeggAssetFolder.mines, ModAssetHandler.FileExtension.Png, "Maps/Mines"),
            new(typeof(WeggMonsterTexture), WeggAssetFolder.mines, ModAssetHandler.FileExtension.Png, "Characters/Monsters"),
        });

        _handler.SetUpAssets();
        _gamePaths = _handler.GamePaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public void Register()
    {
        if (_registered)
            return;

        _helper.Events.Content.AssetRequested += OnAssetRequested;
        _registered = true;
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        var key = e.NameWithoutLocale.BaseName.Replace('\\', '/').ToLowerInvariant();
        if (!_gamePaths.Contains(key))
            return;

        _handler.LoadModAssets(e);
    }

    public void Dispose()
    {
        if (_registered)
            _helper.Events.Content.AssetRequested -= OnAssetRequested;

        _registered = false;
        GC.SuppressFinalize(this);
    }
}
