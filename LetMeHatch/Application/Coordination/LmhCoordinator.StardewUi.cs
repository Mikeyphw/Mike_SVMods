using System;
using System.Collections.Generic;
using LetMeHatch.GameAssetManagement;
using LetMeHatch.Helpers;
using LetMeHatch.Integrations.StardewUi;
using LetMeHatch.Integrations.StardewUi.Advanced;
using LetMeHatch.Integrations.StardewUi.Breeding;
using LetMeHatch.Integrations.StardewUi.Colors;
using LetMeHatch.Models;
using Mikew.NestCore.Integrations.StardewUI;
using Mikew.NestCore.Integrations.StardewUI.Dsl;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;

namespace LetMeHatch.Application.Coordination;

public sealed partial class LmhCoordinator
{
    private const string EditorHubMenuId = "lmh.editorHub";
    private const string EditorHubViewFile = "LmhEditorHub";

    private StardewUiHost? _stardewUiHost;
    private StardewUI.Framework.IMenuController? _editorHubController;
    private bool _stardewUiLayoutReady;
    private LmhEditorHubTab _requestedHubTab = LmhEditorHubTab.Breeding;

    public void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu is not null)
            return;

        var cfg = _config.Get();

        if (cfg.OpenBreedingEditorKey.JustPressed())
        {
            OpenBreedingEditor();
            return;
        }

        if (cfg.OpenColorsEditorKey.JustPressed())
        {
            OpenColorsEditor();
            return;
        }

        if (cfg.OpenAdvancedMappingsEditorKey.JustPressed())
            OpenAdvancedMappingsEditor();
    }

    private void InitializeStardewUi()
    {
        _stardewUiHost ??= StardewUiHost.TryCreate(_core.Helper, _core.Manifest);
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
                    EditorHubMenuId,
                    menu =>
                    {
                        menu.View(EditorHubViewFile);
                        menu.Context(CreateEditorHubViewModel);
                        menu.ConfigureController(controller =>
                        {
                            controller.DimmingAmount = 0.45f;
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
                typeof(LmhEditorHubViewModel),
                typeof(LmhBreedingEditorViewModel),
                typeof(LmhColorsEditorViewModel),
                typeof(LmhItemMappingsEditorViewModel),
                typeof(LmhSettingsEditorViewModel),
                typeof(LmhDebugEditorViewModel),
            });

        _stardewUiLayoutReady = true;
    }

    public bool HasStardewUiAvailable()
        => StardewUiHost.TryCreate(_core.Helper, _core.Manifest) is not null;

    public string GetBreedingEditorAvailabilityText()
        => HasStardewUiAvailable()
            ? "StardewUI editor hub is available and can open the Breeding tab."
            : "StardewUI editor hub requires StardewUI.";

    public string GetColorsEditorAvailabilityText()
        => HasStardewUiAvailable()
            ? "StardewUI editor hub is available and can open the Colors tab."
            : "StardewUI editor hub requires StardewUI.";

    public string GetAdvancedMappingsEditorAvailabilityText()
        => HasStardewUiAvailable()
            ? "StardewUI editor hub is available and can open the Mappings tab."
            : "StardewUI editor hub requires StardewUI.";

    public void OpenBreedingEditor()
        => OpenEditorHub(LmhEditorHubTab.Breeding);

    public void OpenColorsEditor()
        => OpenEditorHub(LmhEditorHubTab.Colors);

    public void OpenAdvancedMappingsEditor()
        => OpenEditorHub(LmhEditorHubTab.Mappings);

    private void OpenEditorHub(LmhEditorHubTab tab)
    {
        if (!Context.IsWorldReady)
            return;

        _stardewUiHost ??= StardewUiHost.TryCreate(_core.Helper, _core.Manifest);
        if (_stardewUiHost is null)
        {
            _log.Log("StardewUI isn't installed; breeding editor unavailable.", LogLevel.Warn);
            return;
        }

        if (!_stardewUiLayoutReady)
            RebuildStardewUiLayout();

        _requestedHubTab = tab;
        if (_editorHubController is not null)
            return;

        var controller = _stardewUiHost.CreateMenuController(EditorHubMenuId);
        if (controller is null)
        {
            _log.Log("Failed to open LetMeHatch StardewUI editor hub.", LogLevel.Error);
            return;
        }

        _editorHubController = controller;
        Game1.activeClickableMenu = controller.Menu;
    }

    private LmhEditorHubViewModel CreateEditorHubViewModel()
        => new(
            new LmhBreedingEditorViewModel(
                new LmhBreedingEditorService(),
                LoadBreedingPacksSnapshot,
                TryLoadBreedingFemaleRecord,
                SaveBreedingDraft,
                TryLoadFarmAnimalPreviewSprite),
            new LmhColorsEditorViewModel(
                new LmhColorsEditorService(),
                LoadColorsCatalog,
                SaveColorsDraft,
                TryLoadEggPreviewSprite,
                TryLoadEggTilePreviewSprite),
            new LmhItemMappingsEditorViewModel(
                new LmhItemMappingsEditorService(),
                LoadItemMappingsCatalog,
                SaveItemMappingsDraft,
                TryLoadFarmAnimalPreviewSprite,
                TryLoadObjectPreviewSprite),
            new LmhSettingsEditorViewModel(
                _config.Get,
                SaveSettingsDraft,
                LoadSettingsAnimals,
                GetBreedingEditorAvailabilityText,
                GetColorsEditorAvailabilityText,
                GetAdvancedMappingsEditorAvailabilityText),
            new LmhDebugEditorViewModel(
                _config.Get,
                SaveDebugDraft),
            _requestedHubTab);

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

    private Tuple<Texture2D, Rectangle>? TryLoadEggPreviewSprite(string eggId, Color color)
    {
        try
        {
            var objects = _core.Helper.GameContent.Load<Dictionary<string, ObjectData>>(Mikew.NestCore.EnumsAndStrings.Strings.AssetNames.Objects);
            if (!objects.TryGetValue(eggId, out var data))
                return null;

            var textureName = string.IsNullOrWhiteSpace(data.Texture) ? Mikew.NestCore.EnumsAndStrings.Strings.AssetNames.SpringObjects : data.Texture;
            var texture = Game1.content.Load<Texture2D>(textureName);
            var crop = new ItemTextureInfo(eggId, data.SpriteIndex, EnumsAndStrings.Enums.CloneItemType.Egg, textureName).GetCropRectangle(texture.Width);
            var cropped = Colors.CropTexture(texture, crop);
            var preview = color == Color.Transparent
                ? Colors.CloneTexture(cropped)
                : Colors.ColorImage(cropped, LmhColorsEditorService.FormatColor(color));
            return Tuple.Create(preview, new Rectangle(0, 0, preview.Width, preview.Height));
        }
        catch
        {
            return null;
        }
    }

    private Tuple<Texture2D, Rectangle>? TryLoadEggTilePreviewSprite(string eggId)
        => TryLoadEggPreviewSprite(eggId, Color.Transparent);

    private Tuple<Texture2D, Rectangle>? TryLoadFarmAnimalPreviewSprite(string animalKey)
    {
        try
        {
            if (!Game1.farmAnimalData.TryGetValue(animalKey, out var data))
                return null;

            if (string.IsNullOrWhiteSpace(data.Texture) || data.SpriteWidth <= 0 || data.SpriteHeight <= 0)
                return null;

            var texture = Game1.content.Load<Texture2D>(data.Texture);
            var rect = new Rectangle(0, 0, data.SpriteWidth, data.SpriteHeight);
            return Tuple.Create(texture, rect);
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
            var itemData = ItemRegistry.GetDataOrErrorItem(itemId);
            return Tuple.Create(itemData.GetTexture(), itemData.GetSourceRect());
        }
        catch
        {
            return null;
        }
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

    private IReadOnlyList<LmhSettingsAnimalOption> LoadSettingsAnimals()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void Add(string key, string label)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;
            map.TryAdd(key, string.IsNullOrWhiteSpace(label) ? key : label);
        }

        try
        {
            var data = _core.Helper.GameContent.Load<Dictionary<string, StardewValley.GameData.FarmAnimals.FarmAnimalData>>(Mikew.NestCore.EnumsAndStrings.Strings.AssetNames.FarmAnimals);
            foreach (var (key, animal) in data)
                Add(key, animal?.DisplayName ?? key);
        }
        catch
        {
        }

        var cfg = _config.Get();
        foreach (var key in cfg.EditedEggListsOptions.Keys)
            Add(key, key);
        foreach (var key in cfg.EditedBirthTextOptions.Keys)
            Add(key, key);
        foreach (var key in cfg.ClonedVanillaEggsOptions.Keys)
            Add(key, key);

        return map
            .Select(kvp => new LmhSettingsAnimalOption(kvp.Key, kvp.Value))
            .OrderBy(option => option.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(option => option.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void SaveSettingsDraft(LmhSettingsDraft draft)
    {
        _config.Update(cfg =>
        {
            cfg.EditEggListByName = draft.EditEggListByName;
            cfg.EditBirthText = draft.EditBirthText;
            cfg.AlternateBirthText = draft.AlternateBirthText;
            cfg.CloneVanillaEggs = draft.CloneVanillaEggs;
            cfg.DinoEggAsArtifact = draft.DinoEggAsArtifact;
            cfg.CloneMachineRules = draft.CloneMachineRules;
            cfg.UseDynamicCloneTextures = draft.UseDynamicCloneTextures;
            cfg.DynamicCloneTintMode = draft.DynamicCloneTintMode;
            cfg.OnlyFertilizedEggs = draft.OnlyFertilizedEggs;
            cfg.ColorClonedMilk = draft.ColorClonedMilk;
            cfg.ExclusiveRabbitWool = draft.ExclusiveRabbitWool;
            cfg.BreedingSystemEnabled = draft.BreedingSystemEnabled;
            cfg.DeterministicBreedingRng = draft.DeterministicBreedingRng;
            cfg.DailyLuckBonus = float.Parse(draft.DailyLuckBonusText, System.Globalization.CultureInfo.InvariantCulture);
            cfg.EggChances[LetMeHatch.EnumsAndStrings.Enums.ItemType.Normal] = float.Parse(draft.NormalEggChanceText, System.Globalization.CultureInfo.InvariantCulture);
            cfg.EggChances[LetMeHatch.EnumsAndStrings.Enums.ItemType.Large] = float.Parse(draft.LargeEggChanceText, System.Globalization.CultureInfo.InvariantCulture);
            cfg.EggChances[LetMeHatch.EnumsAndStrings.Enums.ItemType.FertilizedEgg] = float.Parse(draft.FertilizedEggChanceText, System.Globalization.CultureInfo.InvariantCulture);
            cfg.EditedEggListsOptions = new Dictionary<string, bool>(draft.EditedEggListsOptions, StringComparer.OrdinalIgnoreCase);
            cfg.EditedBirthTextOptions = new Dictionary<string, bool>(draft.EditedBirthTextOptions, StringComparer.OrdinalIgnoreCase);
            cfg.ClonedVanillaEggsOptions = new Dictionary<string, bool>(draft.ClonedVanillaEggsOptions, StringComparer.OrdinalIgnoreCase);
        }, save: true);

        InvalidateAllContent();
        InvalidateGameContentCache();
    }

    private void SaveDebugDraft(LmhDebugDraft draft)
    {
        _config.Update(cfg =>
        {
            cfg.ShowEdits = draft.ShowEdits;
            cfg.ShowAnimalKeys = draft.ShowAnimalKeys;
            cfg.VerboseLogging = draft.VerboseLogging;
            cfg.TestingLogging = draft.TestingLogging;
        }, save: true);
    }
}
