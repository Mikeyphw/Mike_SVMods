using System;

namespace EggsInWild.Application.Progression;

public sealed class UnlockContextProvider
{
    private readonly IUnlockFactsProvider _facts;
    private readonly Func<ProgressionSaveData> _getSaveData;

    public UnlockContextProvider(IUnlockFactsProvider facts, Func<ProgressionSaveData> getSaveData)
    {
        _facts = facts;
        _getSaveData = getSaveData;
    }

    public UnlockContext Get()
    {
        var facts = _facts.GetFacts(_getSaveData());
        return new UnlockContext(facts.LocationName, facts.PlayerName);
    }
}

public sealed record UnlockContext(string LocationName, string PlayerName);
