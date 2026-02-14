using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Content;

namespace WildEggsHatching.Infrastructure.Content.Editors;

/// <summary>
/// Editor for Data/Objects asset - adds wild egg items and special objects.
/// Refactored to be a pure infrastructure editor - progression gating is handled by coordinator.
/// </summary>
public sealed class ObjectsEditor : WildEggAssetEditor
{
    /// <inheritdoc />
    public override string AssetName => GameAssets.Objects;

    /// <summary>
    /// Locations that don't have normal eggs.
    /// </summary>
    private static readonly HashSet<LocationType> LocationsWithoutNormalEgg = new()
    {
        LocationType.Magical
    };

    /// <summary>
    /// Locations that don't have large eggs.
    /// </summary>
    private static readonly HashSet<LocationType> LocationsWithoutLargeEgg = new()
    {
        LocationType.Magical
    };

    /// <summary>
    /// Prismatic Valley Framework custom field keys.
    /// </summary>
    private const string PrismaticValleyColorKey = "JollyLlama.PrismaticValleyFramework/Color";
    private const string PrismaticValleyPaletteKey = "JollyLlama.PrismaticValleyFramework/Palette";
    private const string CustomPalette = "Custom Palette";

    /// <summary>
    /// Context tag constants.
    /// </summary>
    private const string ForageContextTag = "forage_item";
    private const string EggCategoryContextTag = "category_egg";
    private const string WildEggContextTag = "wildegg_item";

    /// <summary>
    /// Creates a new objects editor.
    /// </summary>
    public ObjectsEditor() : base() { }
    /// Creates a new objects editor with dependencies.
    /// </summary>
    public ObjectsEditor(
        IMonitor monitor,
        IModHelper modHelper)
        : base(monitor, modHelper) { }

    /// <inheritdoc />
    public override void Edit<T>(IAssetData<T> asset, string assetName)
    {
        if (assetName != GameAssets.Objects)
            return;

        var objects = asset.AsDictionary<string, ObjectData>().Data;

        // Add wild eggs for all locations (Part I)
        AddWildEggsForAllLocations(objects);

        // Edit existing wild egg (FarmWEgg) - Part I
        EditWildEgg(objects);

        // Add special items - these require higher progression
        // Note: The coordinator gates the minimum step, but we add all items 
        // since the higher-tier items won't be usable until progression is reached anyway
        AddOsirisScroll(objects);
        AddElixir(objects);

        Log($"ObjectsEditor applied edits for {objects.Count} objects");
    }

    /// <summary>
    /// Adds wild eggs for all location types.
    /// </summary>
    private void AddWildEggsForAllLocations(IDictionary<string, ObjectData> objects)
    {
        foreach (LocationType location in System.Enum.GetValues(typeof(LocationType)))
        {
            // Skip Farm - uses different IDs
            if (location == LocationType.Farm)
                continue;

            AddWildEggsForLocation(objects, location);
        }
    }

    /// <summary>
    /// Adds wild eggs for a specific location type.
    /// </summary>
    private void AddWildEggsForLocation(
        IDictionary<string, ObjectData> objects, 
        LocationType location)
    {
        // Create all 4 egg types using helper
        AddWildEggObject(objects, location, isMagic: false, isLarge: false);
        AddWildEggObject(objects, location, isMagic: false, isLarge: true);
        AddWildEggObject(objects, location, isMagic: true, isLarge: false);
        AddWildEggObject(objects, location, isMagic: true, isLarge: true);
    }

    /// <summary>
    /// Adds a single wild egg object to the data.
    /// </summary>
    private void AddWildEggObject(
        IDictionary<string, ObjectData> objects,
        LocationType location,
        bool isMagic,
        bool isLarge)
    {
        string eggId = WildEggConstants.CreateEggId(location.ToString(), isMagic, isLarge);

        // Check if already exists
        if (objects.ContainsKey(eggId))
            return;

        // Get i18n key and fallback
        string eggTypeKey = GetEggTypeKey(isMagic, isLarge);
        
        // Create using factory method with i18n
        var eggData = CreateObjectData(
            description: Translate($"object.{eggTypeKey}.description", GetEggDescription(location, isMagic, isLarge)),
            displayName: Translate($"object.{eggTypeKey}.name", GetEggDisplayName(location, isMagic, isLarge)),
            name: eggId,
            texturePath: WildEggConstants.Textures.Eggs,
            spriteIndex: (isMagic ? 2 : 0) + (isLarge ? 1 : 0),
            price: GetEggPrice(isMagic, isLarge),
            contextTags: new List<string>(WildEggConstants.GetEggTags(isMagic, isLarge)),
            customFields: new Dictionary<string, string>
            {
                { "wildEggLocation", location.ToString() }
            }
        );

        objects[eggId] = eggData;
    }

    /// <summary>
    /// Edits the existing FarmWEgg (base game egg) to add wild egg context tags.
    /// </summary>
    public void EditWildEgg(IDictionary<string, ObjectData> objects)
    {
        const string farmEggId = "194"; // Farm W. Egg
        
        if (!objects.TryGetValue(farmEggId, out var farmEgg))
            return;

        // Add wild egg context tag
        farmEgg.ContextTags ??= new List<string>();
        if (!farmEgg.ContextTags.Contains(WildEggContextTag))
        {
            farmEgg.ContextTags.Add(WildEggContextTag);
        }

        // Update texture to use mod's egg texture
        farmEgg.Texture = GetModTexturePath(WildEggConstants.Textures.Eggs);
        farmEgg.SpriteIndex = 0;

        Log($"Edited FarmWEgg: {farmEggId}", LogLevel.Trace);
    }

