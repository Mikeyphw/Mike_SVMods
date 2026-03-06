using System;
using System.Collections.Generic;
using LetMeHatch.GameAssetManagement;
using LetMeHatch.Integrations.StardewUi.Advanced;
using LetMeHatch.Integrations.StardewUi.Breeding;
using LetMeHatch.Integrations.StardewUi.Colors;
using Mikew.NestCore.Integrations.StardewUI;
using Mikew.NestCore.Integrations.StardewUI.Dsl;
using StardewModdingAPI;

namespace LetMeHatch.Application.Coordination;

public sealed partial class LmhCoordinator
{
    private const string BreedingEditorMenuId = "lmh.breeding.editor";
    private const string BreedingEditorViewFile = "LmhBreedingEditor";
    private const string ColorsEditorMenuId = "lmh.colors.editor";
    private const string ColorsEditorViewFile = "LmhColorsEditor";
    private const string AdvancedMappingsEditorMenuId = "lmh.advancedMappings.editor";
    private const string AdvancedMappingsEditorViewFile = "LmhAdvancedMappingsEditor";

    private StardewUiHost? _stardewUiHost;
    private StardewUiMenuHost? _stardewUiMenuHost;
    private bool _stardewUiLayoutReady;

    private void InitializeStardewUi()
    {
        _stardewUiHost ??= StardewUiHost.TryCreate(_core.Helper, _core.Manifest);
        _stardewUiMenuHost ??= _stardewUiHost?.CreateMenuHost();
        RebuildStardewUiLayout();
    }

    private void RebuildStardewUiLayout()
    {
        if (_stardewUiHost is null)
        {
            _stardewUiLayoutReady = false;
            return;
        }

        _stardewUiHost.RebuildLayout(
            define: layout =>
            {
                layout.Menu(
                    BreedingEditorMenuId,
                    menu =>
                    {
                        menu.View("LmhBreedingEditor");
                        menu.Context(() => new LmhBreedingEditorViewModel(
                            new LmhBreedingEditorService(),
                            LoadBreedingPacksSnapshot,
                            TryLoadBreedingFemaleRecord,
                            SaveBreedingDraft));
                        menu.ConfigureController(controller =>
                        {
                            controller.DimmingAmount = 0.45f;
                            controller.CloseOnOutsideClick = true;
                            controller.EnableCloseButton();
                        });
                    });

                layout.Menu(
                    ColorsEditorMenuId,
                    menu =>
                    {
                        menu.View(ColorsEditorViewFile);
                        menu.Context(() => new LmhColorsEditorViewModel(
                            new LmhColorsEditorService(),
                            LoadColorsCatalog,
                            SaveColorsDraft));
                        menu.ConfigureController(controller =>
                        {
                            controller.DimmingAmount = 0.45f;
                            controller.CloseOnOutsideClick = true;
                            controller.EnableCloseButton();
                        });
                    });

                layout.Menu(
                    AdvancedMappingsEditorMenuId,
                    menu =>
                    {
                        menu.View(AdvancedMappingsEditorViewFile);
                        menu.Context(() => new LmhItemMappingsEditorViewModel(
                            new LmhItemMappingsEditorService(),
                            LoadItemMappingsCatalog,
                            SaveItemMappingsDraft));
                        menu.ConfigureController(controller =>
                        {
                            controller.DimmingAmount = 0.45f;
                            controller.CloseOnOutsideClick = true;
                            controller.EnableCloseButton();
                        });
                    });
            },
            assets: new StardewUiAssetsOptions(ViewsDir: "assets/views", SpritesDir: "assets/sprites"),
            preloadAssetsOnce: true,
            preloadModels: new[] { typeof(LmhBreedingEditorViewModel), typeof(LmhColorsEditorViewModel), typeof(LmhItemMappingsEditorViewModel) });

        _stardewUiLayoutReady = true;
    }

    public bool HasStardewUiAvailable()
        => StardewUiHost.TryCreate(_core.Helper, _core.Manifest) is not null;

    public string GetBreedingEditorAvailabilityText()
        => HasStardewUiAvailable()
            ? "Duplicate StardewUI breeding editor is available."
            : "Duplicate StardewUI breeding editor requires StardewUI.";

    public string GetColorsEditorAvailabilityText()
        => HasStardewUiAvailable()
            ? "Duplicate StardewUI colors editor is available."
            : "Duplicate StardewUI colors editor requires StardewUI.";

    public string GetAdvancedMappingsEditorAvailabilityText()
        => HasStardewUiAvailable()
            ? "Duplicate StardewUI item mappings editor is available."
            : "Duplicate StardewUI item mappings editor requires StardewUI.";

