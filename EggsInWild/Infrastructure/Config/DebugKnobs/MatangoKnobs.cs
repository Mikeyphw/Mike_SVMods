#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class MatangoKnobs
{
    public int VisionTiles { get; set; } = 8;
    public int CastWindupMs { get; set; } = 450;
    public int RecoverMs { get; set; } = 250;
    public int ShotCooldownMs { get; set; } = 2200;
}
#endif

