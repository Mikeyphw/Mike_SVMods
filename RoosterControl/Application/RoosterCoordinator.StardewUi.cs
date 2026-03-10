using Mikew.NestCore.Integrations.StardewUI;
using Mikew.NestCore.Integrations.StardewUI.Dsl;
using RoosterControl.Application.Hatching;
using RoosterControl.Enums;
using RoosterControl.Framework.Config;
using RoosterControl.Integrations.StardewUi;
using RoosterControl.Integrations.StardewUi.Compat;
using RoosterControl.Integrations.StardewUi.Produce;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Objects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
    private const string EditorHubMenuId = "rooster.editorHub";
    private const string EditorHubViewFile = "RoosterEditorHub";

    private StardewUiHost? _stardewUiHost;
    private StardewUI.Framework.IMenuController? _editorHubController;
    private bool _stardewUiLayoutInitialized;
    private RoosterEditorHubTab _requestedHubTab = RoosterEditorHubTab.Compat;
    private RoosterType _requestedProduceRoosterType = RoosterType.White;
    private RoosterProduceSection _requestedProduceSection = RoosterProduceSection.Normal;

    public void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (Game1.activeClickableMenu is not null)
            return;

        if (Config.General.OpenWhiteRoosterProduceEditorKey.JustPressed())
        {
            TryOpenWhiteRoosterProduceEditor();
            return;
        }

        if (Config.General.OpenCompatEditorKey.JustPressed())
        {
            TryOpenCompatEditor();
            return;
        }

    }

    private void TryOpenWhiteRoosterProduceEditor()
        => TryOpenEditorHub(RoosterEditorHubTab.Produce, RoosterType.White, RoosterProduceSection.Normal);

    internal bool HasStardewUiAvailable()
        => StardewUiHost.TryCreate(Helper, ModManifest) is not null;

    internal string GetCompatEditorAvailabilityText()
        => HasStardewUiAvailable()
            ? "StardewUI editor hub is available and can open the Compat tab."
            : "StardewUI editor hub requires StardewUI.";

    internal string GetProduceEditorAvailabilityText()
        => HasStardewUiAvailable()
            ? "StardewUI editor hub is available and can open the Produce tab."
            : "StardewUI editor hub requires StardewUI.";

    internal void TryOpenCompatEditor()
        => TryOpenEditorHub(RoosterEditorHubTab.Compat, RoosterType.White, RoosterProduceSection.Normal);

    internal void TryOpenProduceEditor()
        => TryOpenEditorHub(RoosterEditorHubTab.Produce, RoosterType.White, RoosterProduceSection.Normal);

    internal void TryOpenSettingsEditor()
        => TryOpenEditorHub(RoosterEditorHubTab.Settings, RoosterType.White, RoosterProduceSection.Normal);

    private void TryOpenEditorHub(RoosterEditorHubTab tab, RoosterType produceRoosterType, RoosterProduceSection produceSection)
    {
        if (!Context.IsWorldReady)
            return;

        _stardewUiHost ??= StardewUiHost.TryCreate(Helper, ModManifest);
        if (_stardewUiHost is null)
        {
            Monitor.Log("StardewUI isn't installed; RoosterControl editor hub unavailable.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        EnsureCompatInitialized();
        EnsureRoostersSetup();
        EnsureStardewUiLayoutInitialized();

        _requestedHubTab = tab;
        _requestedProduceRoosterType = produceRoosterType;
        _requestedProduceSection = produceSection;

        if (_editorHubController is not null)
            return;

        var controller = _stardewUiHost.CreateMenuController(EditorHubMenuId);
        if (controller is null)
        {
            Monitor.Log("Failed to create RoosterControl editor hub menu controller.", StardewModdingAPI.LogLevel.Error);
            return;
        }

        _editorHubController = controller;
        Game1.activeClickableMenu = controller.Menu;
    }

    private void EnsureStardewUiLayoutInitialized()
    {
        if (_stardewUiHost is null || _stardewUiLayoutInitialized)
            return;

        _stardewUiLayoutInitialized = true;

        _stardewUiHost.RebuildLayout(
            define: layout =>
            {
                layout.Menu(
                    EditorHubMenuId,
                    menu =>
                    {
                        var viewName = $"Mods/{ModManifest.UniqueID}/Views/{EditorHubViewFile}";
                        menu.View(viewName);
                        menu.Context(CreateEditorHubViewModel);
                        menu.ConfigureController(controller =>
                        {
                            controller.DimmingAmount = 0.6f;
                            controller.CloseOnOutsideClick = true;
                            controller.EnableCloseButton();
                        });
                        menu.OnClosed(() =>
                        {
                            _editorHubController = null;
                        });
                    });
            },
            assets: new StardewUiAssetsOptions(ViewsDir: "assets/views", SpritesDir: "assets/sprites"),
            preloadAssetsOnce: true,
            preloadModels: new[]
            {
                typeof(RoosterEditorHubViewModel),
                typeof(RoosterCompatEditorViewModel),
                typeof(RoosterProduceEditorViewModel),
                typeof(RoosterSettingsEditorViewModel),
                typeof(RoosterDebugEditorViewModel),
            });
    }

    private RoosterEditorHubViewModel CreateEditorHubViewModel()
        => new(
            new RoosterCompatEditorViewModel(
                GetCompatEditorService(),
                _saveState.Compat,
                LoadFarmAnimalsForCompatEditor,
                () => PersistAndApplyCompatChange("stardewui compat editor save")),
            new RoosterProduceEditorViewModel(
                new RoosterProduceEditorService(),
                Config,
                () => PersistAndApplyProduceChange("stardewui produce editor save"),
                _requestedProduceRoosterType,
                _requestedProduceSection,
                TryLoadFarmAnimalPreviewSprite,
                TryLoadObjectPreviewSprite),
            new RoosterSettingsEditorViewModel(
                Config,
                () => PersistAndApplyProduceChange("stardewui settings editor save"),
                GetCompatEditorAvailabilityText,
                GetProduceEditorAvailabilityText),
            new RoosterDebugEditorViewModel(
                Config,
                () => PersistAndApplyProduceChange("stardewui debug editor save")),
            _requestedHubTab);

    private IDictionary<string, FarmAnimalData> LoadFarmAnimalsForCompatEditor()
    {
        if (TryLoadFarmAnimals(out var farmAnimals, out _))
            return farmAnimals;

        return new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase);
    }

    private void PersistAndApplyProduceChange(string reason)
    {
        ConfigManager.Save(Helper, Config);
        InvalidateDailyPlanCache(reason);
        _needsRoosterSetup = true;
        EnsureRoostersSetup();
        RequestInvalidate(FarmAnimalsAssetName);
        RequestInvalidate(ObjectsAssetName);
    }

    private Tuple<Texture2D, Rectangle>? TryLoadFarmAnimalPreviewSprite(string animalKey)
    {
        try
        {
            animalKey = animalKey switch
            {
                nameof(RoosterType.White) => "White Chicken",
                nameof(RoosterType.Brown) => "Brown Chicken",
                nameof(RoosterType.Blue) => "Blue Chicken",
                nameof(RoosterType.Void) => "Void Chicken",
                nameof(RoosterType.Gold) => "Golden Chicken",
                _ => animalKey,
            };

            if (!Game1.farmAnimalData.TryGetValue(animalKey, out var data))
                return null;

            if (string.IsNullOrWhiteSpace(data.Texture) || data.SpriteWidth <= 0 || data.SpriteHeight <= 0)
                return null;

            var texture = Game1.content.Load<Texture2D>(data.Texture);
            return Tuple.Create(texture, new Rectangle(0, 0, data.SpriteWidth, data.SpriteHeight));
        }
        catch
        {
            return null;
        }
    }

    private Tuple<Texture2D, Rectangle>? TryLoadObjectPreviewSprite(string itemId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            var objects = Helper.GameContent.Load<Dictionary<string, ObjectData>>("Data/Objects");
            if (!objects.TryGetValue(itemId, out var data))
                return null;

            var textureName = string.IsNullOrWhiteSpace(data.Texture) ? "Maps/springobjects" : data.Texture;
            var texture = Game1.content.Load<Texture2D>(textureName);
            var columns = Math.Max(1, texture.Width / 16);
            var spriteIndex = Math.Max(0, data.SpriteIndex);
            var rect = new Rectangle((spriteIndex % columns) * 16, (spriteIndex / columns) * 16, 16, 16);
            return Tuple.Create(texture, rect);
        }
        catch
        {
            return null;
        }
    }
}
