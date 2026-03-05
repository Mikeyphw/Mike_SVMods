using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EggsInWild.Infrastructure.Diagnostics;

public class WeightRowSet : IReadOnlyList<WeightRow>
{
    private readonly List<WeightRow> _rows;

    public WeightRowSet(List<WeightRow> rows, double medianRaw, double minClamp, double maxClamp)
    {
        _rows = rows;
        MedianRaw = medianRaw;
        MinClamp = minClamp;
        MaxClamp = maxClamp;
    }

    public double MedianRaw { get; }
    public double MinClamp { get; }
    public double MaxClamp { get; }

    public int Count => _rows.Count;
    public WeightRow this[int index] => _rows[index];

    public IEnumerator<WeightRow> GetEnumerator() => _rows.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _rows.GetEnumerator();
}

