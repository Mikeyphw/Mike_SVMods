using System;
using LetMeHatch.Application.Gateways;
using LetMeHatch.Application.UseCases;
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
        IAssetLoads assetLoads)
    {
        if (rng is null) throw new ArgumentNullException(nameof(rng));
        Gateways = gateways ?? throw new ArgumentNullException(nameof(gateways));
        FarmAnimalsEdits = farmAnimalsEdits ?? throw new ArgumentNullException(nameof(farmAnimalsEdits));
        ObjectsEdits = objectsEdits ?? throw new ArgumentNullException(nameof(objectsEdits));
        MachinesEdits = machinesEdits ?? throw new ArgumentNullException(nameof(machinesEdits));
        AssetLoads = assetLoads ?? throw new ArgumentNullException(nameof(assetLoads));

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
}
