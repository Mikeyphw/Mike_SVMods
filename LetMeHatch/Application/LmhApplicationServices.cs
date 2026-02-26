using System;
using LetMeHatch.Application.Gateways;
using LetMeHatch.Application.UseCases;
using LetMeHatch.Core.ClonePipeline;
using Mikew.NestCore.Framework.Random;

namespace LetMeHatch.Application;

/// <summary>Composition root for LMH application-layer services.</summary>
public sealed class LmhApplicationServices
{
    public LmhApplicationServices(
        IRandomProvider rng,
        LmhGateways gateways,
        IFarmAnimalsAssetEdits farmAnimalsEdits,
        IObjectsAssetEdits objectsEdits,
        IMachinesAssetEdits machinesEdits,
        IAssetLoads assetLoads,
        IColorResolver? colorResolver = null,
        IClonePlanBuilder? clonePlanBuilder = null,
        ICloneObjectDataBuilder? cloneObjectDataBuilder = null,
        ICloneTextureNamer? cloneTextureNamer = null)
    {
        if (rng is null) throw new ArgumentNullException(nameof(rng));
        Gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        FarmAnimalsEdits = farmAnimalsEdits ?? throw new ArgumentNullException(nameof(farmAnimalsEdits));
        ObjectsEdits = objectsEdits ?? throw new ArgumentNullException(nameof(objectsEdits));
        MachinesEdits = machinesEdits ?? throw new ArgumentNullException(nameof(machinesEdits));
        AssetLoads = assetLoads ?? throw new ArgumentNullException(nameof(assetLoads));

        ColorResolver = colorResolver ?? new ColorResolver();
        ClonePlanBuilder = clonePlanBuilder ?? new ClonePlanBuilder();
        CloneObjectDataBuilder = cloneObjectDataBuilder ?? new ThrowingCloneObjectDataBuilder();
        CloneTextureNamer = cloneTextureNamer ?? new ThrowingCloneTextureNamer();

        CloneIds = new CloneIdUseCase();
        EggSelection = new EggSelectionUseCase(rng);
    }

    public LmhGateways Gateways { get; }

    public CloneIdUseCase CloneIds { get; }
    public EggSelectionUseCase EggSelection { get; }

    public IFarmAnimalsAssetEdits FarmAnimalsEdits { get; }
    public IObjectsAssetEdits ObjectsEdits { get; }
    public IMachinesAssetEdits MachinesEdits { get; }
    public IAssetLoads AssetLoads { get; }

    public IColorResolver ColorResolver { get; }
    public IClonePlanBuilder ClonePlanBuilder { get; }
    public ICloneObjectDataBuilder CloneObjectDataBuilder { get; }
    public ICloneTextureNamer CloneTextureNamer { get; }

    private sealed class ThrowingColorResolver : IColorResolver
    {
        public Microsoft.Xna.Framework.Color Resolve(ColorResolutionContext context) =>
            throw new NotSupportedException("Clone pipeline is not wired yet.");
    }

    private sealed class ThrowingClonePlanBuilder : IClonePlanBuilder
    {
        public ClonePlan Build(ClonePlanBuildRequest request) =>
            throw new NotSupportedException("Clone pipeline is not wired yet.");
    }

    private sealed class ThrowingCloneObjectDataBuilder : ICloneObjectDataBuilder
    {
        public CloneObjectData Build(CloneObjectDataRequest request) =>
            throw new NotSupportedException("Clone pipeline is not wired yet.");
    }

    private sealed class ThrowingCloneTextureNamer : ICloneTextureNamer
    {
        public string GetCloneTextureAssetName(string cloneItemId) =>
            throw new NotSupportedException("Clone pipeline is not wired yet.");
    }
}
