using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Infrastructure.Config;
using LetMeHatchThisEgg.Infrastructure.Logging;
using Mikew.BirdsOfAFeather.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace LetMeHatchThisEgg.Infrastructure.Content;

public sealed class SmapiContentCoordinator : IDisposable
{
    private readonly CoreContext _core;
    private readonly SmapiConfigProvider<ModConfig> _configProvider;
    private readonly ILog _log;
    private readonly List<IAssetEditor> _editors;
    private bool _registered;

    public SmapiContentCoordinator(
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider,
        ILog log,
        IEnumerable<IAssetEditor> editors)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _editors = editors?.ToList() ?? throw new ArgumentNullException(nameof(editors));
    }

    public void Register()
    {
        if (_registered)
            return;

        _core.Helper.Events.Content.AssetRequested += OnAssetRequested;
        _registered = true;

        _log.Log($"Registered content coordinator with {_editors.Count} editors.", LogLevel.Debug);
    }

    public void InvalidateAll()
    {
        foreach (var name in _editors.Select(e => e.AssetName).Distinct())
            _core.Helper.GameContent.InvalidateCache(name);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        var ctx = new ContentContext(_core, _configProvider.Config, _log);

        foreach (var editor in _editors.OrderBy(e => e.Priority))
        {
            if (!e.NameWithoutLocale.IsEquivalentTo(editor.AssetName))
                continue;

            e.Edit(asset => editor.Edit(asset, ctx), editor.Priority);
        }
    }

    public void Dispose()
    {
        if (_registered)
            _core.Helper.Events.Content.AssetRequested -= OnAssetRequested;

        _registered = false;
        _editors.Clear();
        GC.SuppressFinalize(this);
    }
}
