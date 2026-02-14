using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using WildEggsHatching.Core.Enums;
using WildEggsHatching.Infrastructure.Content;
using WildEggsHatching.Infrastructure.Content.Editors;

namespace WildEggsHatching.Infrastructure;

/// <summary>
/// Coordinates content asset editing through SMAPI's content pipeline.
/// Acts as a pure router to IAssetEditor implementations.
/// Progression gating is done here - editors don't need to know about progression.
/// </summary>
public sealed class SmapiContentCoordinator : IDisposable
{
    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;
    private readonly Func<ProgressionStep> _getCurrentStep;
    
    /// <summary>Registered asset editors with their activation steps.</summary>
    private readonly List<EditorRegistration> _editorRegistrations;

    /// <summary>Tracks if we've registered editors for the current save.</summary>
    private bool _editorsRegistered;

    /// <summary>Helper struct to track editor + activation step.</summary>
    private readonly struct EditorRegistration
    {
        public IAssetEditor Editor { get; }
        public ProgressionStep ActivationStep { get; }

        public EditorRegistration(IAssetEditor editor, ProgressionStep activationStep)
        {
            Editor = editor;
            ActivationStep = activationStep;
        }
    }

    /// <summary>
    /// Creates a new content coordinator.
    /// </summary>
    /// <param name="helper">The SMAPI mod helper.</param>
    /// <param name="monitor">The SMAPI monitor.</param>
    /// <param name="getCurrentStep">Function to get current progression step.</param>
    public SmapiContentCoordinator(
        IModHelper helper,
        IMonitor monitor,
        Func<ProgressionStep> getCurrentStep)
    {
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _getCurrentStep = getCurrentStep ?? throw new ArgumentNullException(nameof(getCurrentStep));
        _editorRegistrations = new List<EditorRegistration>();
    }

    /// <summary>
    /// Registers all content editors with SMAPI.
    /// Should be called once at mod initialization.
    /// </summary>
    public void RegisterEditors()
    {
        if (_editorsRegistered)
            return;

        // Create editors - no progression service needed, just the base editor
        var editors = new (IAssetEditor editor, ProgressionStep activationStep)[]
        {
            // Objects - core wild eggs (Part I+)
            (new ObjectsEditor(), ProgressionStep.PartI),
            
            // Farm Animals - prices and eggs (Part I+)
            (new FarmAnimalsEditor(), ProgressionStep.PartI),
            
            // Mail - progression letters (Part II+)
            (new MailEditor(), ProgressionStep.PartII),
            
            // Quests - progression quests (Part I+)
            (new QuestEditor(), ProgressionStep.PartI),
            
            // Special Orders - progression orders (Part I+, Part IV+)
            (new SpecialOrdersEditor(), ProgressionStep.PartI),
            
            // Big Craftables - incubator (Part V+)
            (new BigCraftablesEditor(), ProgressionStep.PartV),
            
            // Crafting Recipes - incubator recipe (Part V+)
            (new CraftingRecipesEditor(), ProgressionStep.PartV),
            
            // Machines - output rules (Part I+)
            (new MachinesEditor(), ProgressionStep.PartI),
        };

        // Sort by priority and register
        foreach (var (editor, activationStep) in editors.OrderBy(e => e.editor.Priority))
        {
            _editorRegistrations.Add(new EditorRegistration(editor, activationStep));
            RegisterEditor(editor);
        }

        _editorsRegistered = true;
        _monitor.Log($"Registered {_editorRegistrations.Count} content editors.", LogLevel.Debug);
    }

