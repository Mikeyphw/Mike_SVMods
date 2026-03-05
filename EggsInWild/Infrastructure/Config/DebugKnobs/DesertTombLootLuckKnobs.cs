#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class DesertTombLootLuckKnobs
{
    public float UnluckyDelta { get; set; } = -0.001f;
    public float LuckyDelta { get; set; } = 0.001f;
    public float SuperLuckyDelta { get; set; } = 0.01f;

    public float SuperUnluckyMax { get; set; } = -0.07f;
    public float UnluckyMax { get; set; } = -0.02f;
    public float NeutralMax { get; set; } = 0.02f;
    public float LuckyMax { get; set; } = 0.07f;
}
#endif

