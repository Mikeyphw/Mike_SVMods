using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Machines;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure;

namespace WildEggsHatching.Infrastructure.Content;

/// <summary>
/// Base class for asset editors that provides factory methods for creating game data.
/// This is now a pure infrastructure class - no domain logic (progression, config) should be here.
/// The coordinator controls when editors are applied based on progression.
/// </summary>
public abstract class WildEggAssetEditor : IAssetEditor
{
    /// <summary>SMAPI monitor for logging.</summary>
    protected readonly IMonitor? Monitor;

    /// <summary>SMAPI helper for mod content.</summary>
    protected readonly IModHelper? ModHelper;

    /// <summary>Translation helper for i18n support.</summary>
    protected ITranslationHelper? I18n { get; private set; }

    /// <summary>
    /// Creates a new asset editor.
    /// </summary>
    protected WildEggAssetEditor() 
    {
        Monitor = null;
        ModHelper = null;
    }

    /// <summary>
    /// Creates a new asset editor with dependencies.
    /// </summary>
    protected WildEggAssetEditor(
        IMonitor? monitor = null,
        IModHelper? modHelper = null)
    {
        Monitor = monitor;
        ModHelper = modHelper;
        I18n = modHelper?.Translation;
    }

    /// <summary>
    /// The asset name this editor targets.
    /// </summary>
    public abstract string AssetName { get; }

    /// <summary>
    /// The edit priority (default is Normal).
    /// </summary>
    public virtual AssetEditPriority Priority => AssetEditPriority.Normal;

    /// <summary>
    /// Edits the asset data.
    /// </summary>
    public abstract void Edit<T>(IAssetData<T> asset, string assetName) where T : class;

    /// <summary>
    /// Logs a message if monitor is available.
    /// </summary>
    protected void Log(string message, LogLevel level = LogLevel.Debug)
    {
        Monitor?.Log(message, level);
    }

    /// <summary>
    /// Safely gets or adds a new entry to the dictionary.
    /// </summary>
    protected TValue GetOrAdd<TKey, TValue>(
        IDictionary<TKey, TValue> dict, 
        TKey key, 
        Func<TValue> factory)
    {
        if (dict.TryGetValue(key, out var existing))
            return existing;

        var value = factory();
        dict[key] = value;
        return value;
    }

    /// <summary>
    /// Safely gets a config value with fallback.
    /// </summary>
    protected static int GetConfigPrice(string? configValue, int defaultPrice)
    {
        if (string.IsNullOrEmpty(configValue))
            return defaultPrice;

        if (configValue.Equals("default", StringComparison.OrdinalIgnoreCase))
            return defaultPrice;

        return int.TryParse(configValue, out int price) ? price : defaultPrice;
    }

    #region Factory Methods

    /// <summary>
    /// Creates an ObjectData instance with i18n support.
    /// </summary>
    protected ObjectData CreateObjectData(
        string description,
        string displayName,
        string name,
        string texturePath,
        int spriteIndex = 0,
        int price = 0,
        List<string>? contextTags = null,
        Dictionary<string, string>? customFields = null,
        string type = "AnimalProduct",
        int category = -5,
        int edibility = 15,
        bool excludeFromRandomSale = true,
        bool isDrink = false)
    {
        return new ObjectData
        {
            Description = description,
            DisplayName = displayName,
            Name = name,
            Type = type,
            Category = category,
            Price = price,
            Texture = GetModTexturePath(texturePath),
            SpriteIndex = spriteIndex,
            Edibility = edibility,
            IsDrink = isDrink,
            ExcludeFromRandomSale = excludeFromRandomSale,
            ContextTags = contextTags ?? new List<string>(),
            CustomFields = customFields
        };
    }

    /// <summary>
    /// Creates a BigCraftableData instance with i18n support.
    /// </summary>
    protected BigCraftableData CreateBigCraftableData(
        string name,
        string displayName,
        string description,
        string texturePath,
        int spriteIndex = 0,
        bool canBePlacedIndoors = true,
        bool canBePlacedOutdoors = false,
        bool providesLight = false,
        int price = 0,
        string type = "Crafting",
        int category = -9)
    {
        return new BigCraftableData
        {
            Name = name,
            DisplayName = displayName,
            Description = description,
            Texture = GetModTexturePath(texturePath),
            SpriteIndex = spriteIndex,
            CanBePlacedIndoors = canBePlacedIndoors,
            CanBePlacedOutdoors = canBePlacedOutdoors,
            ProvidesLight = providesLight,
            Price = price,
            Type = type,
            Category = category
        };
    }