    /// <summary>
    /// Changes the Elixir sprite index based on config.
    /// Call this at runtime when config changes.
    /// </summary>
    public void ChangeElixirSprite(IDictionary<string, ObjectData> objects, bool alternateSprite)
    {
        if (!objects.TryGetValue(WildEggConstants.Items.Elixir, out var elixir))
            return;

        elixir.SpriteIndex = alternateSprite ? 1 : 0;
        Log($"Changed Elixir sprite to {elixir.SpriteIndex}", LogLevel.Trace);
    }

    /// <summary>
    /// Adds the Osiris Scroll object using factory method.
    /// </summary>
    private void AddOsirisScroll(IDictionary<string, ObjectData> objects)
    {
        if (objects.ContainsKey(WildEggConstants.Items.OsirisScroll))
            return;

        var customFields = new Dictionary<string, string>
        {
            { PrismaticValleyColorKey, CustomPalette },
            { PrismaticValleyPaletteKey, " #FFC080, #E67E73, #342B1F" }
        };

        objects[WildEggConstants.Items.OsirisScroll] = CreateObjectData(
            description: Translate("object.OsirisScroll.description", "A mysterious scroll from the desert tomb..."),
            displayName: Translate("object.OsirisScroll.name", "Osiris Scroll"),
            name: WildEggConstants.Items.OsirisScroll,
            texturePath: WildEggConstants.Textures.Scroll,
            price: 0,
            contextTags: new List<string> { WildEggConstants.Tags.WildEggSpecial },
            customFields: customFields,
            type: "Basic",
            category: -8,
            edibility: -300
        );
    }

    /// <summary>
    /// Adds the Elixir object using factory method.
    /// </summary>
    private void AddElixir(IDictionary<string, ObjectData> objects)
    {
        if (objects.ContainsKey(WildEggConstants.Items.Elixir))
            return;

        // Use default sprite index - config-based changes happen at runtime
        bool alternateSprite = false;

        var customFields = new Dictionary<string, string>
        {
            { PrismaticValleyColorKey, CustomPalette },
            { PrismaticValleyPaletteKey, " #FFC080, #E67E73, #342B1F" }
        };

        objects[WildEggConstants.Items.Elixir] = CreateObjectData(
            description: Translate("object.ElixirOfRevitalizedEssence.description", "A powerful elixir that restores a large amount of energy and health."),
            displayName: Translate("object.ElixirOfRevitalizedEssence.name", "Elixir Of Revitalized Essence"),
            name: WildEggConstants.Items.Elixir,
            texturePath: WildEggConstants.Textures.ElixirTexture,
            spriteIndex: alternateSprite ? 1 : 0,
            price: 500,
            contextTags: new List<string> 
            { 
                WildEggConstants.Tags.WildEggSpecial, 
                WildEggConstants.Tags.ElixirItem 
            },
            customFields: customFields,
            type: "Drink",
            category: -7,
            edibility: 100,
            isDrink: true
        );
    }

    /// <summary>
    /// Creates a Junimo Note for a hatched animal.
    /// Used for Prismatic Valley Framework integration.
    /// </summary>
    public ObjectData CreateJunimoNote(string animalKey, string? displayName = null)
    {
        var name = displayName ?? animalKey;
        
        var customFields = new Dictionary<string, string>
        {
            { PrismaticValleyColorKey, CustomPalette },
            { PrismaticValleyPaletteKey, " #34C759, #8BC34A, #2E865F" },
            { "Animal", animalKey }
        };

        return CreateObjectData(
            description: Translate("object.JunimoNote.description", $"A note about your new {name}."),
            displayName: $"{Translate("object.JunimoNote.name", "Junimo Note")}: {name}",
            name: $"{WildEggConstants.ModId}_junimoNote_{animalKey.Replace(" ", "")}",
            texturePath: "Objects/Note",
            contextTags: new List<string> { WildEggConstants.Tags.WildEggSpecial },
            customFields: customFields
        );
    }

    #region Helper Methods

    private static string GetEggTypeKey(bool isMagic, bool isLarge)
    {
        if (isMagic && isLarge) return "largeMagicWildEgg";
        if (isMagic) return "magicWildEgg";
        if (isLarge) return "largeWildEgg";
        return "wildEgg";
    }

    private static string GetEggDisplayName(LocationType location, bool isMagic, bool isLarge)
    {
        string baseName = isMagic ? "Magic " : "";
        baseName += isLarge ? "Large Wild Egg" : "Wild Egg";
        return $"{baseName} ({location})";
    }

    private static string GetEggDescription(LocationType location, bool isMagic, bool isLarge)
    {
        if (isMagic)
            return $"A mysterious {(isLarge ? "large " : "")}egg from a magical creature. It hums with ancient energy.";
        return $"A wild {(isLarge ? "large " : "")}egg found in the {location}. It looks incubatable.";
    }

    private static int GetEggPrice(bool isMagic, bool isLarge)
    {
        if (isMagic && isLarge) return WildEggConstants.Prices.LargeMagicWildEgg;
        if (isMagic) return WildEggConstants.Prices.MagicWildEgg;
        if (isLarge) return WildEggConstants.Prices.LargeWildEgg;
        return WildEggConstants.Prices.WildEgg;
    }

    #endregion
}

