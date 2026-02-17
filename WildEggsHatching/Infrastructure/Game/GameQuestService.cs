using System;
using System.Linq;
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

    
    public void AddQuest(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return;

        try
        {
            // Avoid duplicates.
            if (Game1.player.questLog.Any(q => string.Equals(q.id.Value, questId, StringComparison.OrdinalIgnoreCase)))
                return;
        }
        catch
        {
            // ignore
        }

        try { Game1.player.addQuest(questId); }
        catch { /* ignored */ }
    }

public bool HasCompletedSpecialOrder(string orderId)
        => Game1.player.team.completedSpecialOrders.Contains(orderId);
}
