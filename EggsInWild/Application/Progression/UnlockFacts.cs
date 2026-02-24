using System.Collections.Generic;

namespace EggsInWild.Application.Progression;

public sealed class UnlockFacts
{
    public UnlockFacts(
        int dayNumber,
        bool isMainPlayer,
        string locationName,
        string playerName,
        IReadOnlyDictionary<string, bool> receivedMail,
        IReadOnlyDictionary<string, bool> completedQuests,
        IReadOnlyDictionary<string, bool> completedSpecialOrders,
        IReadOnlyDictionary<string, int> questCompletedDay
    )
    {
        DayNumber = dayNumber;
        IsMainPlayer = isMainPlayer;
        LocationName = locationName;
        PlayerName = playerName;
        _receivedMail = receivedMail;
        _completedQuests = completedQuests;
        _completedSpecialOrders = completedSpecialOrders;
        QuestCompletedDay = questCompletedDay;
    }

    public int DayNumber { get; }
    public bool IsMainPlayer { get; }
    public string LocationName { get; }
    public string PlayerName { get; }

    public IReadOnlyDictionary<string, int> QuestCompletedDay { get; }

    public bool HasReceivedMail(string id) => _receivedMail.TryGetValue(id, out var has) && has;
    public bool HasCompletedQuest(string id) => _completedQuests.TryGetValue(id, out var has) && has;

    public bool HasCompletedSpecialOrder(string id)
        => _completedSpecialOrders.TryGetValue(id, out var has) && has;

    private readonly IReadOnlyDictionary<string, bool> _receivedMail;
    private readonly IReadOnlyDictionary<string, bool> _completedQuests;
    private readonly IReadOnlyDictionary<string, bool> _completedSpecialOrders;
}
