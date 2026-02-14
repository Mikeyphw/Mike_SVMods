using StardewValley;

namespace WildEggsHatching.Progression.Rules;

/// <summary>
/// Rule that requires a specific quest to be completed.
/// </summary>
public sealed class QuestCompletedRule : IUnlockRule
{
    /// <summary>The ID of the required quest.</summary>
    private readonly string _questId;

    /// <summary>
    /// Creates a new quest completion rule.
    /// </summary>
    /// <param name="questId">The ID of the quest that must be completed.</param>
    public QuestCompletedRule(string questId)
    {
        _questId = questId ?? throw new System.ArgumentNullException(nameof(questId));
    }

    /// <summary>
    /// Creates a rule for a completed special order.
    /// </summary>
    public static QuestCompletedRule SpecialOrder(string orderName) => new($"SpecialOrder_{orderName}");

    /// <summary>
    /// Creates a rule for a completed mail quest.
    /// </summary>
    public static QuestCompletedRule MailQuest(string mailId) => new($"MailQuest_{mailId}");

    /// <summary>
    /// Evaluates whether the quest has been completed.
    /// </summary>
    public bool IsSatisfied(UnlockContext context)
    {
        return context.HasCompletedQuest(_questId);
    }

    /// <summary>
    /// Gets a human-readable description of this rule.
    /// </summary>
    public string Description => $"Quest '{_questId}' must be completed";

    /// <summary>
    /// Gets the priority of this rule.
    /// </summary>
    public int Priority => 20; // Medium priority - quest gating

    /// <summary>
    /// Returns a string representation of this rule.
    /// </summary>
    public override string ToString()
    {
        return $"QuestCompletedRule({_questId})";
    }
}

