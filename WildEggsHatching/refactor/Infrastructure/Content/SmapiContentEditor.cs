using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData;
using StardewValley.ItemTypeDefinitions;
using WildEggsHatching.Core.Models;

namespace WildEggsHatching.Infrastructure.Content;

/// <summary>
/// Handles all content asset editing through SMAPI's content pipeline.
/// This is the infrastructure boundary for asset modifications.
/// </summary>
public sealed class SmapiContentEditor
{
    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;

    // Asset edit handlers registered by domain services
    private readonly Dictionary<string, Action<AssetRequestedEventArgs>> _assetHandlers;

    /// <summary>
    /// Creates a new content editor.
    /// </summary>
    /// <param name="helper">The SMAPI mod helper.</param>
    /// <param name="monitor">The SMAPI monitor.</param>
    public SmapiContentEditor(IModHelper helper, IMonitor monitor)
    {
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _assetHandlers = new Dictionary<string, Action<AssetRequestedEventArgs>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Registers an asset editor for a specific asset.
    /// </summary>
    /// <param name="assetName">The asset name to handle (e.g., "Data/FarmAnimals").</param>
    /// <param name="priority">The edit priority.</param>
    /// <param name="editor">The editor action.</param>
    public void RegisterAssetEditor(string assetName, AssetEditPriority priority, Action<AssetRequestedEventArgs> editor)
    {
        var key = assetName.ToLowerInvariant();
        _assetHandlers[key] = editor;

        _monitor.Log($"Registered asset editor for: {assetName}", LogLevel.Debug);
    }

    /// <summary>
    /// Unregisters an asset editor.
    /// </summary>
    /// <param name="assetName">The asset name to unregister.</param>
    public void UnregisterAssetEditor(string assetName)
    {
        var key = assetName.ToLowerInvariant();
        _assetHandlers.Remove(key);
    }

    /// <summary>
    /// Edits the farm animals data asset.
    /// </summary>
    public void EditFarmAnimals(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/FarmAnimals"))
            return;

        e.Edit(
            asset =>
            {
                var farmAnimals = asset.AsDictionary<string, FarmAnimalData>().Data;

                // Add wild egg variants or modify existing animals
                // This is a placeholder - actual implementation would modify based on progression
            },
            AssetEditPriority.Low
        );
    }

    /// <summary>
    /// Edits the objects data asset to add wild eggs.
    /// </summary>
    public void EditObjects(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            return;

        e.Edit(
            asset =>
            {
                var objects = asset.AsDictionary<string, ObjectData>().Data;

                // Add wild egg object definitions
                // This is a placeholder for actual implementation
            },
            AssetEditPriority.Default
        );
    }

    /// <summary>
    /// Edits the crafting recipes asset.
    /// </summary>
    public void EditCraftingRecipes(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            return;

        e.Edit(
            asset =>
            {
                var recipes = asset.AsDictionary<string, string>().Data;

                // Add or modify crafting recipes for wild eggs
            },
            AssetEditPriority.Default
        );
    }

    /// <summary>
    /// Edits the mail asset.
    /// </summary>
    public void EditMail(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Mail"))
            return;

        e.Edit(
            asset =>
            {
                var mail = asset.AsDictionary<string, string>().Data;

                // Add mail letters for wild egg progression
            },
            AssetEditPriority.Default
        );
    }

    /// <summary>
    /// Edits the special orders asset.
    /// </summary>
    public void EditSpecialOrders(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/SpecialOrders"))
            return;

        e.Edit(
            asset =>
            {
                var specialOrders = asset.AsDictionary<string, SpecialOrderData>().Data;

                // Add special orders for wild egg quests
            },
            AssetEditPriority.Default
        );
    }

    /// <summary>
    /// Edits the quests asset.
    /// </summary>
    public void EditQuests(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/Quests"))
            return;

        e.Edit(
            asset =>
            {
                var quests = asset.AsDictionary<string, QuestData>().Data;

                // Add quests for wild egg progression
            },
            AssetEditPriority.Default
        );
    }

    /// <summary>
    /// Edits the big craftables asset.
    /// </summary>
    public void EditBigCraftables(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/BigCraftables"))
            return;

        e.Edit(
            asset =>
            {
                var bigCraftables = asset.AsDictionary<string, BigCraftableData>().Data;

                // Add wild egg incubator and other craftables
            },
            AssetEditPriority.Default
        );
    }

    /// <summary>
    /// Edits the machines output rules.
    /// </summary>
    public void EditMachines(AssetRequestedEventArgs e)
    {
        if (!e.NameWithoutLocale.IsEquivalentTo("Data/MachineOutputData"))
            return;

        e.Edit(
            asset =>
            {
                var machines = asset.AsDictionary<string, MachineData>().Data;

                // Add machine output rules for eggs
            },
            AssetEditPriority.Default
        );
    }

    /// <summary>
    /// Invalidates a cached asset so it reloads.
    /// </summary>
    /// <param name="assetName">The asset name to invalidate.</param>
    public void InvalidateAsset(string assetName)
    {
        _helper.GameContent.InvalidateCache(assetName);
    }

    /// <summary>
    /// Invalidates multiple cached assets.
    /// </summary>
    /// <param name="assetNames">The asset names to invalidate.</param>
    public void InvalidateAssets(IEnumerable<string> assetNames)
    {
        foreach (var assetName in assetNames)
        {
            InvalidateAsset(assetName);
        }
    }

    /// <summary>
    /// Handles an asset request by forwarding to registered editors.
    /// This is called by ModEntry when SMAPI requests an asset.
    /// </summary>
    /// <param name="e">The asset request event args.</param>
    public void OnAssetRequested(AssetRequestedEventArgs e)
    {
        var assetNameLower = e.NameWithoutLocale.Name.ToLowerInvariant();

        // Check if we have a registered handler for this asset
        if (_assetHandlers.TryGetValue(assetNameLower, out var handler))
        {
            try
            {
                _monitor.Log($"Handling asset request: {e.NameWithoutLocale.Name}", LogLevel.Debug);
                handler(e);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error handling asset {e.NameWithoutLocale.Name}: {ex.Message}", LogLevel.Error);
            }
            return;
        }

        // Also check without locale for partial matches
        foreach (var kvp in _assetHandlers)
        {
            if (assetNameLower.Contains(kvp.Key) || kvp.Key.Contains(assetNameLower))
            {
                try
                {
                    _monitor.Log($"Handling asset request (partial match): {e.NameWithoutLocale.Name}", LogLevel.Debug);
                    kvp.Value(e);
                }
                catch (Exception ex)
                {
                    _monitor.Log($"Error handling asset {e.NameWithoutLocale.Name}: {ex.Message}", LogLevel.Error);
                }
                return;
            }
        }
    }

    /// <summary>
    /// Gets the registered asset editor for a specific asset.
    /// Returns null if not found.
    /// </summary>
    public Action<AssetRequestedEventArgs>? GetAssetEditor(string assetName)
    {
        var key = assetName.ToLowerInvariant();
        return _assetHandlers.TryGetValue(key, out var handler) ? handler : null;
    }

    /// <summary>
    /// Gets all registered asset names.
    /// </summary>
    public IReadOnlyCollection<string> GetRegisteredAssets()
    {
        return _assetHandlers.Keys.ToList().AsReadOnly();
    }
}

