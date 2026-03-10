#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class CrawlerKnobs
{
    public int CurlAnimIntervalMs { get; set; } = 100;
    public int FrameWidth { get; set; } = 24;
    public int FrameHeight { get; set; } = 32;
    public int CurlTriggerDistanceTiles { get; set; } = 3;
    public int CurledSpeedBonus { get; set; } = 2;
    public int DeathFrame { get; set; } = 20;
    public int DeathFrameLoops { get; set; } = 6;
    public int CurlHoldMs { get; set; } = 500;
    public int CurledHaltGraceMs { get; set; } = 250;
    public float MovementDetectThresholdSquared { get; set; } = 1f;
    public int CurlDurationMs { get; set; } = 1800;
    public int CurlCooldownMs { get; set; } = 3500;
}
#endif
