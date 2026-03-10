#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class MatangoKnobs
{
    public int FrameWidth { get; set; } = 16;
    public int FrameHeight { get; set; } = 16;
    public int VisionTiles { get; set; } = 8;
    public int SpellCooldownMs { get; set; } = 1500;
    public int CastWindupMs { get; set; } = 500;
    public int TileCenterTolerancePx { get; set; } = 10;
    public int RecoverMs { get; set; } = 250;
    public int ShotCooldownMs { get; set; } = 2200;
}
#endif