    /// <summary>
    /// Registers a single editor with SMAPI.
    /// </summary>
    private void RegisterEditor(IAssetEditor editor)
    {
        var assetName = editor.AssetName;
        var priority = editor.Priority;

        _helper.Content.AssetEditor.Register(assetName, priority, OnAssetRequested);
        
        void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            // Check if this editor handles this asset
            if (!e.NameWithoutLocale.IsEquivalentTo(assetName))
                return;

            // Get current progression step
            var currentStep = _getCurrentStep();
            
            // Find this editor's activation step
            var registration = _editorRegistrations.FirstOrDefault(r => r.Editor == editor);
            
            // Check if we've reached the activation step
            if (currentStep < registration.ActivationStep)
                return;

            _monitor.Log($"Applying editor {editor.GetType().Name} to {assetName}", LogLevel.Trace);

            // Edit the asset using proper SMAPI API
            e.Edit(
                asset =>
                {
                    // Use AsDictionary for dictionary-based assets
                    if (asset.Data is IDictionary<string, object> dict)
                    {
                        // Handle based on asset type
                        if (assetName == GameAssets.Objects)
                        {
                            var data = asset.AsDictionary<string, StardewValley.GameData.Objects.ObjectData>().Data;
                            editor.Edit(asset, assetName);
                        }
                        else if (assetName == GameAssets.FarmAnimals)
                        {
                            var data = asset.AsDictionary<string, StardewValley.GameData.FarmAnimalData>().Data;
                            editor.Edit(asset, assetName);
                        }
                        else if (assetName == GameAssets.CraftingRecipes)
                        {
                            var data = asset.AsDictionary<string, string>().Data;
                            editor.Edit(asset, assetName);
                        }
                        else if (assetName == GameAssets.Mail)
                        {
                            var data = asset.AsDictionary<string, string>().Data;
                            editor.Edit(asset, assetName);
                        }
                        else if (assetName == GameAssets.SpecialOrders)
                        {
                            var data = asset.AsDictionary<string, StardewValley.GameData.SpecialOrderData>().Data;
                            editor.Edit(asset, assetName);
                        }
                        else if (assetName == GameAssets.Quests)
                        {
                            var data = asset.AsDictionary<string, StardewValley.GameData.QuestData>().Data;
                            editor.Edit(asset, assetName);
                        }
                        else if (assetName == GameAssets.BigCraftables)
                        {
                            var data = asset.AsDictionary<string, StardewValley.GameData.BigCraftableData>().Data;
                            editor.Edit(asset, assetName);
                        }
                        else if (assetName == GameAssets.MachineOutput)
                        {
                            var data = asset.AsDictionary<string, StardewValley.GameData.MachineData>().Data;
                            editor.Edit(asset, assetName);
                        }
                        else
                        {
                            // Fallback for unknown asset types
                            editor.Edit(asset, assetName);
                        }
                    }
                    else
                    {
                        // Non-dictionary asset
                        editor.Edit(asset, assetName);
                    }
                },
                priority
            );
        }

        _monitor.Log($"Registered editor for: {assetName}", LogLevel.Debug);
    }

    /// <summary>
    /// Invalidates all content assets - should be called when progression changes.
    /// </summary>
    public void InvalidateAssets()
    {
        var assetsToInvalidate = new[]
        {
            GameAssets.FarmAnimals,
            GameAssets.Objects,
            GameAssets.CraftingRecipes,
            GameAssets.Mail,
            GameAssets.SpecialOrders,
            GameAssets.Quests,
            GameAssets.BigCraftables,
            GameAssets.MachineOutput,
        };

        foreach (var asset in assetsToInvalidate)
        {
            _helper.GameContent.InvalidateCache(asset);
        }

        _monitor.Log("Invalidated all content assets due to progression change.", LogLevel.Info);
    }

    /// <summary>
    /// Invalidates a specific asset.
    /// </summary>
    public void InvalidateAsset(string assetName)
    {
        _helper.GameContent.InvalidateCache(assetName);
        _monitor.Log($"Invalidated asset: {assetName}", LogLevel.Debug);
    }

    /// <summary>
    /// Gets all registered editors.
    /// </summary>
    public IReadOnlyList<IAssetEditor> GetEditors() => 
        _editorRegistrations.Select(r => r.Editor).ToList().AsReadOnly();

    /// <summary>
    /// Disposes the coordinator.
    /// </summary>
    public void Dispose()
    {
        _editorRegistrations.Clear();
    }
}