    public void OpenBreedingEditor()
    {
        if (!Context.IsWorldReady || ModEntry.CurrentAssetManager is null)
            return;

        _stardewUiHost ??= StardewUiHost.TryCreate(_core.Helper, _core.Manifest);
        if (_stardewUiHost is null)
        {
            _log.Log("StardewUI isn't installed; breeding editor unavailable.", LogLevel.Warn);
            return;
        }

        if (!_stardewUiLayoutReady)
            RebuildStardewUiLayout();

        _stardewUiMenuHost ??= _stardewUiHost.CreateMenuHost();
        var handle = _stardewUiMenuHost.OpenAsActiveMenu(BreedingEditorMenuId);
        if (handle is null)
            _log.Log("Failed to open StardewUI breeding editor.", LogLevel.Error);
    }

    public void OpenColorsEditor()
    {
        if (!Context.IsWorldReady)
            return;

        _stardewUiHost ??= StardewUiHost.TryCreate(_core.Helper, _core.Manifest);
        if (_stardewUiHost is null)
        {
            _log.Log("StardewUI isn't installed; colors editor unavailable.", LogLevel.Warn);
            return;
        }

        if (!_stardewUiLayoutReady)
            RebuildStardewUiLayout();

        _stardewUiMenuHost ??= _stardewUiHost.CreateMenuHost();
        var handle = _stardewUiMenuHost.OpenAsActiveMenu(ColorsEditorMenuId);
        if (handle is null)
            _log.Log("Failed to open StardewUI colors editor.", LogLevel.Error);
    }

    public void OpenAdvancedMappingsEditor()
    {
        if (!Context.IsWorldReady || ModEntry.CurrentAssetManager is null)
            return;

        _stardewUiHost ??= StardewUiHost.TryCreate(_core.Helper, _core.Manifest);
        if (_stardewUiHost is null)
        {
            _log.Log("StardewUI isn't installed; advanced mappings editor unavailable.", LogLevel.Warn);
            return;
        }

        if (!_stardewUiLayoutReady)
            RebuildStardewUiLayout();

        _stardewUiMenuHost ??= _stardewUiHost.CreateMenuHost();
        var handle = _stardewUiMenuHost.OpenAsActiveMenu(AdvancedMappingsEditorMenuId);
        if (handle is null)
            _log.Log("Failed to open StardewUI item mappings editor.", LogLevel.Error);
    }

    private IReadOnlyList<LmhBreedingPackRecord> LoadBreedingPacksSnapshot()
    {
        var manager = ModEntry.CurrentAssetManager;
        if (manager is null)
            return Array.Empty<LmhBreedingPackRecord>();

        return new LmhBreedingEditorService().LoadPacksSnapshot(manager.LoadedFemaleBreedingData, manager.LoadedContentPacks);
    }

    private LmhBreedingFemaleRecord? TryLoadBreedingFemaleRecord(string contentPackId, string femaleKey)
    {
        var manager = ModEntry.CurrentAssetManager;
        if (manager is null)
            return null;

        return new LmhBreedingEditorService().TryLoadFemaleRecord(
            manager.LoadedFemaleBreedingData,
            manager.BreedingConfigsByPack,
            contentPackId,
            femaleKey,
            out var record)
            ? record
            : null;
    }

    private void SaveBreedingDraft(string contentPackId, string femaleKey, LmhBreedingDraft draft)
    {
        var manager = ModEntry.CurrentAssetManager;
        if (manager is null)
            return;

        if (!manager.LoadedFemaleBreedingData.TryGetValue(contentPackId, out var byFemaleData)
            || !byFemaleData.TryGetValue(femaleKey, out var femaleData)
            || !manager.BreedingConfigsByPack.TryGetValue(contentPackId, out var byFemaleConfig)
            || !byFemaleConfig.TryGetValue(femaleKey, out var femaleConfig))
        {
            return;
        }

        new LmhBreedingEditorService().ApplyDraft(femaleData, femaleConfig, draft);
        GameAssetManager.SaveData(manager.BreedingConfigsByPack, "BreedingConfigsByPack");
        InvalidateAllContent();
        InvalidateGameContentCache();
    }

    private LmhColorsEditorCatalog LoadColorsCatalog()
        => new LmhColorsEditorService().LoadCatalog(_core.Helper, _config.Get());

    private void SaveColorsDraft(LmhColorsEditorDraft draft)
    {
        _config.Update(cfg => new LmhColorsEditorService().ApplyDraft(cfg, draft), save: true);
        InvalidateAllContent();
        InvalidateGameContentCache();
    }

    private LmhItemMappingsEditorCatalog LoadItemMappingsCatalog()
    {
        var manager = ModEntry.CurrentAssetManager;
        return manager is null
            ? new LmhItemMappingsEditorCatalog(Array.Empty<LmhItemMappingsGroupRecord>())
            : new LmhItemMappingsEditorService().LoadCatalog(manager.SavedItemsData, _config.Get());
    }

    private void SaveItemMappingsDraft(LmhItemMappingsEditorDraft draft)
    {
        var manager = ModEntry.CurrentAssetManager;
        if (manager is null)
            return;

        _config.Update(cfg => new LmhItemMappingsEditorService().ApplyDraft(cfg, manager.SavedItemsData, draft), save: true);
        GameAssetManager.SaveData(manager.SavedItemsData);
        InvalidateAllContent();
        InvalidateGameContentCache();
    }
}
