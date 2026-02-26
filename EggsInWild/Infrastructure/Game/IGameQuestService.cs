namespace EggsInWild.Infrastructure.Game;

public interface IGameQuestService
{
    bool HasCompletedQuest(string questId);
    bool HasCompletedSpecialOrder(string orderId);
    bool HasQuestInLog(string questId);

    bool CompleteQuest(string questId);
    bool UncompleteQuest(string questId);

    bool MarkSpecialOrderCompleted(string orderId);
    bool UnmarkSpecialOrderCompleted(string orderId);

    void AddQuest(string questId);
    bool RemoveQuest(string questId);

    bool AddSpecialOrder(string orderId);
    bool RemoveSpecialOrder(string orderId);
}
