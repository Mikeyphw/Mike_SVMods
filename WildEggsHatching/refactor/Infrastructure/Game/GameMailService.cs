using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace WildEggsHatching.Infrastructure.Game;

/// <summary>
/// Implementation of IGameMailService that directly modifies game mail/quest state.
/// </summary>
public sealed class GameMailService : IGameMailService
{
    /// <inheritdoc />
    public void SendMail(string mailId, int daysDelay = 0, bool fromMailbox = true)
    {
        try
        {
            if (daysDelay > 0)
            {
                // Queue mail for future delivery
                Game1.mailbox.Add(mailId);
            }
            else
            {
                // Immediate delivery
                Game1.player.mailForTomorrow.Add(mailId);
            }
        }
        catch (Exception ex)
        {
            ModEntry.LoggerHelper.Log(
                $"Error sending mail {mailId}: {ex.Message}",
                LogLevel.Error
            );
        }
    }

    /// <inheritdoc />
    public void AddMailReceived(string mailId)
    {
        try
        {
            if (!Game1.player.mailReceived.Contains(mailId))
            {
                Game1.player.mailReceived.Add(mailId);
            }
        }
        catch (Exception ex)
        {
            ModEntry.LoggerHelper.Log(
                $"Error adding mail received {mailId}: {ex.Message}",
                LogLevel.Error
            );
        }
    }

    /// <inheritdoc />
    public void RemoveMailReceived(string mailId)
    {
        try
        {
            Game1.player.mailReceived.Remove(mailId);
        }
        catch (Exception ex)
        {
            ModEntry.LoggerHelper.Log(
                $"Error removing mail received {mailId}: {ex.Message}",
                LogLevel.Error
            );
        }
    }

    /// <inheritdoc />
    public bool HasMail(string mailId)
    {
        return Game1.player.mailReceived.Contains(mailId);
    }

    /// <inheritdoc />
    public void AddSpecialOrder(string specialOrderId)
    {
        try
        {
            // Special orders are added through the SpecialOrder API
            // This is a simplified implementation
            var specialOrder = SpecialOrder.GetSpecialOrder(specialOrderId, null);
            if (specialOrder != null)
            {
                Game1.player.specialOrders.Add(specialOrder);
            }
        }
        catch (Exception ex)
        {
            ModEntry.LoggerHelper.Log(
                $"Error adding special order {specialOrderId}: {ex.Message}",
                LogLevel.Error
            );
        }
    }

    /// <inheritdoc />
    public void RemoveSpecialOrder(string specialOrderId)
    {
        try
        {
            var orderToRemove = Game1.player.specialOrders
                .FirstOrDefault(o => o.questKey.Value == specialOrderId);
            if (orderToRemove != null)
            {
                Game1.player.specialOrders.Remove(orderToRemove);
            }
        }
        catch (Exception ex)
        {
            ModEntry.LoggerHelper.Log(
                $"Error removing special order {specialOrderId}: {ex.Message}",
                LogLevel.Error
            );
        }
    }

    /// <inheritdoc />
    public void AddQuest(string questId)
    {
        try
        {
            // Check if quest is already in the quest log
            if (Game1.player.questLog.Any(q => q.id.Value == questId))
                return;

            var quest = Quest.GetQuest(questId);
            if (quest != null)
            {
                Game1.player.questLog.Add(quest);
            }
        }
        catch (Exception ex)
        {
            ModEntry.LoggerHelper.Log(
                $"Error adding quest {questId}: {ex.Message}",
                LogLevel.Error
            );
        }
    }

    /// <inheritdoc />
    public void RemoveQuest(string questId)
    {
        try
        {
            var questToRemove = Game1.player.questLog
                .FirstOrDefault(q => q.id.Value == questId);
            if (questToRemove != null)
            {
                Game1.player.questLog.Remove(questToRemove);
            }
        }
        catch (Exception ex)
        {
            ModEntry.LoggerHelper.Log(
                $"Error removing quest {questId}: {ex.Message}",
                LogLevel.Error
            );
        }
    }

    /// <inheritdoc />
    public void CompleteQuest(string questId)
    {
        try
        {
            var quest = Game1.player.questLog
                .FirstOrDefault(q => q.id.Value == questId);
            if (quest != null)
            {
                quest.complete.Value = true;
            }
        }
        catch (Exception ex)
        {
            ModEntry.LoggerHelper.Log(
                $"Error completing quest {questId}: {ex.Message}",
                LogLevel.Error
            );
        }
    }

    /// <inheritdoc />
    public bool HasQuest(string questId)
    {
        return Game1.player.hasQuest(questId);
    }

    /// <inheritdoc />
    public bool IsQuestCompleted(string questId)
    {
        return Game1.player.questLog
            .Any(q => q.id.Value == questId && q.completed.Value);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetReceivedMail()
    {
        return Game1.player.mailReceived;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetActiveQuests()
    {
        return Game1.player.questLog
            .Where(q => !q.completed.Value)
            .Select(q => q.id.Value)
            .ToList();
    }
}

