using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Microsoft.Xna.Framework.Graphics;

namespace WildEggsHatching.Infrastructure.Assets;

/// <summary>
/// Minimal asset loader for custom textures referenced by game data (e.g. ObjectData.Texture).
/// </summary>
public sealed class ModAssetLoader : IDisposable
{
    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Dictionary<string, string> _textures;
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
        if (_textures.TryGetValue(e.NameWithoutLocale.BaseName, out var relPath))
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
