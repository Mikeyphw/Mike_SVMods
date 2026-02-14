using System.Collections.Generic;

namespace WildEggsHatching.Infrastructure.Game;

/// <summary>
/// Provides methods for sending mail, quests, and special orders.
/// All mail-related side effects should go through this interface.
/// </summary>
public interface IGameMailService
{
    /// <summary>
    /// Adds a mail letter to the player's mailbox.
    /// </summary>
    /// <param name="mailId">The mail ID to send.</param>
    /// <param name="daysDelay">Days to wait before delivery (0 = immediate).</param>
    /// <param name="fromMailbox">Whether to show in mailbox (vs. directly in mailbox).</param>
    void SendMail(string mailId, int daysDelay = 0, bool fromMailbox = true);

    /// <summary>
    /// Adds mail directly to received list (no mailbox delay).
    /// </summary>
    /// <param name="mailId">The mail ID to add.</param>
    void AddMailReceived(string mailId);

    /// <summary>
    /// Removes mail from received list.
    /// </summary>
    /// <param name="mailId">The mail ID to remove.</param>
    void RemoveMailReceived(string mailId);

    /// <summary>
    /// Checks if player has received specific mail.
    /// </summary>
    /// <param name="mailId">The mail ID to check.</param>
    /// <returns>True if mail has been received.</returns>
    bool HasMail(string mailId);

    /// <summary>
    /// Adds a special order for the player.
    /// </summary>
    /// <param name="specialOrderId">The special order ID to add.</param>
    void AddSpecialOrder(string specialOrderId);

    /// <summary>
    /// Removes a special order from the player.
    /// </summary>
    /// <param name="specialOrderId">The special order ID to remove.</param>
    void RemoveSpecialOrder(string specialOrderId);

    /// <summary>
    /// Adds a quest to the player's quest log.
    /// </summary>
    /// <param name="questId">The quest ID to add.</param>
    void AddQuest(string questId);

    /// <summary>
    /// Removes a quest from the player's quest log.
    /// </summary>
    /// <param name="questId">The quest ID to remove.</param>
    void RemoveQuest(string questId);

    /// <summary>
    /// Marks a quest as completed.
    /// </summary>
    /// <param name="questId">The quest ID to complete.</param>
    void CompleteQuest(string questId);

    /// <summary>
    /// Checks if player has a specific quest.
    /// </summary>
    /// <param name="questId">The quest ID to check.</param>
    /// <returns>True if player has the quest.</returns>
    bool HasQuest(string questId);

    /// <summary>
    /// Checks if a specific quest is completed.
    /// </summary>
    /// <param name="questId">The quest ID to check.</param>
    /// <returns>True if the quest is completed.</returns>
    bool IsQuestCompleted(string questId);

    /// <summary>
    /// Gets all mail IDs that have been received.
    /// </summary>
    /// <returns>Collection of received mail IDs.</returns>
    IReadOnlyCollection<string> GetReceivedMail();

    /// <summary>
    /// Gets all active quest IDs.
    /// </summary>
    /// <returns>Collection of active quest IDs.</returns>
    IReadOnlyCollection<string> GetActiveQuests();
}

