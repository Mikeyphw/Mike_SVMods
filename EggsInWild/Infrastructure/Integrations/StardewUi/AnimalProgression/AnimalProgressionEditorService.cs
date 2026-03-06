using System;
using System.Collections.Generic;
using System.Linq;
using EggsInWild.Core.ShopProgression;
using EggsInWild.Infrastructure.Config;
using StardewModdingAPI;
using StardewValley.GameData.FarmAnimals;

namespace EggsInWild.Infrastructure.Integrations.StardewUi.AnimalProgression;

public sealed class AnimalProgressionEditorService
{
    public IReadOnlyList<AnimalProgressionAnimalRecord> LoadCatalog(IModHelper helper, ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(helper);
        ArgumentNullException.ThrowIfNull(config);

        var farmAnimals = helper.GameContent.Load<Dictionary<string, FarmAnimalData>>("Data/FarmAnimals");
        config.AnimalProgressionMode ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        return farmAnimals
            .OrderBy(pair => pair.Value?.DisplayName?.ToString() ?? pair.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair =>
            {
                var defaultEnabled = AnimalShopProgressionPolicy.DefaultEnabled(pair.Key);
                var configuredEnabled = config.AnimalProgressionMode.TryGetValue(pair.Key, out var configured)
                    ? configured
                    : defaultEnabled;
                return new AnimalProgressionAnimalRecord(
                    pair.Key,
                    BuildAnimalOptionName(pair.Key, pair.Value),
                    defaultEnabled,
                    configuredEnabled,
                    configuredEnabled);
            })
            .ToList();
    }

    public AnimalProgressionDraft CreateDraft(ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.AnimalProgressionMode ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        return new AnimalProgressionDraft(new Dictionary<string, bool>(config.AnimalProgressionMode, StringComparer.OrdinalIgnoreCase));
    }

    public void ApplyDraft(ModConfig config, AnimalProgressionDraft draft, IReadOnlyList<AnimalProgressionAnimalRecord> catalog)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(catalog);

        config.AnimalProgressionMode ??= new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        config.AnimalProgressionMode.Clear();

        foreach (var animal in catalog)
        {
            if (!draft.ConfiguredModes.TryGetValue(animal.AnimalId, out var value) || value == animal.DefaultEnabled)
                continue;

            config.AnimalProgressionMode[animal.AnimalId] = value;
        }
    }

    public static string BuildAnimalOptionName(string animalId, FarmAnimalData? farmAnimal)
    {
        var displayName = farmAnimal?.DisplayName?.ToString();
        if (string.IsNullOrWhiteSpace(displayName) || string.Equals(displayName, animalId, StringComparison.OrdinalIgnoreCase))
            return animalId;

        return $"{displayName} ({animalId})";
    }
}
