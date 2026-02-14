namespace WildEggsHatching.Core.Enums;

/// <summary>
/// Represents the daily luck state from the game.
/// Used to influence spawn chances.
/// </summary>
public enum LuckyTypes
{
    /// <summary>Super unlucky luck state.</summary>
    SuperUnlucky = -2,

    /// <summary>Standard unlucky luck state.</summary>
    Unlucky = -1,

    /// <summary>Neutral (default) luck state.</summary>
    Neutral = 0,

    /// <summary>Standard lucky luck state.</summary>
    Lucky = 1,

    /// <summary>Super lucky luck state.</summary>
    SuperLucky = 2
}