    /// <summary>
    /// Creates a MachineData instance with output rules.
    /// </summary>
    protected MachineData CreateMachineData(
        List<MachineOutputRule>? outputRules = null,
        List<MachineItemAdditionalConsumedItems>? consumedItems = null,
        string? invalidItemMessage = null,
        string? invalidItemMessageCondition = null,
        bool isIncubator = false,
        bool hasInput = false,
        bool hasOutput = false,
        float workingEffectChance = 0.33f,
        List<MachineEffects>? loadEffects = null,
        List<MachineEffects>? workingEffects = null)
    {
        return new MachineData
        {
            HasInput = hasInput,
            HasOutput = hasOutput,
            OutputRules = outputRules ?? new List<MachineOutputRule>(),
            AdditionalConsumedItems = consumedItems,
            InvalidItemMessage = invalidItemMessage,
            InvalidItemMessageCondition = invalidItemMessageCondition,
            LoadEffects = loadEffects ?? new List<MachineEffects>(),
            WorkingEffects = workingEffects ?? new List<MachineEffects>(),
            WorkingEffectChance = workingEffectChance,
            IsIncubator = isIncubator,
            AllowFairyDust = true,
            ShowNextIndexWhileWorking = true
        };
    }

    /// <summary>
    /// Creates a machine output rule for the incubator.
    /// </summary>
    protected MachineOutputRule CreateIncubatorOutputRule(
        string requiredItemId,
        bool incrementIndex = false)
    {
        const string outputMethod = "StardewValley.Object, Stardew Valley: OutputIncubator";
        
        return new MachineOutputRule
        {
            Id = requiredItemId,
            Triggers = new List<MachineOutputTriggerRule>
            {
                new()
                {
                    Id = $"ItemPlacedInMachine_{requiredItemId}",
                    Trigger = MachineOutputTrigger.ItemPlacedInMachine,
                    RequiredItemId = requiredItemId
                }
            },
            UseFirstValidOutput = true,
            OutputItem = new List<MachineItemOutput>
            {
                new()
                {
                    OutputMethod = outputMethod,
                    IncrementMachineParentSheetIndex = incrementIndex ? 1 : 0
                }
            }
        };
    }

    /// <summary>
    /// Creates additional consumed items for the incubator (e.g., ferns).
    /// </summary>
    protected MachineItemAdditionalConsumedItems CreateConsumedItem(
        string itemId,
        int requiredCount,
        string? invalidCountMessage = null)
    {
        return new MachineItemAdditionalConsumedItems
        {
            ItemId = itemId,
            RequiredCount = requiredCount,
            InvalidCountMessage = invalidCountMessage
        };
    }

    /// <summary>
    /// Creates a crafting recipe string in SMAPI format.
    /// Format: "ItemId Count .../Description/IsBigCraftable/UnlockConditions/"
    /// </summary>
    protected string CreateRecipeString(
        List<(string itemId, int quantity)> ingredients,
        (string outputId, int quantity) output,
        bool isBigCraftable = false,
        string? unlockConditions = null,
        string? description = null)
    {
        var ingredientsString = string.Join(
            ' ',
            ingredients.Select(x => $"{x.itemId} {x.quantity}")
        );
        
        var outputString = $"{output.outputId} {output.quantity}";
        var unlockConditionsString = string.IsNullOrEmpty(unlockConditions) 
            ? "none" 
            : unlockConditions;
        var descriptionString = description ?? "";

        return $"{ingredientsString}//{outputString}/{isBigCraftable}/{unlockConditionsString}/{descriptionString}";
    }

