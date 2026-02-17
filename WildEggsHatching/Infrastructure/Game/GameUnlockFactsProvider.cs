using System.Collections.Generic;
using WildEggsHatching.Application.Progression;
using WildEggsHatching.Core.Ids;

namespace WildEggsHatching.Infrastructure.Game;

public sealed class GameUnlockFactsProvider : IUnlockFactsProvider
{
    private readonly IGameStateReader _game;
    private readonly IGameMailService _mail;
    private readonly IGameQuestService _quests;

    public GameUnlockFactsProvider(IGameStateReader game, IGameMailService mail, IGameQuestService quests)
    {
        _game = game;
        _mail = mail;
        _quests = quests;
    }

    public UnlockFacts GetFacts(ProgressionSaveData saveData)
    {
        var player = _game.GetPlayer();
        var location = _game.GetLocation();

        var receivedMail = new Dictionary<string, bool>();
        foreach (var id in ProgressionIds.Mail.Tracked)
            receivedMail[id] = _mail.HasReceivedMail(id);

        var completedQuests = new Dictionary<string, bool>();
        var completedSpecialOrders = new Dictionary<string, bool>();
        foreach (var id in ProgressionIds.Quests.Tracked)
        {
            if (ProgressionIds.Quests.SpecialOrders.Contains(id))
                completedSpecialOrders[id] = _quests.HasCompletedSpecialOrder(id);
            else
                completedQuests[id] = _quests.HasCompletedQuest(id);
        }

        return new UnlockFacts(
            dayNumber: _game.GetDayNumber(),
            isMainPlayer: player.IsMainPlayer,
            locationName: location.NameOrUniqueName,
            playerName: player.Name,
            receivedMail: receivedMail,
            completedQuests: completedQuests,
            completedSpecialOrders: completedSpecialOrders,
            questCompletedDay: saveData.QuestCompletedDay
        );
    }
}
