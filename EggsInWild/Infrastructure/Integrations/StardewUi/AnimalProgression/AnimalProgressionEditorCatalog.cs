using System.Collections.Generic;

namespace EggsInWild.Infrastructure.Integrations.StardewUi.AnimalProgression;

public sealed record AnimalProgressionAnimalRecord(
    string AnimalId,
    string DisplayName,
    bool DefaultEnabled,
    bool ConfiguredEnabled,
    bool EffectiveEnabled);

public sealed record AnimalProgressionDraft(
    IReadOnlyDictionary<string, bool> ConfiguredModes);
