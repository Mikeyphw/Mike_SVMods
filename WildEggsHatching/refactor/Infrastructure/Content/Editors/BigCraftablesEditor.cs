using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.BigCraftables;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Content;

namespace WildEggsHatching.Infrastructure.Content.Editors;

/// <summary>
/// Editor for Data/BigCraftables asset - adds wild egg incubator.
/// Refactored to use factory methods and support sprite patching.
/// </summary>
public sealed class BigCraftablesEditor : ProgressionAwareAssetEditor
{
    /// <inheritdoc />
    public override string AssetName => GameAssets.BigCraftables;

    /// <summary>
    /// Creates a new big craftables editor.
    /// </summary>
    public BigCraftablesEditor() : base() { }

    /// <summary>
    /// Creates a new big craftables editor with dependencies.
    /// </summary>
    public BigCraftablesEditor(
        PhaseProgressionService progressionService,
        IConfigProvider configProvider,
        IMonitor monitor,
        IModHelper modHelper) 
        : base(progressionService, configProvider, monitor, modHelper) { }

    /// <inheritdoc />
    public override void Edit<T>(IAssetData<T> asset, string assetName)
    {
        if (assetName != GameAssets.BigCraftables)
            return;

        var craftables = asset.AsDictionary<string, BigCraftableData>().Data;

        // Part V: Wild Egg Incubator
        if (HasReachedStep(ProgressionStep.PartV))
        {
            AddCraftable(craftables, WildEggConstants.Items.WildEggIncubator, CreateIncubator());
        }

        Log($"BigCraftablesEditor applied edits to {craftables.Count} craftables");
    }

    /// <summary>
    /// Safely adds a craftable.
    /// </summary>
    private void AddCraftable(IDictionary<string, BigCraftableData> craftables, string id, BigCraftableData data)
    {
        if (!craftables.ContainsKey(id))
        {
            craftables[id] = data;
            Log($"Added big craftable: {id}", LogLevel.Trace);
        }
    }

    /// <summary>
    /// Creates the wild egg incubator data.
    /// </summary>
    private BigCraftableData CreateIncubator()
    {
        // Use factory method from base class
        return CreateBigCraftableData(
            name: WildEggConstants.Items.WildEggIncubator,
            displayName: Translate("object.WildEggIncubator.name", "Wild Egg Incubator"),
            description: Translate("object.WildEggIncubator.description", "A specialized incubator for hatching wild eggs. Works with all wild egg types."),
            texturePath: WildEggConstants.Textures.Incubator,
            spriteIndex: 0,
            canBePlacedIndoors: true,
            canBePlacedOutdoors: false,
            providesLight: false,
            price: 5000,
            type: "Crafting",
            category: -9
        );
    }

    /// <summary>
    /// Edits the Wild Egg Incubator sprite image.
    /// Patches the source image to create the correct sprite layout.
    /// </summary>
    /// <param name="editor">The image asset editor.</param>
    public void EditWildEggIncubatorAsset(IAssetDataForImage editor)
    {
        if (ModHelper == null)
        {
            Log("Cannot edit incubator asset - ModHelper not available", LogLevel.Warn);
            return;
        }

        // Load the source texture from mod content
        string texturePath = $"assets/BigCraftables/WildEggIncubator.png";
        var sourceImage = LoadModTexture(texturePath);
        
        if (sourceImage == null)
        {
            Log($"Could not load source texture: {texturePath}", LogLevel.Warn);
            return;
        }

        // Patch the image: copy from source area (0, 32, 16, 32) to target area (0, 0, 16, 32)
        // This converts from 32x64 format to 16x32 format
        PatchImage(
            editor,
            sourceImage,
            new Rectangle(0, 32, 16, 32),
            new Rectangle(0, 0, 16, 32)
        );

        Log("Wild Egg Incubator sprite patched successfully", LogLevel.Trace);
    }

    /// <inheritdoc />
    public override bool ShouldApply() => HasReachedStep(ProgressionStep.PartV);
}
