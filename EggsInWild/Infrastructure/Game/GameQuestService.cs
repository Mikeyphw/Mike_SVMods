using System;
using System.Linq;
using StardewValley;
using StardewValley.SpecialOrders;

namespace EggsInWild.Infrastructure.Game;

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

    public bool HasQuestInLog(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return false;

        try
        {
            return Game1.player.questLog.Any(q => string.Equals(q.id.Value, questId, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    public void AddQuest(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return;

        try
        {
            if (Game1.player.questLog.Any(q => string.Equals(q.id.Value, questId, StringComparison.OrdinalIgnoreCase)))
                return;
        }
        catch
        {
            // ignored
        }

        try { Game1.player.addQuest(questId); }
        catch { /* ignored */ }
    }

    public bool CompleteQuest(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return false;

        try
        {
            if (HasCompletedQuest(questId))
                return false;

            Game1.player.completeQuest(questId);
            return HasCompletedQuest(questId);
        }
        catch
        {
            return false;
        }
    }

    public bool UncompleteQuest(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return false;

        try
        {
            var field = Game1.player.GetType().GetField("completedQuests", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (field?.GetValue(Game1.player) is System.Collections.IList list)
            {
                // Some versions store completed quests in a NetStringList-like type.
                for (var j = list.Count - 1; j >= 0; j--)
                {
                    if (string.Equals(list[j]?.ToString(), questId, StringComparison.OrdinalIgnoreCase))
                    {
                        list.RemoveAt(j);
                        return true;
                    }
                }
                return false;
            }

            if (field?.GetValue(Game1.player) is System.Collections.Generic.ICollection<string> set)
                return set.Remove(questId);
        }
        catch
        {
            // ignored
        }

        return false;
    }

    public bool MarkSpecialOrderCompleted(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return false;

        try
        {
            if (Game1.player.team.completedSpecialOrders.Contains(orderId))
                return false;

            // Remove any active instance first (best-effort).
            try { Game1.player.team.specialOrders.RemoveWhere(o => string.Equals(o.questKey.Value, orderId, StringComparison.Ordinal)); }
            catch { /* ignored */ }

            Game1.player.team.completedSpecialOrders.Add(orderId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool UnmarkSpecialOrderCompleted(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return false;

        try
        {
            return Game1.player.team.completedSpecialOrders.Remove(orderId);
        }
        catch
        {
            return false;
        }
    }

    public bool RemoveQuest(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return false;

        try
        {
            if (!Game1.player.questLog.Any(q => string.Equals(q.id.Value, questId, StringComparison.OrdinalIgnoreCase)))
                return false;
        }
        catch
        {
            // ignored
        }

        try
        {
            Game1.player.removeQuest(questId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool AddSpecialOrder(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return false;

        try
        {
            if (Game1.player.team.SpecialOrderActive(orderId) || Game1.player.team.completedSpecialOrders.Contains(orderId))
                return false;

            Game1.player.team.AddSpecialOrder(orderId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool RemoveSpecialOrder(string orderId)
    {
        if (string.IsNullOrWhiteSpace(orderId))
            return false;

        var removed = false;

        try
        {
            removed |= Game1.player.team.specialOrders.RemoveWhere(o => string.Equals(o.questKey.Value, orderId, StringComparison.Ordinal)) > 0;
        }
        catch
        {
            // ignored
        }

        return removed;
    }
}

