using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Events;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Smapi;

namespace WildEggsHatching.Infrastructure.Content;

public sealed class SmapiContentCoordinator : IDisposable
{
    public sealed record Registration(IAssetEditor Editor, ProgressionStep ActivationStep);

    private readonly ModContext _ctx;
    private readonly Func<ProgressionStep> _getStep;
    private readonly List<Registration> _registrations;
    private bool _registered;

    public SmapiContentCoordinator(ModContext ctx, Func<ProgressionStep> getStep, IEnumerable<Registration> registrations)
    {
        _ctx = ctx;
        _getStep = getStep;
        _registrations = registrations.ToList();
    }

    public void Register()
    {
        if (_registered)
            return;

        _ctx.Helper.Events.Content.AssetRequested += OnAssetRequested;
        _registered = true;
        _ctx.Log($"Registered content coordinator with {_registrations.Count} editors.");
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
                config: _ctx.ConfigProvider.Config,
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
