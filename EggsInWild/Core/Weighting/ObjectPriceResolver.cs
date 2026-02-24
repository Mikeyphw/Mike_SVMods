using System;
using System.Reflection;
using StardewValley.GameData.Objects;

namespace EggsInWild.Core.Weighting;

internal static class ObjectPriceResolver
{
    public static bool TryGetSellPrice(ObjectData obj, out int price)
    {
        price = 0;

        if (obj is null)
            return false;

        foreach (var name in new[] { "Price", "SellPrice", "sellPrice" })
        {
            var prop = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (prop is null)
                continue;

            var v = prop.GetValue(obj);
            if (v is int i)
            {
                price = i;
                return true;
            }

            if (v is long l)
            {
                price = (int)l;
                return true;
            }
        }

        return false;
    }
}
