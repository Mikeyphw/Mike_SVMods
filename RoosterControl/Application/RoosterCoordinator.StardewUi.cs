using Mikew.NestCore.Integrations.StardewUI;
using Mikew.NestCore.Integrations.StardewUI.Dsl;
using RoosterControl.Enums;
using RoosterControl.Integrations.StardewUi.Compat;
using RoosterControl.Integrations.StardewUi;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using System;
using System.Collections.Generic;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
    private const string WhiteProduceEditorMenuId = "rooster.white.produceEditor";
    private const string WhiteProduceEditorViewFile = "RoosterWhiteProduceEditor";
    private const string CompatEditorMenuId = "rooster.compat.editor";
    private const string CompatEditorViewFile = "RoosterCompatEditor";

    private StardewUiHost? _stardewUiHost;
    private StardewUI.Framework.IMenuController? _whiteProduceEditorController;
    private StardewUI.Framework.IMenuController? _compatEditorController;
    private bool _stardewUiLayoutInitialized;

    public void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (Game1.activeClickableMenu is not null)
            return;

        if (!Config.General.OpenWhiteRoosterProduceEditorKey.JustPressed())
            return;

        TryOpenWhiteRoosterProduceEditor();
    }

    private void TryOpenWhiteRoosterProduceEditor()
    {
        _stardewUiHost ??= StardewUiHost.TryCreate(Helper, ModManifest);
        if (_stardewUiHost is null)
        {
            Monitor.Log("StardewUI isn't installed; White rooster produce editor unavailable.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        EnsureStardewUiLayoutInitialized();

        if (_whiteProduceEditorController is not null)
            return;

        var controller = _stardewUiHost.CreateMenuController(WhiteProduceEditorMenuId);
        if (controller is null)
        {
            Monitor.Log("Failed to create White rooster produce editor menu controller.", StardewModdingAPI.LogLevel.Error);
            return;
        }

        _whiteProduceEditorController = controller;
        Game1.activeClickableMenu = controller.Menu;
    }

    internal bool HasStardewUiAvailable()
        => StardewUiHost.TryCreate(Helper, ModManifest) is not null;

    internal string GetCompatEditorAvailabilityText()
        => HasStardewUiAvailable()
            ? "Duplicate StardewUI compat editor is available."
            : "Duplicate StardewUI compat editor requires StardewUI.";

    internal void TryOpenCompatEditor()
    {
        if (!Context.IsWorldReady)
            return;

        _stardewUiHost ??= StardewUiHost.TryCreate(Helper, ModManifest);
        if (_stardewUiHost is null)
        {
            Monitor.Log("StardewUI isn't installed; compat editor unavailable.", StardewModdingAPI.LogLevel.Warn);
            return;
        }

        EnsureCompatInitialized();
        EnsureRoostersSetup();
        EnsureStardewUiLayoutInitialized();

        if (_compatEditorController is not null)
            return;

        var controller = _stardewUiHost.CreateMenuController(CompatEditorMenuId);
        if (controller is null)
        {
            Monitor.Log("Failed to create compat editor menu controller.", StardewModdingAPI.LogLevel.Error);
            return;
        }

        _compatEditorController = controller;
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
                    WhiteProduceEditorMenuId,
                    menu =>
                    {
                        var viewName = $"Mods/{ModManifest.UniqueID}/Views/{WhiteProduceEditorViewFile}";
                        menu.View(viewName);
                        menu.Context(() => new WhiteRoosterProduceEditorViewModel(Config.Roosters[RoosterType.White]));
                        menu.ConfigureController(controller =>
                        {
                            controller.DimmingAmount = 0.6f;
                            controller.CloseOnOutsideClick = true;
                            controller.EnableCloseButton();
                        });
                        menu.OnClosed(() =>
                        {
                            _whiteProduceEditorController = null;
                            Helper.WriteConfig(Config);
                        });
                    });

                layout.Menu(
                    CompatEditorMenuId,
                    menu =>
                    {
                        var viewName = $"Mods/{ModManifest.UniqueID}/Views/{CompatEditorViewFile}";
                        menu.View(viewName);
                        menu.Context(() => new RoosterCompatEditorViewModel(
                            GetCompatEditorService(),
                            _saveState.Compat,
                            LoadFarmAnimalsForCompatEditor,
                            () => PersistAndApplyCompatChange("stardewui compat editor save")));
                        menu.ConfigureController(controller =>
                        {
                            controller.DimmingAmount = 0.6f;
                            controller.CloseOnOutsideClick = true;
                            controller.EnableCloseButton();
                        });
                        menu.OnClosed(() =>
                        {
                            _compatEditorController = null;
                        });
                    });
            },
            assets: new StardewUiAssetsOptions(ViewsDir: "assets/views", SpritesDir: "assets/sprites"),
            preloadAssetsOnce: true,
            preloadModels: new[] { typeof(WhiteRoosterProduceEditorViewModel), typeof(RoosterCompatEditorViewModel) });
    }

    private IDictionary<string, FarmAnimalData> LoadFarmAnimalsForCompatEditor()
    {
        if (TryLoadFarmAnimals(out var farmAnimals, out _))
            return farmAnimals;

        return new Dictionary<string, FarmAnimalData>(StringComparer.OrdinalIgnoreCase);
    }
}
