using System;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Integrations.StardewUi.AnimalProgression;
using EggsInWild.Infrastructure.Integrations.StardewUi.WildAnimals;
using Mikew.NestCore.Framework;
using Mikew.NestCore.Integrations.StardewUI;
using Mikew.NestCore.Integrations.StardewUI.Dsl;
using StardewModdingAPI;

namespace EggsInWild;

public sealed partial class ModEntry
{
    private const string WildAnimalsEditorMenuId = "wildAnimalsEditor";
    private const string AnimalProgressionEditorMenuId = "animalProgressionEditor";

    private StardewUiHost? _wildAnimalsStardewUiHost;
    private StardewUiMenuHost? _wildAnimalsMenuHost;
    private WildAnimalsEditorViewModel? _currentWildAnimalsEditorViewModel;
    private AnimalProgressionEditorViewModel? _currentAnimalProgressionEditorViewModel;
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
                    WildAnimalsEditorMenuId,
                    menu =>
                    {
                        menu.View("wildAnimals/editor");
                        menu.Context(() =>
                        {
                            _currentWildAnimalsEditorViewModel = new WildAnimalsEditorViewModel(
                                new WildAnimalsEditorService(Helper, Monitor, () => _config.Get()),
                                () => _config.Get(),
                                SaveWildAnimalsDraft);
                            return _currentWildAnimalsEditorViewModel;
                        });
                        menu.ConfigureController(controller =>
                        {
                            controller.DimmingAmount = 0.45f;
                            controller.CloseOnOutsideClick = true;
                            controller.EnableCloseButton();
                            controller.CanClose = () => _currentWildAnimalsEditorViewModel?.CanCloseMenu() ?? true;
                        });
                        menu.OnClosed(() => _currentWildAnimalsEditorViewModel = null);
                    });

                layout.Menu(
                    AnimalProgressionEditorMenuId,
                    menu =>
                    {
                        menu.View("animalProgression/editor");
                        menu.Context(() =>
                        {
                            _currentAnimalProgressionEditorViewModel = new AnimalProgressionEditorViewModel(
                                new AnimalProgressionEditorService(),
                                () => new AnimalProgressionEditorService().LoadCatalog(Helper, _config!.Get()),
                                () => new AnimalProgressionEditorService().CreateDraft(_config!.Get()),
                                SaveAnimalProgressionDraft);
                            return _currentAnimalProgressionEditorViewModel;
                        });
                        menu.ConfigureController(controller =>
                        {
                            controller.DimmingAmount = 0.45f;
                            controller.CloseOnOutsideClick = true;
                            controller.EnableCloseButton();
                        });
                        menu.OnClosed(() => _currentAnimalProgressionEditorViewModel = null);
                    });
            },
            assets: new StardewUiAssetsOptions(ViewsDir: "assets/views", SpritesDir: "assets/sprites"),
            preloadAssetsOnce: true,
            preloadModels: new[] { typeof(WildAnimalsEditorViewModel), typeof(WildAnimalDraftViewModel), typeof(AnimalProgressionEditorViewModel) });

        _wildAnimalsLayoutReady = true;
    }

    private void OpenWildAnimalsEditor()
    {
        if (_wildAnimalsStardewUiHost is null || !_wildAnimalsLayoutReady)
        {
            Monitor.Log("StardewUI isn't installed; WildAnimals editor unavailable.", LogLevel.Warn);
            return;
        }

        _wildAnimalsMenuHost ??= _wildAnimalsStardewUiHost.CreateMenuHost();
        var handle = _wildAnimalsMenuHost.OpenAsActiveMenu(WildAnimalsEditorMenuId);
        if (handle is null)
            Monitor.Log("Failed to open the WildAnimals StardewUI editor.", LogLevel.Error);
    }

    private void OpenAnimalProgressionEditor()
    {
        if (_wildAnimalsStardewUiHost is null || !_wildAnimalsLayoutReady)
        {
            Monitor.Log("StardewUI isn't installed; Animal Progression editor unavailable.", LogLevel.Warn);
            return;
        }

        _wildAnimalsMenuHost ??= _wildAnimalsStardewUiHost.CreateMenuHost();
        var handle = _wildAnimalsMenuHost.OpenAsActiveMenu(AnimalProgressionEditorMenuId);
        if (handle is null)
            Monitor.Log("Failed to open the Animal Progression StardewUI editor.", LogLevel.Error);
    }

    private void DisposeWildAnimalsEditor()
    {
        _wildAnimalsMenuHost?.DisposeAll();
        _wildAnimalsMenuHost?.Dispose();
        _wildAnimalsMenuHost = null;
    }

    private void SaveWildAnimalsDraft(ModConfig cfg, WildAnimalDraft draft)
    {
        var service = new WildAnimalsEditorService(Helper, Monitor, () => cfg);
        service.ApplyDraft(cfg, draft);
        NormalizeLocationTypesOverrides(cfg);
        _config?.Save();
        InvalidateGameAssets();
        SyncApiState();
        UpdateDiagnosticsState();
    }

    private void SaveAnimalProgressionDraft(AnimalProgressionDraft draft, IReadOnlyList<AnimalProgressionAnimalRecord> catalog)
    {
        if (_config is null)
            return;

        var cfg = _config.Get();
        new AnimalProgressionEditorService().ApplyDraft(cfg, draft, catalog);
        _config.Save();
        InvalidateGameAssets();
        SyncApiState();
        UpdateDiagnosticsState();
    }
}
