using System;
using System.Collections.Generic;
using System.Linq;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Infrastructure.Config;
using LetMeHatchThisEgg.Infrastructure.Content.Loaders;
using LetMeHatchThisEgg.Infrastructure.Logging;
using Mikew.BirdsOfAFeather.Framework;
using StardewModdingAPI.Events;

namespace LetMeHatchThisEgg.Infrastructure.Content;

/// <summary>Routes SMAPI asset requests through a loader-first, editor-second pipeline.</summary>
public sealed class SmapiContentCoordinator : IDisposable
{
    private readonly CoreContext _core;
    private readonly SmapiConfigProvider<ModConfig> _configProvider;
    private readonly ILog _log;
    private readonly List<IAssetLoader> _loaders;
    private readonly List<IAssetEditor> _editors;
    private bool _registered;

    public SmapiContentCoordinator(
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider,
        ILog log,
        IEnumerable<IAssetEditor> editors)
        : this(core, configProvider, log, loaders: Array.Empty<IAssetLoader>(), editors: editors)
    {
    }

    public SmapiContentCoordinator(
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider,
        ILog log,
        IEnumerable<IAssetLoader> loaders,
        IEnumerable<IAssetEditor> editors)
    {
        _core = core ?? throw new ArgumentNullException(nameof(core));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _loaders = loaders?.ToList() ?? throw new ArgumentNullException(nameof(loaders));
        _editors = editors?.ToList() ?? throw new ArgumentNullException(nameof(editors));
    }

    /// <summary>Optionally registers this coordinator directly with SMAPI events.</summary>
    public void Register()
    {
        if (_registered)
            return;

        _core.Helper.Events.Content.AssetRequested += OnAssetRequested;
        _registered = true;

        _log.Log(
            $"Registered content coordinator with {_loaders.Count} loaders and {_editors.Count} editors.",
            LogLevel.Debug
        );
    }

    /// <summary>Handles an asset request. Intended to be called from a mod-owned AssetRequested handler.</summary>
    public void HandleAssetRequested(AssetRequestedEventArgs e)
    {
        var ctx = new ContentContext(_core, _configProvider.Config, _log);
        var assetNameLower = e.NameWithoutLocale.BaseName.ToLowerInvariant();

        foreach (var loader in _loaders.OrderBy(l => l.Priority))
        {
            if (loader.CanLoad(assetNameLower, e))
                loader.Load(assetNameLower, e);
        }

        foreach (var editor in _editors.OrderBy(ed => ed.Priority))
        {
            if (!e.NameWithoutLocale.IsEquivalentTo(editor.AssetName))
                continue;

            e.Edit(asset => editor.Edit(asset, ctx), editor.Priority);
        }
    }

    public void InvalidateAll()
    {
        foreach (var name in _editors.Select(e => e.AssetName).Distinct())
            _core.Helper.GameContent.InvalidateCache(name);
    }

    
    /// <summary>Returns a human-friendly report of the registered loaders and editors (for console diagnostics).</summary>
    public IEnumerable<string> GetDebugReportLines()
    {
        yield return $"Content pipeline: registered={_registered}; loaders={_loaders.Count}; editors={_editors.Count}";
        yield return "Loaders (run before editors):";

        foreach (var loader in _loaders.OrderBy(l => l.Priority).ThenBy(l => l.GetType().Name))
        {
            var typeName = loader.GetType().Name;
            var handles = loader is IContentHandlerDebugInfo d ? d.DebugHandles : "n/a";
            var gates = loader is IContentHandlerDebugInfo g ? g.DebugGates : "n/a";
            yield return $"- [{loader.Priority}] {typeName} — handles: {handles}; gates: {gates}";
        }

        yield return "Editors:";
        foreach (var editor in _editors.OrderBy(e => e.Priority).ThenBy(e => e.AssetName).ThenBy(e => e.GetType().Name))
        {
            var typeName = editor.GetType().Name;
            var gates = editor is IContentHandlerDebugInfo g ? g.DebugGates : "n/a";
            yield return $"- [{editor.Priority}] {typeName} (asset: {editor.AssetName}) — gates: {gates}";
        }
    }
private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        => HandleAssetRequested(e);

    public void Dispose()
    {
        if (_registered)
            _core.Helper.Events.Content.AssetRequested -= OnAssetRequested;

        _registered = false;
        _loaders.Clear();
        _editors.Clear();
        GC.SuppressFinalize(this);
    }
}
