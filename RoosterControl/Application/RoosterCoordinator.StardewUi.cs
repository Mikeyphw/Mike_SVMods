using Mikew.NestCore.Integrations.StardewUI;
using Mikew.NestCore.Integrations.StardewUI.Dsl;
using RoosterControl.Enums;
using RoosterControl.Integrations.StardewUi;
using StardewModdingAPI.Events;
using StardewValley;

namespace RoosterControl.Application;

internal sealed partial class RoosterCoordinator
{
    private const string WhiteProduceEditorMenuId = "rooster.white.produceEditor";
    private const string WhiteProduceEditorViewFile = "RoosterWhiteProduceEditor";

    private StardewUiHost? _stardewUiHost;
    private StardewUI.Framework.IMenuController? _whiteProduceEditorController;
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

        if (!_stardewUiLayoutInitialized)
        {
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
                },
                assets: new StardewUiAssetsOptions(ViewsDir: "assets/views", SpritesDir: "assets/sprites"),
                preloadAssetsOnce: true,
                preloadModels: new[] { typeof(WhiteRoosterProduceEditorViewModel) });
        }

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
}
