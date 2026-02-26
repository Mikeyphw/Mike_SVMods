using System;
using System.Collections;
using System.Collections.Generic;

namespace LetMeHatch.Core.ClonePipeline;

/// <summary>
/// Immutable set of clone specifications.
/// </summary>
public sealed class ClonePlan : IReadOnlyList<CloneSpec>
{
    private readonly IReadOnlyList<CloneSpec> _items;

    public ClonePlan(IReadOnlyList<CloneSpec> items)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    public int Count => _items.Count;
    public CloneSpec this[int index] => _items[index];

    public IEnumerator<CloneSpec> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}
