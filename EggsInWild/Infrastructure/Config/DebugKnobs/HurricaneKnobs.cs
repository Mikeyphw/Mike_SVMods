#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class HurricaneKnobs
{
    public int AlignTiles { get; set; } = 10;
    public int SuckDistanceTiles { get; set; } = 2;

    public int SwirlSegmentMs { get; set; } = 600;
    public int ChargeMs { get; set; } = 900;
    public int SuckMs { get; set; } = 1600;
}
#endif

