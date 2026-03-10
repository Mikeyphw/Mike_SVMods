#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class PharaohKnobs
{
    public int TeleportAnimStartFrame { get; set; } = 32;
    public int TeleportAnimFrames { get; set; } = 10;
    public int TeleportAnimIntervalMs { get; set; } = 70;
    public int VisionTiles { get; set; } = 8;
    public int CastRangeTiles { get; set; } = 8;
    public int RewrapRangeTiles { get; set; } = 6;

    public int BaseCastCooldownMs { get; set; } = 2600;
    public int EnragedCastCooldownMs { get; set; } = 1700;
    public int CastWindupMs { get; set; } = 800;
    public int RecoverMs { get; set; } = 450;

    public int MicroUnstuckAfterTicks { get; set; } = 15;
    public int PathfindUnstuckAfterTicks { get; set; } = 35;

    public int SandStepAfterTicks { get; set; } = 65;
    public int SandStepCooldownMs { get; set; } = 6500;

    public int LeashRadiusTiles { get; set; } = 12;
    public int LeashTicksBeforeReturn { get; set; } = 180;

    public float RewrapChance { get; set; } = 0.45f;
    public int EnrageCooldownCapMs { get; set; } = 800;

    public int ReviveTimerMax { get; set; } = 600;
    public int HealEnraged { get; set; } = 180;
    public int HealNormal { get; set; } = 80;
}
#endif
