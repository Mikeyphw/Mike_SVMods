namespace WildEggsHatching.Progression.Rules;

/// <summary>
/// Rule that requires a quest to be completed AND a minimum number of days to have passed since.
/// </summary>
public sealed class QuestCompletedDaysRule : IUnlockRule
{
    /// <summary>The ID of the required quest.</summary>
    private readonly string _questId;

    /// <summary>The minimum number of days required after quest completion.</summary>
    private readonly int _daysRequired;

    /// <summary>
    /// Creates a new quest completion + days rule.
    /// </summary>
    /// <param name="questId">The ID of the quest that must be completed.</param>
    /// <param name="daysRequired">The minimum number of days that must pass after completion.</param>
    public QuestCompletedDaysRule(string questId, int daysRequired)
    {
        _questId = questId ?? throw new System.ArgumentNullException(nameof(questId));
        _daysRequired = daysRequired;
    }

    /// <summary>
    /// Creates a rule for a special order completed days ago.
    /// </summary>
    public static QuestCompletedDaysRule SpecialOrderDays(string orderName, int days) 
        => new($"SpecialOrder_{orderName}", days);

    /// <summary>
    /// Creates a rule for mail quest completed days ago.
    /// </summary>
    public static QuestCompletedDaysRule MailQuestDays(string mailId, int days) 
        => new($"MailQuest_{mailId}", days);

    /// <summary>
    /// Creates a rule for a wizard quest completed days ago.
    /// </summary>
    public static QuestCompletedDaysRule WizardQuestDays(string questId, int days) 
        => new(questId, days);

    /// <summary>
    /// Evaluates whether the quest was completed and enough days have passed.
    /// </summary>
    public bool IsSatisfied(UnlockContext context)
    {
        // First check if quest is completed
        if (!context.HasCompletedQuest(_questId))
        {
            return false;
        }

        // Then check if enough days have passed
        int daysSince = context.DaysSinceQuestCompletion(_questId);
        return daysSince >= _daysRequired;
    }

    /// <summary>
    /// Gets a human-readable description of this rule.
    /// </summary>
    public string Description => $"Quest '{_questId}' completed + {_daysRequired} days";

    /// <summary>
    /// Gets the priority of this rule.
    /// </summary>
    public int Priority => 15; // Between quest and pure day requirements

    /// <summary>
    /// Returns a string representation of this rule.
    /// </summary>
    public override string ToString()
    {
        return $"QuestCompletedDaysRule({_questId}, {_daysRequired} days)";
    }
}

