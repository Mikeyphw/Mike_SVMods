namespace WildEggsHatching.Infrastructure.Game;

public interface IGameQuestService
{
    bool HasCompletedQuest(string questId);
    bool HasCompletedSpecialOrder(string orderId);
}
