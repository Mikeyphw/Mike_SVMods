using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.GameAssetManagement;
using LetMeHatchThisEgg.Infrastructure.Config;
using LetMeHatchThisEgg.Infrastructure.Content.Editors;
using LetMeHatchThisEgg.Infrastructure.Content.Loaders;
using LetMeHatchThisEgg.Infrastructure.Logging;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;

namespace LetMeHatchThisEgg.Infrastructure.Content;

/// <summary>Creates the LMH content pipeline (loaders + editors) in a single place.</summary>
public static class LetMeHatchContentPipeline
{
    public static SmapiContentCoordinator Create(
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider,
        ILog log,
        ModAssetHandler assetHandler,
        GameAssetManager assets)
    {
        if (core is null) throw new ArgumentNullException(nameof(core));
        if (configProvider is null) throw new ArgumentNullException(nameof(configProvider));
        if (log is null) throw new ArgumentNullException(nameof(log));
        if (assetHandler is null) throw new ArgumentNullException(nameof(assetHandler));
        if (assets is null) throw new ArgumentNullException(nameof(assets));

        var loaders = new List<IAssetLoader>
        {
            new MaleClonesTextureLoader(assets),
            new ClonedEggsAssetLoader(assetHandler, assets),
            new BoafGamePathsLoader(assetHandler),
        };

        var editors = new List<IAssetEditor>
        {
            // Farm animals
            new FarmAnimalsClonePlaceholdersEditor(assets),
            new FarmAnimalsClonesEditor(assets),
            new FarmAnimalsReplaceOriginalItemsEditor(assets),
            new FarmAnimalsEggListByNameEditor(assets),
            new FarmAnimalsBirthTextEditor(assets),
            new FarmAnimalsBreedingSystemEditor(assets),

            // Objects
            new ObjectsClonedEggsEditor(assets),
            new ObjectsFertilizedEggsEditor(assets),

            // Machines
            new MachinesRulesEditor(assets),
        };

        return new SmapiContentCoordinator(core, configProvider, log, loaders, editors);
    }
}
