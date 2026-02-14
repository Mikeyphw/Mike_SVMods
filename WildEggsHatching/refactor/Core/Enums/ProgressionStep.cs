namespace WildEggsHatching.Core.Models;

/// <summary>
/// Represents the 6-step progression system for wild eggs.
/// Each step unlocks new content and mechanics.
/// </summary>
public enum ProgressionStep
{
    /// <summary>
    /// Initial step. All basic functionality is available.
    /// No special unlocks required.
    /// </summary>
    PartI,

    /// <summary>
    /// Second step. Unlocked after completing Special Order I.
    /// Marnie sends a thank-you mail.
    /// </summary>
    PartII,

    /// <summary>
    /// Third step. Unlocked 2 days after completing the Wizard's study quest.
    /// Wizard sends completion mail.
    /// </summary>
    PartIII,

    /// <summary>
    /// Fourth step. Unlocked 3 days after completing Special Order II.
    /// Wizard sends a thanks mail.
    /// </summary>
    PartIV,

    /// <summary>
    /// Fifth step. Unlocked 3 days after receiving the Desert Scroll.
    /// Wizard sends findings mail.
    /// </summary>
    PartV,

    /// <summary>
    /// Final step. Unlocked 5 days after receiving the Desert Scroll.
    /// Marnie sends incubator recipe.
    /// </summary>
    PartVI
}


