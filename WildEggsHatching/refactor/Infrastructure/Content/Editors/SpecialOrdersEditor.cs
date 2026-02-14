using System.Collections.Generic;
using StardewModdingAPI.Events;
using StardewValley.GameData.SpecialOrders;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Content;

namespace WildEggsHatching.Infrastructure.Content.Editors;

/// <summary>
/// Editor for Data/SpecialOrders asset - adds progression special orders.
/// Refactored to use WildEggConstants.
/// </summary>
public sealed class SpecialOrdersEditor : ProgressionAwareAssetEditor
{
    /// <inheritdoc />
    public override string AssetName => GameAssets.SpecialOrders;

    /// <inheritdoc />
    public SpecialOrdersEditor() : base() { }

    /// <inheritdoc />
    public SpecialOrdersEditor(
        PhaseProgressionService progressionService,
        IConfigProvider configProvider,
        IMonitor monitor) 
        : base(progressionService, configProvider, monitor) { }

    /// <inheritdoc />
    public override void Edit<T>(IAssetData<T> asset, string assetName)
    {
        if (assetName != GameAssets.SpecialOrders)
            return;

        var orders = asset.AsDictionary<string, SpecialOrderData>().Data;

        // Part I: Special Order I
        if (HasReachedStep(ProgressionStep.PartI))
        {
            AddOrder(orders, WildEggConstants.SpecialOrderIds.WildEggsI, CreateSpecialOrderI());
        }

        // Part IV: Special Order II
        if (HasReachedStep(ProgressionStep.PartIV))
        {
            AddOrder(orders, WildEggConstants.SpecialOrderIds.WildEggsII, CreateSpecialOrderII());
        }

        Log($"SpecialOrdersEditor applied edits to {orders.Count} orders");
    }

    /// <summary>
    /// Safely adds a special order.
    /// </summary>
    private void AddOrder(IDictionary<string, SpecialOrderData> orders, string id, SpecialOrderData data)
    {
        if (!orders.ContainsKey(id))
        {
            orders[id] = data;
            Log($"Added special order: {id}", LogLevel.Trace);
        }
    }

    #region Special Order Factory Methods

    private static SpecialOrderData CreateSpecialOrderI() => new()
    {
        OrderId = WildEggConstants.SpecialOrderIds.WildEggsI,
        Name = "Wild Egg Research: Phase I",
        Description = "Help the Wizard study wild eggs by collecting samples from around the valley.",
        Requester = "Wizard",
        Duration = 7,
        RequiredItems = new List<SpecialOrderReqItem>
        {
            new() 
            { 
                ItemId = WildEggConstants.CreateEggId("Forest", isMagic: false, isLarge: false), 
                Amount = 5 
            }
        },
        Rewards = new List<SpecialOrderReward>
        {
            new() { Amount = 1000, Type = "Money" }
        }
    };

    private static SpecialOrderData CreateSpecialOrderII() => new()
    {
        OrderId = WildEggConstants.SpecialOrderIds.WildEggsII,
        Name = "Wild Egg Research: Phase II",
        Description = "Continue your research with more advanced analysis of wild eggs.",
        Requester = "Wizard",
        Duration = 7,
        RequiredItems = new List<SpecialOrderReqItem>
        {
            new() 
            { 
                ItemId = WildEggConstants.CreateEggId("Forest", isMagic: true, isLarge: false), 
                Amount = 3 
            },
            new() 
            { 
                ItemId = WildEggConstants.CreateEggId("Forest", isMagic: false, isLarge: true), 
                Amount = 3 
            }
        },
        Rewards = new List<SpecialOrderReward>
        {
            new() { Amount = 2000, Type = "Money" }
        }
    };

    #endregion

    /// <inheritdoc />
    public override bool ShouldApply() => true;
}
