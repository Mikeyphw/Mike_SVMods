using ContentPatcher;
using LetMeHatchThisEgg.Framework;

namespace LetMeHatchThisEgg.Infrastructure.Runtime;

/// <summary>Optional integration APIs that may or may not be available at runtime.</summary>
public sealed record LmheIntegrations(
    IContentPatcherAPI? ContentPatcher,
    IWildEggsApi? WildEggs,
    IRoosterHatchingAPI? RoostersAddon
);
