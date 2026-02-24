namespace EggsInWild.Infrastructure.Game;

public interface IGameQuestService
{
    bool HasCompletedQuest(string questId);
    bool HasCompletedSpecialOrder(string orderId);

    void AddQuest(string questId);
}

