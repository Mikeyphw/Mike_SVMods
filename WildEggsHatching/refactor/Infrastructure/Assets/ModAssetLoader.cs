using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Microsoft.Xna.Framework.Graphics;
using xTile;

namespace WildEggsHatching.Infrastructure.Assets;

/// <summary>
/// Minimal asset loader for custom textures referenced by game data (e.g. ObjectData.Texture).
/// </summary>
public sealed class ModAssetLoader : IDisposable
{
    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Dictionary<string, string> _textures;
    private readonly Dictionary<string, string> _maps;
    private bool _registered;

    public ModAssetLoader(IModHelper helper, IMonitor monitor)
    {
        _helper = helper;
        _monitor = monitor;

        _textures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [$"Mods/{Constants.ModId}/eggs"] = "assets/Objects/eggs.png",
            [$"Mods/{Constants.ModId}/scroll"] = "assets/Objects/scroll.png",
            [$"Mods/{Constants.ModId}/elixir"] = "assets/Objects/elixir.png",
            [$"Mods/{Constants.ModId}/note"] = "assets/Objects/note.png",
            [$"Mods/{Constants.ModId}/wildeggincubator"] = "assets/BigCraftables/wildeggincubator.png",

            // DesertTomb mine assets (served as vanilla-like content keys).
            ["Maps/Mines/Z_TombTilesheet"] = "assets/Mines/Z_TombTilesheet.png",
            ["Characters/Monsters/SandyDuggy"] = "assets/Mines/SandyDuggy.png",
            ["Characters/Monsters/BruteMummy"] = "assets/Mines/BruteMummy.png",
            ["Characters/Monsters/SkeletonMummy"] = "assets/Mines/SkeletonMummy.png",
            ["Characters/Monsters/ThePharaoh"] = "assets/Mines/ThePharaoh.png",
            ["Characters/Monsters/ThePharaoh_CombatMode"] = "assets/Mines/ThePharaoh_CombatMode.png",
        };

        _maps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Maps/Mines/Level77377"] = "assets/Mines/Level77377.tmx",
            ["Maps/Mines/Level14"] = "assets/Mines/Level14.tmx",
            ["Maps/Mines/Level19"] = "assets/Mines/Level19.tmx",
            ["Maps/Mines/Level28"] = "assets/Mines/Level28.tmx",
            ["Maps/Mines/Level39"] = "assets/Mines/Level39.tmx",
        };
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
        var baseName = e.NameWithoutLocale.BaseName;
        var normalized = baseName.Replace('\\', '/');

        if (_maps.TryGetValue(baseName, out var mapRel) || _maps.TryGetValue(normalized, out mapRel))
        {
            e.LoadFromModFile<Map>(mapRel, AssetLoadPriority.Medium);
            return;
        }

        if (_textures.TryGetValue(baseName, out var relPath) || _textures.TryGetValue(normalized, out relPath))
        {
            e.LoadFromModFile<Texture2D>(relPath, AssetLoadPriority.Medium);
            return;
        }

        // Compatibility: some callers may request without the Mods/<id> prefix (rare).
        // Keep the fallback quiet unless explicitly requested later.
    }

    public void Dispose()
    {
        if (_registered)
            _helper.Events.Content.AssetRequested -= OnAssetRequested;

        _registered = false;
        GC.SuppressFinalize(this);
    }
}
