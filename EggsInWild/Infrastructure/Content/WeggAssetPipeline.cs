using System;
using System.Collections.Generic;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Content;
using Mikew.NestCore.Helpers.Timing;
using StardewModdingAPI;
using EggsInWild.Core.Enums;

namespace EggsInWild.Infrastructure.Content;

/// <summary>Wires NestCore's asset-request pipeline to this mod's editors and mod assets.</summary>
public sealed class WeggAssetPipeline : IDisposable
{
    private readonly AssetRequestCoordinator<ContentContext> _coordinator;
    private readonly AssetInvalidationBatcher _invalidation;
    private readonly HashSet<string> _assetNames = new(StringComparer.OrdinalIgnoreCase);
    private bool _attached;

    public WeggAssetPipeline(IModHelper helper, IMonitor monitor, Func<ContentContext> contextFactory)
    {
        _coordinator = new AssetRequestCoordinator<ContentContext>(helper, monitor, contextFactory);
        _invalidation = new AssetInvalidationBatcher(helper, monitor, new NextTickScheduler(helper, monitor));
    }

    public void RegisterModAssets(ModAssetHandler handler, int order = -100)
    {
        var router = new ModAssetRequestRouter(handler);

        _coordinator.Register(
            AssetRequestRegistration<ContentContext>.FromHandler(
                debugName: "ModAssets",
                order: order,
                isMatch: e => handler.HasGamePath(e.NameWithoutLocale.BaseName),
                handle: (_, e) => router.TryHandle(e),
                stopAfterHandle: false
            )
        );
    }

    public void RegisterEditor(IAssetEditor<ContentContext> editor, ProgressionStep activationStep, Func<ProgressionStep> getStep, int order)
    {
        _assetNames.Add(editor.AssetName);

        _coordinator.Register(
            AssetRequestRegistration<ContentContext>.FromEditor(
                editor: editor,
                order: order,
                shouldApply: (_, __) => getStep() >= activationStep
            )
        );
    }

    public void Attach()
    {
        if (_attached)
            return;

        _coordinator.Attach();
        _attached = true;
    }

    public void RequestInvalidateAll()
    {
        foreach (var asset in _assetNames)
            _invalidation.Request(asset);
    }


    public void RequestInvalidate(string assetName)
        => _invalidation.Request(assetName);



    public void Dispose()
    {
        _coordinator.Dispose();
        _invalidation.Dispose();
    }
}
