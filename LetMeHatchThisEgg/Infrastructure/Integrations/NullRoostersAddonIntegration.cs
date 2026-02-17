using LetMeHatchThisEgg.Core.Abstractions.Integrations;
using LetMeHatchThisEgg.Framework;

namespace LetMeHatchThisEgg.Infrastructure.Integrations;

/// <summary>Null-object Roosters Addon integration adapter.</summary>
public sealed class NullRoostersAddonIntegration : IRoostersAddonIntegration
{
    public static readonly NullRoostersAddonIntegration Instance = new();

    public bool IsAvailable => false;

    public IRoosterHatchingAPI? Api => null;

    private NullRoostersAddonIntegration() { }
}
