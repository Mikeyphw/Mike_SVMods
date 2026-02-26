using System;
using System.Collections.Generic;
using LetMeHatch.Application;
using LetMeHatch.Config;
using LetMeHatch.GameAssetManagement;
using LetMeHatch.Infrastructure.Content.Editors;
using LetMeHatch.Infrastructure.Content.Loaders;
using Mikew.NestCore.Framework;
using Mikew.NestCore.Helpers;
using Mikew.NestCore.Helpers.Config;
using Mikew.NestCore.Helpers.Content;
using Mikew.NestCore.Helpers.Logging;
using static LetMeHatch.EnumsAndStrings.Enums;

namespace LetMeHatch.Infrastructure.Content;

/// <summary>Creates the LMH content pipeline (loaders + editors) in a single place.</summary>
public static class LetMeHatchContentPipeline
{
    public static AssetPipelineHost<ContentContext> Create(
        CoreContext core,
        SmapiConfigProvider<ModConfig> configProvider,
        ILog log,
        ModAssetHandler assetHandler,
        GameAssetManager assets,
        LmhApplicationServices app)
    {
        if (core is null) throw new ArgumentNullException(nameof(core));
        if (configProvider is null) throw new ArgumentNullException(nameof(configProvider));
        if (log is null) throw new ArgumentNullException(nameof(log));
        if (assetHandler is null) throw new ArgumentNullException(nameof(assetHandler));
        if (assets is null) throw new ArgumentNullException(nameof(assets));
        if (app is null) throw new ArgumentNullException(nameof(app));

        var pipeline = new AssetPipelineHost<ContentContext>(
            core.Helper,
            log,
            () => new ContentContext(core, configProvider, log, assetHandler, assets, app));

        foreach (var loader in CreateLoaders(assetHandler, app))
            pipeline.RegisterHandler(new LmhAssetLoaderHandler(loader));

        foreach (var reg in CreateEditors(app))
            pipeline.RegisterEditor(reg.Editor, order: reg.Order);

        foreach (var target in CreateInvalidationTargets(assetHandler, app))
            pipeline.AddInvalidationTarget(target);

        return pipeline;
    }

    private static IReadOnlyCollection<string> CreateInvalidationTargets(ModAssetHandler assetHandler, LmhApplicationServices app)
    {
        var targets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var editor in CreateEditors(app))
            targets.Add(editor.Editor.AssetName);

        targets.Add(assetHandler.GetAssetPath(Assets.Object.ClonedItems, ModAssetHandler.AssetPathType.GamePath));

        return targets;
    }

    private static IEnumerable<IAssetLoader> CreateLoaders(ModAssetHandler assetHandler, LmhApplicationServices app)
    {
        yield return new ClonedEggsAssetLoader(assetHandler, app.AssetLoads);
        yield return new DynamicCloneObjectTextureLoader(ModEntry.ModManifestInstance.UniqueID);
    }

    private static IEnumerable<EditorRegistration> CreateEditors(LmhApplicationServices app)
    {
        yield return new EditorRegistration(new FarmAnimalsBreedingSystemEditor(), Order: 10);
        yield return new EditorRegistration(new FarmAnimalsEggListByNameEditor(), Order: 20);
        yield return new EditorRegistration(new FarmAnimalsClonesEditor(), Order: 30);
        yield return new EditorRegistration(new FarmAnimalsBirthTextEditor(), Order: 40);
        yield return new EditorRegistration(new FarmAnimalsClonePlaceholdersEditor(), Order: 50);
        yield return new EditorRegistration(new FarmAnimalsReplaceOriginalItemsEditor(), Order: 60);
        yield return new EditorRegistration(new ObjectsClonedEggsEditor(), Order: 70);
        yield return new EditorRegistration(new ObjectsFertilizedEggsEditor(), Order: 80);
        yield return new EditorRegistration(new MachinesRulesEditor(), Order: 90);
    }

    internal sealed record EditorRegistration(IAssetEditor<ContentContext> Editor, int Order);
}
