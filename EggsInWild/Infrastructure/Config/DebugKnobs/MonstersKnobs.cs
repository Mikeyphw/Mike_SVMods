#if WEGG_DEV
namespace EggsInWild.Infrastructure.Config;

public sealed class MonstersKnobs
{
    public PharaohKnobs Pharaoh { get; set; } = new();
    public SandiKnobs Sandi { get; set; } = new();
    public CrawlerKnobs Crawler { get; set; } = new();
    public HurricaneKnobs Hurricane { get; set; } = new();
    public MatangoKnobs Matango { get; set; } = new();
}
#endif

