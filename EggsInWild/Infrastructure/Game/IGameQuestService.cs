namespace EggsInWild.Infrastructure.Game;

public interface IGameQuestService
{
    bool HasCompletedQuest(string questId);
    bool HasCompletedSpecialOrder(string orderId);

    void AddQuest(string questId);
    bool RemoveQuest(string questId);

    bool AddSpecialOrder(string orderId);
    bool RemoveSpecialOrder(string orderId);
}
