using System;
using System.Collections.Generic;
using LetMeHatchThisEgg.Application;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.GameAssetManagement;
using LetMeHatchThisEgg.Infrastructure.Content.Editors;
using LetMeHatchThisEgg.Infrastructure.Content.Loaders;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using Mikew.BirdsOfAFeather.Helpers.Config;
using Mikew.BirdsOfAFeather.Helpers.Content;
using Mikew.BirdsOfAFeather.Helpers.Logging;

namespace LetMeHatchThisEgg.Infrastructure.Content;

/// <summary>Creates the LMH content pipeline (loaders + editors) in a single place.</summary>
public static class LetMeHatchContentPipeline
{
    public static LmhAssetPipeline Create(
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider,
        ILog log,
        ModAssetHandler assetHandler,
        GameAssetManager assets,
        LmhApplicationServices app,
        AssetInvalidationBatcher invalidation)
    {
        if (core is null) throw new ArgumentNullException(nameof(core));
        if (configProvider is null) throw new ArgumentNullException(nameof(configProvider));
        if (log is null) throw new ArgumentNullException(nameof(log));
        if (assetHandler is null) throw new ArgumentNullException(nameof(assetHandler));
        if (assets is null) throw new ArgumentNullException(nameof(assets));
        if (app is null) throw new ArgumentNullException(nameof(app));
        if (invalidation is null) throw new ArgumentNullException(nameof(invalidation));

        var coordinator = new AssetRequestCoordinator<ContentContext>(
            core.Helper,
            log,
            () => new ContentContext(core, configProvider, log, assetHandler, assets, app));

        foreach (var loader in CreateLoaders(assetHandler, app))
            coordinator.Register(new LmhAssetLoaderHandler(loader));

        foreach (var reg in CreateEditors(app))
            coordinator.RegisterEditor(reg.Editor, order: reg.Order);

        return new LmhAssetPipeline(coordinator, invalidation, CreateInvalidationTargets(assetHandler, app));
    }

    private static IReadOnlyCollection<string> CreateInvalidationTargets(ModAssetHandler assetHandler, LmhApplicationServices app)
    {
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var editor in CreateEditors(app))
            targets.Add(editor.Editor.AssetName);

        targets.Add(assetHandler.GetAssetPath(ModAssetHandler.Assets.Boaf.GamePaths, ModAssetHandler.AssetPathType.GamePath));
        targets.Add(assetHandler.GetAssetPath(ModAssetHandler.Assets.Object.ClonedItems, ModAssetHandler.AssetPathType.GamePath));
        targets.Add(assetHandler.GetAssetPath(ModAssetHandler.Assets.Texture.MaleClones, ModAssetHandler.AssetPathType.GamePath));

        return targets;
    }

    private static IEnumerable<IAssetLoader> CreateLoaders(ModAssetHandler assetHandler, LmhApplicationServices app)
    {
        yield return new BoafGamePathsLoader(assetHandler, app.AssetLoads);
        yield return new ClonedEggsAssetLoader(assetHandler, app.AssetLoads);
        yield return new MaleClonesTextureLoader(assetHandler, app.AssetLoads);
    }

    private static IEnumerable<EditorRegistration> CreateEditors(LmhApplicationServices app)
    {
        yield return new EditorRegistration(new FarmAnimalsBreedingSystemEditor(), order: 10);
        yield return new EditorRegistration(new FarmAnimalsEggListByNameEditor(), order: 20);
        yield return new EditorRegistration(new FarmAnimalsClonesEditor(), order: 30);
        yield return new EditorRegistration(new FarmAnimalsBirthTextEditor(), order: 40);
        yield return new EditorRegistration(new FarmAnimalsClonePlaceholdersEditor(), order: 50);
        yield return new EditorRegistration(new FarmAnimalsReplaceOriginalItemsEditor(), order: 60);
        yield return new EditorRegistration(new ObjectsClonedEggsEditor(), order: 70);
        yield return new EditorRegistration(new ObjectsFertilizedEggsEditor(), order: 80);
        yield return new EditorRegistration(new MachinesRulesEditor(), order: 90);
    }

    internal sealed record EditorRegistration(IAssetEditor<ContentContext> Editor, int Order);
}
