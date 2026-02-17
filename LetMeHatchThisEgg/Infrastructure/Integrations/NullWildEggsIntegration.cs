using LetMeHatchThisEgg.Core.Abstractions.Integrations;
using LetMeHatchThisEgg.Framework;

namespace LetMeHatchThisEgg.Infrastructure.Integrations;

/// <summary>Null-object WildEggsHatching integration adapter.</summary>
public sealed class NullWildEggsIntegration : IWildEggsIntegration
{
    public static readonly NullWildEggsIntegration Instance = new();

    public bool IsAvailable => false;

    public IWildEggsApi? Api => null;

    private NullWildEggsIntegration() { }
}
