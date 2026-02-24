using System;
using System.Collections.Generic;
using Mikew.NestCore.Helpers.Content;
using StardewModdingAPI.Events;

namespace LetMeHatch.Infrastructure.Content;

/// <summary>
/// LMH's asset pipeline wrapper which owns NestCore coordination plus centralized cache invalidation.
/// </summary>
public sealed class LmhAssetPipeline : IDisposable
{
    private readonly AssetRequestCoordinator<ContentContext> _coordinator;
    private readonly AssetInvalidationBatcher _invalidation;
    private readonly IReadOnlyCollection<string> _invalidationTargets;
    private bool _attached;

    public LmhAssetPipeline(
        AssetRequestCoordinator<ContentContext> coordinator,
        AssetInvalidationBatcher invalidation,
        IReadOnlyCollection<string> invalidationTargets)
    {
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _invalidation = invalidation ?? throw new ArgumentNullException(nameof(invalidation));
        _invalidationTargets = invalidationTargets ?? throw new ArgumentNullException(nameof(invalidationTargets));
    }

    public void Attach()
    {
        if (_attached)
            return;

        _coordinator.Attach();
        _attached = true;
    }

    public void HandleAssetRequested(AssetRequestedEventArgs e)
    {
        // Event-driven handling is owned by the coordinator after Attach().
    }

    public IEnumerable<string> GetDebugReportLines() => Array.Empty<string>();

    public void RequestInvalidateAll()
    {
        foreach (var target in _invalidationTargets)
            _invalidation.Request(target);
    }

    public void Dispose() => _coordinator.Dispose();
}
