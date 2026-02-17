using ContentPatcher;
using LetMeHatchThisEgg.Core.Abstractions.Integrations;

namespace LetMeHatchThisEgg.Infrastructure.Integrations;

/// <summary>Null-object Content Patcher integration adapter.</summary>
public sealed class NullContentPatcherIntegration : IContentPatcherIntegration
{
    public static readonly NullContentPatcherIntegration Instance = new();

    public bool IsAvailable => false;

    public IContentPatcherAPI? Api => null;

    private NullContentPatcherIntegration() { }
}
