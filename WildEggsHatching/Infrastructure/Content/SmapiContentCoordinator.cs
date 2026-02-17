using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Events;
using WildEggsHatching.Core.Enums;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using WildEggsHatching.Infrastructure.Config;

namespace WildEggsHatching.Infrastructure.Content;

public sealed class SmapiContentCoordinator : IDisposable
{
    public sealed record Registration(IAssetEditor Editor, ProgressionStep ActivationStep);

    private readonly CoreContext _ctx;
    private readonly IConfigProvider _config;
    private readonly Logger _log;
    private readonly Func<ProgressionStep> _getStep;
    private readonly List<Registration> _registrations;
    private bool _registered;

    public SmapiContentCoordinator(CoreContext ctx, IConfigProvider config, Logger log, Func<ProgressionStep> getStep, IEnumerable<Registration> registrations)
    {
        _ctx = ctx;
        _config = config;
        _log = log;
        _getStep = getStep;
        _registrations = registrations.ToList();
    }

    public void Register()
    {
        if (_registered)
            return;

        _ctx.Helper.Events.Content.AssetRequested += OnAssetRequested;
        _registered = true;
        _log.Log($"Registered content coordinator with {_registrations.Count} editors.", StardewModdingAPI.LogLevel.Debug);
    }

    public void InvalidateAll()
    {
        foreach (var name in _registrations.Select(r => r.Editor.AssetName).Distinct())
            _ctx.Helper.GameContent.InvalidateCache(name);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        var step = _getStep();

        foreach (var reg in _registrations.OrderBy(r => r.Editor.Priority))
        {
            if (step < reg.ActivationStep)
                continue;

            if (!e.NameWithoutLocale.IsEquivalentTo(reg.Editor.AssetName))
                continue;

            var contentCtx = new ContentContext(
                helper: _ctx.Helper,
                monitor: _ctx.Monitor,
                i18n: _ctx.Translation,
                config: _config.Config,
                step: step
            );

            e.Edit(asset => reg.Editor.Edit(asset, contentCtx), reg.Editor.Priority);
        }
    }

    public void Dispose()
    {
        if (_registered)
            _ctx.Helper.Events.Content.AssetRequested -= OnAssetRequested;

        _registered = false;
        _registrations.Clear();
        GC.SuppressFinalize(this);
    }
}
