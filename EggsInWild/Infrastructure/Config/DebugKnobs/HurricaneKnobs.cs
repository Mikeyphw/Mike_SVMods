#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class HurricaneKnobs
{
    public int FrameWidth { get; set; } = 16;
    public int FrameHeight { get; set; } = 24;
    public int AlignTiles { get; set; } = 10;
    public int SuckDistanceTiles { get; set; } = 2;
    public int SpinAnimStartFrame { get; set; } = 17;
    public int SpinAnimFrames { get; set; } = 5;
    public int SuckLoopStartFrame { get; set; } = 20;
    public int SuckLoopFrames { get; set; } = 2;
    public int SpinAnimIntervalMs { get; set; } = 90;
    public int SuckLoopIntervalMs { get; set; } = 45;
    public int SuckMs { get; set; } = 1600;
    public int DeathAnimStartFrame { get; set; } = 24;
    public int DeathAnimFrames { get; set; } = 4;
    public int DefaultFacingFrame { get; set; } = 8;
    public int FacingFrameStride { get; set; } = 4;
    public int SuckLaneToleranceTiles { get; set; } = 1;
}
#endif
