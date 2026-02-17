using ContentPatcher;

namespace LetMeHatchThisEgg.Core.Abstractions.Integrations;

/// <summary>Adapter for Content Patcher integration.</summary>
public interface IContentPatcherIntegration
{
    /// <summary>Whether the integration API is available.</summary>
    bool IsAvailable { get; }

    /// <summary>The underlying Content Patcher API if available; otherwise <see langword="null"/>.</summary>
    IContentPatcherAPI? Api { get; }
}