    /// <summary>
    /// Creates a mail string with attached item.
    /// </summary>
    protected string CreateMailWithItem(
        string text,
        string itemType,
        string itemId,
        bool autoAccept = true,
        string? mailName = null)
    {
        var name = mailName ?? itemId;
        return $"{text} %item {itemType} {itemId} {(autoAccept ? "true " : " ")}%% [#]{name}";
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the mod texture path with proper formatting.
    /// </summary>
    protected string GetModTexturePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "";
        
        // Already a full path
        if (path.StartsWith("Mods/") || path.Contains(':'))
            return path;
        
        return $"Mods/{WildEggConstants.ModId}/{path}";
    }

    /// <summary>
    /// Gets a translated string with fallback.
    /// </summary>
    protected string Translate(string key, string? fallback = null)
    {
        if (I18n == null)
            return fallback ?? key;
        
        return I18n.Get(key, fallback: fallback ?? key);
    }

    /// <summary>
    /// Creates machine load effects (sounds when loading).
    /// </summary>
    protected List<MachineEffects> CreateLoadEffects(
        string soundId = "trashcan",
        int interval = 100,
        int shakeDuration = -1)
    {
        return new List<MachineEffects>
        {
            new()
            {
                Id = "Default",
                Condition = null,
                Sounds = new List<MachineSoundData>
                {
                    new() { Id = soundId, Delay = 0 }
                },
                Interval = interval,
                ShakeDuration = shakeDuration
            }
        };
    }

    /// <summary>
    /// Creates machine working effects (sounds and animations while working).
    /// </summary>
    protected List<MachineEffects> CreateWorkingEffects(
        string soundId = "trashcan",
        int interval = 100,
        int shakeDuration = -1,
        string? animationTexture = null,
        Rectangle? animationSourceRect = null,
        float animationScale = 0.25f)
    {
        var effects = new List<MachineEffects>
        {
            new()
            {
                Id = "Default",
                Condition = null,
                Sounds = new List<MachineSoundData>
                {
                    new() { Id = soundId, Delay = 0 }
                },
                Interval = interval,
                ShakeDuration = shakeDuration
            }
        };

        // Add animation if provided
        if (animationTexture != null && animationSourceRect.HasValue)
        {
            effects[0].TemporarySprites = new List<TemporaryAnimatedSpriteDefinition>
            {
                new()
                {
                    Id = "Default",
                    Texture = animationTexture,
                    SourceRect = animationSourceRect.Value,
                    Interval = 20f,
                    Frames = 6,
                    Loops = 999999,
                    PositionOffset = new Vector2(0, -32),
                    Flicker = false,
                    Scale = animationScale,
                    AlphaFade = 0.005f
                }
            };
        }

        return effects;
    }

    /// <summary>
    /// Modifies a crafting recipe field using DataFieldsEditor.
    /// Note: Requires BirdsOfAFeather.DataFieldsEditor to be available.
    /// </summary>
    protected string? ModifyRecipeField(string recipeString, int fieldIndex, object value)
    {
        try
        {
            // This would use DataFieldsEditor.ModifyData from BirdsOfAFeather
            // For now, return null to indicate it's not implemented
            Log("ModifyRecipeField requires BirdsOfAFeather.DataFieldsEditor", LogLevel.Warn);
            return null;
        }
        catch (Exception ex)
        {
            Log($"Error modifying recipe field: {ex.Message}", LogLevel.Error);
            return null;
        }
    }

    /// <summary>
    /// Patches an image asset (for sprite editing).
    /// </summary>
    protected void PatchImage(
        IAssetDataForImage editor,
        IRawTextureData sourceImage,
        Rectangle? sourceArea = null,
        Rectangle? targetArea = null)
    {
        if (sourceArea.HasValue && targetArea.HasValue)
        {
            editor.PatchImage(sourceImage, sourceArea.Value, targetArea.Value);
        }
        else if (sourceArea.HasValue)
        {
            editor.PatchImage(sourceImage, sourceArea.Value);
        }
        else
        {
            editor.PatchImage(sourceImage);
        }
    }

    /// <summary>
    /// Loads a raw texture from mod content.
    /// </summary>
    protected IRawTextureData? LoadModTexture(string path)
    {
        try
        {
            return ModHelper?.ModContent.Load<IRawTextureData>(path);
        }
        catch (Exception ex)
        {
            Log($"Error loading texture {path}: {ex.Message}", LogLevel.Error);
            return null;
        }
    }

    #endregion
}

