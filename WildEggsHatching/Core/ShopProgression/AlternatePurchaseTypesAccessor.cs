using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using StardewValley.GameData.FarmAnimals;

namespace WildEggsHatching.Core.ShopProgression;

/// <summary>
/// Reflection-based helpers for <c>FarmAnimalData.AlternatePurchaseTypes</c>,
/// which varies slightly across game versions.
/// </summary>
public static class AlternatePurchaseTypesAccessor
{
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    public sealed record AltEntry(object AltObject, IList<string> AnimalIds, string? Id, string? Condition);

    public static IReadOnlyList<AltEntry> Read(FarmAnimalData parent)
    {
        var list = new List<AltEntry>();
        if (parent is null)
            return list;

        var prop = parent.GetType().GetProperty("AlternatePurchaseTypes", Flags);
        if (prop?.GetValue(parent) is not IEnumerable alternates)
            return list;

        foreach (var alt in alternates)
        {
            if (alt is null)
                continue;

            var ids = TryGetAnimalIds(alt);
            if (ids is null)
                continue;

            var id = TryGetString(alt, "Id");
            var cond = TryGetString(alt, "Condition");
            list.Add(new AltEntry(alt, ids, id, cond));
        }

        return list;
    }

    public static void RemoveAnimalIds(FarmAnimalData parent, ISet<string> idsToRemove)
    {
        if (parent is null || idsToRemove is null || idsToRemove.Count == 0)
            return;

        foreach (var entry in Read(parent))
        {
            for (var i = entry.AnimalIds.Count - 1; i >= 0; i--)
            {
                var id = entry.AnimalIds[i];
                if (id is not null && idsToRemove.Contains(id))
                    entry.AnimalIds.RemoveAt(i);
            }
        }
    }

    public static bool ContainsAnimalId(FarmAnimalData parent, string childId)
    {
        if (parent is null || string.IsNullOrWhiteSpace(childId))
            return false;

        foreach (var entry in Read(parent))
        {
            foreach (var id in entry.AnimalIds)
            {
                if (id is not null && childId.Equals(id, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }



public static void SetCondition(AltEntry entry, string? condition)
{
    if (entry is null)
        return;

    var p = entry.AltObject.GetType().GetProperty("Condition", Flags);
    if (p is not null && p.CanWrite)
        p.SetValue(entry.AltObject, condition);
}

public static void SetId(AltEntry entry, string? id)
{
    if (entry is null)
        return;

    var p = entry.AltObject.GetType().GetProperty("Id", Flags);
    if (p is not null && p.CanWrite)
        p.SetValue(entry.AltObject, id);
}

public static bool TryInsertAfter(
    FarmAnimalData parent,
    AltEntry existing,
    IEnumerable<string> animalIds,
    string? id,
    string? condition)
{
    if (parent is null || existing is null)
        return false;

    var listProp = parent.GetType().GetProperty("AlternatePurchaseTypes", Flags);
    if (listProp?.GetValue(parent) is not IList list)
        return false;

    var altType = existing.AltObject.GetType();
    object? created;
    try
    {
        created = Activator.CreateInstance(altType);
    }
    catch
    {
        return false;
    }

    if (created is null)
        return false;

    if (!TrySetAnimalIds(created, animalIds))
        return false;

    if (!TrySetString(created, "Id", id))
        return false;

    if (!TrySetString(created, "Condition", condition))
        return false;

    var idx = -1;
    for (var i = 0; i < list.Count; i++)
    {
        if (ReferenceEquals(list[i], existing.AltObject))
        {
            idx = i;
            break;
        }
    }

    if (idx < 0)
    {
        list.Add(created);
        return true;
    }

    list.Insert(Math.Min(idx + 1, list.Count), created);
    return true;
}

public static bool TryRemoveAlternate(FarmAnimalData parent, AltEntry entry)
{
    if (parent is null || entry is null)
        return false;

    var listProp = parent.GetType().GetProperty("AlternatePurchaseTypes", Flags);
    if (listProp?.GetValue(parent) is not IList list)
        return false;

    for (var i = 0; i < list.Count; i++)
    {
        if (ReferenceEquals(list[i], entry.AltObject))
        {
            list.RemoveAt(i);
            return true;
        }
    }

    return false;
}

private static bool TrySetAnimalIds(object alt, IEnumerable<string> ids)
{
    var t = alt.GetType();
    var p = t.GetProperty("AnimalIds", Flags) ?? t.GetProperty("AnimalIDs", Flags);
    if (p is null || !p.CanWrite)
        return false;

    var list = new List<string>();
    foreach (var s in ids ?? Array.Empty<string>())
    {
        if (!string.IsNullOrWhiteSpace(s))
            list.Add(s);
    }

    try
    {
        p.SetValue(alt, list);
        return true;
    }
    catch
    {
        return false;
    }
}

private static bool TrySetString(object alt, string propName, string? value)
{
    var p = alt.GetType().GetProperty(propName, Flags);
    if (p is null || !p.CanWrite)
        return false;

    try
    {
        p.SetValue(alt, value);
        return true;
    }
    catch
    {
        return false;
    }
}
    private static IList<string>? TryGetAnimalIds(object alt)
    {
        var t = alt.GetType();
        var p = t.GetProperty("AnimalIds", Flags) ?? t.GetProperty("AnimalIDs", Flags);
        if (p?.GetValue(alt) is IList<string> ids)
            return ids;

        if (p?.GetValue(alt) is IList raw)
        {
            var list = new List<string>();
            foreach (var v in raw)
                if (v is string s)
                    list.Add(s);

            // Can't write back safely if it's not IList<string>.
            return null;
        }

        return null;
    }

    private static string? TryGetString(object obj, string propName)
        => obj.GetType().GetProperty(propName, Flags)?.GetValue(obj) as string;
}
