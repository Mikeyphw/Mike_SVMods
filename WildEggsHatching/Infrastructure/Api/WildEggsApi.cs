using System;
using System.Collections.Generic;
using System.Linq;
using WildEggsHatching.Api;
using WildEggsHatching.Application.Facade;
using WildEggsHatching.Core.Enums;
using StardewValley;
using StardewValley.GameData.Objects;

namespace WildEggsHatching.Infrastructure.Api;

public sealed class WildEggsApi : IWildEggsApi
{
    private readonly IModFacade _facade;

    private IReadOnlyCollection<string>? _cachedWildEggIds;
    private IReadOnlyCollection<string>? _cachedValidAnimals;

    public WildEggsApi(IModFacade facade) => _facade = facade;

    public int ApiVersion => 1;

    public IReadOnlyCollection<string> WildEggsIDs => _cachedWildEggIds ??= ComputeWildEggIds();

    public IReadOnlyCollection<string> ValidAnimals => _cachedValidAnimals ??= ComputeValidAnimals();

    public string BirthTextMode => "Default";

    public ProgressionStep GetProgressionStep() => _facade.GetProgressionStep();

    public bool IsProgressionModeEnabled() => _facade.GetConfigSnapshot().ProgressionMode;

    public bool IsDesertTombModuleEnabled() => _facade.GetConfigSnapshot().DesertTombModuleEnabled;

    public IReadOnlyDictionary<string, int> GetQuestCompletionDays() => _facade.GetQuestCompletionDays();
}
