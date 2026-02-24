namespace EggsInWild.DesertTomb.Core.Model;

/// <summary>
/// A tomb layout is a set of areas for a given mine level.
/// </summary>
public sealed class TombLayout
{
    public IReadOnlyList<TombAreaLayout> Areas { get; }

    public TombLayout(IReadOnlyList<TombAreaLayout> areas)
    {
        Areas = areas;
    }
}
