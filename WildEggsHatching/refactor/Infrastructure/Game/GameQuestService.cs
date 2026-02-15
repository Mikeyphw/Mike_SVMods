using System;
using StardewValley;

namespace WildEggsHatching.Infrastructure.Game;

public sealed class GameQuestService : IGameQuestService
{
    public bool HasCompletedQuest(string questId)
    {
        var player = Game1.player;
        var method = player.GetType().GetMethod("hasCompletedQuest", new[] { typeof(string) });
        if (method is null)
            return false;

        return (bool)(method.Invoke(player, new object[] { questId }) ?? false);
    }

    public bool HasCompletedSpecialOrder(string orderId)
        => Game1.player.team.completedSpecialOrders.Contains(orderId);
}
