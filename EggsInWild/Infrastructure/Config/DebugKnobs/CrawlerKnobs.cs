#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class CrawlerKnobs
{
    public int CurlTriggerDistanceTiles { get; set; } = 3;
    public int CurlDurationMs { get; set; } = 1800;
    public int CurlCooldownMs { get; set; } = 3500;
}
#endif

