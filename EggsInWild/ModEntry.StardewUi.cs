using System;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Integrations.StardewUi;
using EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;
using Mikew.NestCore.Framework;
using Mikew.NestCore.Integrations.StardewUI;
using Mikew.NestCore.Integrations.StardewUI.Dsl;
using StardewModdingAPI;

namespace EggsInWild;

public sealed partial class ModEntry
{
    private const string EditorsHubMenuId = "editorsHub";
    private const string WildAnimalsTabKey = "wildAnimals";

    private StardewUiHost? _wildAnimalsStardewUiHost;
    private StardewUiMenuHost? _wildAnimalsMenuHost;
    private WildAnimalsEditorViewModel? _currentWildAnimalsEditorViewModel;
    private EggsInWildEditorHubViewModel? _currentEditorsHubViewModel;
    private string _pendingEditorsHubTab = WildAnimalsTabKey;
    private bool _wildAnimalsLayoutReady;

    private void InitializeWildAnimalsStardewUi()
    {
        var nestCore = Helper.ModRegistry.GetApi<INestCoreApi>("Mikew.nestcore");
        _wildAnimalsStardewUiHost = nestCore?.TryCreateStardewUiHost(Helper, ModManifest);
        _wildAnimalsMenuHost ??= _wildAnimalsStardewUiHost?.CreateMenuHost();
        RebuildWildAnimalsStardewUiLayout();
    }

    private void RebuildWildAnimalsStardewUiLayout()
    {
        if (_wildAnimalsStardewUiHost is null || _config is null)
        {
            _wildAnimalsLayoutReady = false;
            return;
        }

        _wildAnimalsStardewUiHost.RebuildLayout(
            define: layout =>
            {
                layout.Menu(
                    EditorsHubMenuId,
                    menu =>
                    {
                        menu.View("editorHub/editor");
                        menu.Context(() =>
                        {
                            _currentWildAnimalsEditorViewModel = new WildAnimalsEditorViewModel(
                                new WildAnimalsEditorService(Helper, Monitor, () => _config.Get()),
                                () => _config.Get(),
                                SaveWildAnimalsDraft);
                            _currentEditorsHubViewModel = new EggsInWildEditorHubViewModel(
                                _currentWildAnimalsEditorViewModel,
                                _pendingEditorsHubTab);
                            return _currentEditorsHubViewModel;
                        });
                        menu.ConfigureController(controller =>
                        {
                            controller.DimmingAmount = 0.45f;
                            controller.CloseOnOutsideClick = true;
                            controller.EnableCloseButton();
                            controller.CanClose = () => _currentEditorsHubViewModel?.CanCloseMenu() ?? true;
                        });
                        menu.OnClosed(() =>
                        {
                            _currentEditorsHubViewModel = null;
                            _currentWildAnimalsEditorViewModel = null;
                        });
                    });
            },
            assets: new StardewUiAssetsOptions(ViewsDir: "assets/views", SpritesDir: "assets/sprites"),
            preloadAssetsOnce: true,
            preloadModels: new[] { typeof(EggsInWildEditorHubViewModel), typeof(WildAnimalsEditorViewModel), typeof(WildAnimalDraftViewModel) });

        _wildAnimalsLayoutReady = true;
    }

    private void OpenWildAnimalsEditor()
    {
        if (_wildAnimalsStardewUiHost is null || !_wildAnimalsLayoutReady)
        {
            Monitor.Log("StardewUI isn't installed; WildAnimals editor unavailable.", LogLevel.Warn);
            return;
        }

        _pendingEditorsHubTab = WildAnimalsTabKey;
        _wildAnimalsMenuHost ??= _wildAnimalsStardewUiHost.CreateMenuHost();
        var handle = _wildAnimalsMenuHost.OpenAsActiveMenu(EditorsHubMenuId);
        if (handle is null)
            Monitor.Log("Failed to open the WildAnimals StardewUI editor.", LogLevel.Error);
    }

    private void OpenAnimalProgressionEditor()
    {
        OpenWildAnimalsEditor();
    }

    private void DisposeWildAnimalsEditor()
    {
        _wildAnimalsMenuHost?.DisposeAll();
        _wildAnimalsMenuHost?.Dispose();
        _wildAnimalsMenuHost = null;
    }

    private void SaveWildAnimalsDraft(ModConfig cfg, WildAnimalDraft draft, WildAnimalProgressionOverride? progression)
    {
        var service = new WildAnimalsEditorService(Helper, Monitor, () => cfg);
        service.ApplyDraft(cfg, draft);
        if (progression is not null && !string.IsNullOrWhiteSpace(progression.FarmAnimalKey))
        {
            cfg.AnimalProgressionMode ??= new System.Collections.Generic.Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (progression.ConfiguredEnabled is null)
                cfg.AnimalProgressionMode.Remove(progression.FarmAnimalKey);
            else
                cfg.AnimalProgressionMode[progression.FarmAnimalKey] = progression.ConfiguredEnabled.Value;
        }
        NormalizeLocationTypesOverrides(cfg);
        _config?.Save();
        InvalidateGameAssets();
        SyncApiState();
        UpdateDiagnosticsState();
    }
}
