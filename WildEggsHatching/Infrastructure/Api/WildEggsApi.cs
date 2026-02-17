using System.Collections.Generic;
using WildEggsHatching.Api;
using WildEggsHatching.Application.Facade;
using WildEggsHatching.Core.Enums;

namespace WildEggsHatching.Infrastructure.Api;

public sealed class WildEggsApi : IWildEggsApi
{
    private readonly IModFacade _facade;

    public WildEggsApi(IModFacade facade) => _facade = facade;

    public int ApiVersion => 1;

    public ProgressionStep GetProgressionStep() => _facade.GetProgressionStep();

    public bool IsProgressionModeEnabled() => _facade.GetConfigSnapshot().ProgressionMode;

    public bool IsDesertTombModuleEnabled() => _facade.GetConfigSnapshot().DesertTombModuleEnabled;

    public IReadOnlyDictionary<string, int> GetQuestCompletionDays() => _facade.GetQuestCompletionDays();
}
