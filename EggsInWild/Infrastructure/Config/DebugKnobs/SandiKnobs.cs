#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class SandiKnobs
{
    public float DiveChanceOnHit { get; set; } = 0.18f;
    public int DiveDurationMs { get; set; } = 2200;
    public int DiveCooldownMs { get; set; } = 4200;
}
#endif

