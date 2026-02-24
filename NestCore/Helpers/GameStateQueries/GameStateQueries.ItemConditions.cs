namespace Mikew.NestCore.Helpers.GameStateQueries;

public static class ItemCondition
{
    /// <summary>
    /// For items only. These queries apply in cases where there's an item (e.g. machine recipes, shops, etc); 
    /// they'll return false if not applicable. They take a &lt;type&gt; argument which can be Input (the machine input item) 
    /// or Target (the machine output, tree fruit, shop item, etc).
    /// </summary>

    /// <summary>
    /// Whether the item has a non-zero category.
    /// </summary>
    /// <param name="target">The type of item to check (Input or Target)</param>
    /// <param name="categories">The categories to check (optional)</param>
    public static string ITEM_CATEGORY(Type target, params int[] categories)
    {
        return $"ITEM_CATEGORY {target} {string.Join(" ", categories)}";
    }

    /// <summary>
    /// Whether the item has all of the given space-delimited tags.
    /// </summary>
    /// <param name="target">The type of item to check (Input or Target)</param>
    /// <param name="tags">The tags to check</param>
    public static string ITEM_CONTEXT_TAG(Type target, params string[] tags)
    {
        return $"ITEM_CONTEXT_TAG {target} {string.Join(" ", tags)}";
    }

    /// <summary>
    /// Whether the item's edibility is between [min] (default -299) and [max] (default unlimited) inclusively.
    /// A value of -300 is inedible, so ITEM_EDIBILITY &lt;target&gt; without min/max values checks if the item is edible.
    /// </summary>
    /// <param name="target">The type of item to check (Input or Target)</param>
    /// <param name="min">The minimum edibility (default -299)</param>
    /// <param name="max">The maximum edibility (default unlimited)</param>
    public static string ITEM_EDIBILITY(Type target, int min = -299, int max = int.MaxValue)
    {
        return $"ITEM_EDIBILITY {target} {min} {max}";
    }

    /// <summary>
    /// Whether the item has one of the given qualified or unqualified item IDs.
    /// </summary>
    /// <param name="target">The type of item to check (Input or Target)</param>
    /// <param name="itemIds">The item IDs to check</param>
    public static string ITEM_ID(Type target, params int[] itemIds)
    {
        return$"ITEM_ID {target} {string.Join(" ", itemIds)}";
    }

    /// <summary>
    /// Whether the item's qualified or unqualified item ID starts with the given prefix.
    /// </summary>
    /// <param name="target">The type of item to check (Input or Target)</param>
    /// <param name="prefix">The prefix to check</param>
    public static string ITEM_ID_PREFIX(Type target, string prefix)
    {
        return $"ITEM_ID_PREFIX {target} {prefix}";
    }

    /// <summary>
    /// Whether the item has a numeric unqualified item ID which is between [min] and [max] (both defaulting to the lowest and highest possible value).
    /// </summary>
    /// <param name="target">The type of item to check (Input or Target)</param>
    /// <param name="min">The minimum item ID (default lowest possible value)</param>
    /// <param name="max">The maximum item ID (default highest possible value)</param>
    public static string ITEM_NUMERIC_ID(Type target, int min = int.MinValue, int max = int.MaxValue)
    {
        return $"ITEM_NUMERIC_ID {target} {min} {max}";
    }

    /// <summary>
    /// Whether the item has one of the given object types.
    /// </summary>
    /// <param name="target">The type of item to check (Input or Target)</param>
    /// <param name="objectTypes">The object types to check</param>
    public static string ITEM_OBJECT_TYPE(Type target, params string[] objectTypes)
    {
        return $"ITEM_OBJECT_TYPE {target} {string.Join(" ", objectTypes)}";
    }

    /// <summary>
    /// Whether the item has a default purchase-from-shop price between &lt;min&gt; and [max] (default highest possible value).
    /// This checks the base sell price (including profit margins, but excluding custom shop data like custom prices, price modifiers, out-of-season pricing, etc).
    /// </summary>
    /// <param name="target">The type of item to check (Input or Target)</param>
    /// <param name="min">The minimum price</param>
    /// <param name="max">The maximum price (default highest possiblevalue)</param>
    public static string ITEM_PRICE(Type target, int min, int max = int.MaxValue)
    {
        return $"ITEM_PRICE {target} {min} {max}";
    }

    /// <summary>
    /// Whether the item's quality is between &lt;min&gt; and [max] (default unlimited) inclusively.
    /// The possible values are 0 (normal), 1 (silver), 2 (gold), or 4 (iridium).
    /// </summary>
    /// <param name="target">The type of item to check (Input or Target)</param>
    /// <param name="min">The minimum quality (default unlimited)</param>
    /// <param name="max">The maximum quality (default unlimited)</param>
    public static string ITEM_QUALITY(Type target, int min, int max = int.MaxValue)
    {
        return $"ITEM_QUALITY {target} {min} {max}";
    }

    /// <summary>
    /// Whether the item stack size is between &lt;min&gt; and [max] (default unlimited) inclusively.
    /// Note that this only applies to the target item, it doesn't include other stacks in the inventory.
    /// </summary>
    /// <param name="target">The type of item to check (Input or Target)</param>
    /// <param name="min">The minimum stack size (default unlimited)</param>
    /// <param name="max">The maximum stack size (default unlimited)</param>
    public static string ITEM_STACK(Type target, int min, int max = int.MaxValue)
    {
        return $"ITEM_STACK {target} {min} {max}";
    }

    /// <summary>
    /// Whether the item's type definition ID matches one of the given values.
    /// For example, ITEM_TYPE Target (BC) matches bigcraftables.
    /// </summary>
    /// <param name="target">The type of item to check (Input or Target)</param>
    /// <param name="types">The type definition IDs to check</param>
    public static string ITEM_TYPE(Type target, params string[] types)
    {
        return $"ITEM_TYPE {target} {string.Join(" ", types)}";
    }

    /// <summary>
    /// (Specialized) Whether the item has an explicit category set in Data/Objects, ignoring categories assigned dynamically in code (e.g. for rings).
    /// Items without an explicit category are often (but not always) special items like secret notes or unimplemented items.
    /// </summary>
    /// <param name="target">The type of item to check (Input or Target)</param>
    public static string ITEM_HAS_EXPLICIT_OBJECT_CATEGORY(Type target)
    {
        return $"ITEM_HAS_EXPLICIT_OBJECT_CATEGORY {target}";
    }
}

/// <summary>
/// The type of item to check (Input or Target)
/// </summary>
public enum Type
{
    Input,
    Target
}