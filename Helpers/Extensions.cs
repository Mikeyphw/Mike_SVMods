using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StardewValley.GameData.FarmAnimals;

namespace Mikew.BirdsOfAFeather.Helpers
{
    public static class StringArrayExtensions
    {
        public static bool ContainsCI(this string[] array, string? value)
        {
            if (value == null)
            {
                return false;
            }
            return array.Any(s => String.Equals(s, value, StringComparison.OrdinalIgnoreCase));
        }
    }

    public static class StringExtensions
    {
        public static bool IsEmptyOrWhitespace(this string? input)
        {
            return input == null ? false : string.IsNullOrEmpty(input.Trim());
        }
    }

    public static class EnumExtensions
    {
        /// <summary>
        /// Gets all values of the specified enum type as an enumerable collection.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <returns>An enumerable collection of all values in the enum.</returns>

        public static IEnumerable<TEnum> GetValues<TEnum>() where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
        }
    }
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> AddIf<T>(this IEnumerable<T> source, T element, Func<T, bool> condition)
        {
            // Check if the condition is met

            if (condition(element))
            {
                // Return the original enumerable plus the new element
                return source.Concat(new[] { element });

            }

            // Return the original enumerable unchanged
            return source;

        }
    }
    public static class ListExtensions
    {
        public static T GetRandomElementOrDefault<T>(this List<T> list, Random random)
        {
            if (list == null || list.Count == 0)
            {
                return default(T);
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            return list[random.Next(list.Count)];
        }
        public static bool TryAdd<T>(this List<T> list, T item)
        {
            list ??= new();

            if (!list.Contains(item))
            {
                list.Add(item);
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public static class WeightedRandomExtensions
    {
        public static T GetRandomItem<T>(
            this IEnumerable<T> collection,
            Func<T, float> weightSelector,
            T? fallbackItem = default,
            bool useInverseWeights = false
        )
        {
            var picker = new WeightedRandomPicker<T>(fallbackItem, useInverseWeights);

            foreach (var item in collection)
            {
                picker.AddItem(item, weightSelector(item));
            }

            return picker.GetRandomItem();
        }

        public static T GetRandomItem<T>(
            this IEnumerable<(T item, float weight)> weightedCollection,
            T? fallbackItem = default,
            bool useInverseWeights = false
        )
        {
            var picker = new WeightedRandomPicker<T>(fallbackItem, useInverseWeights);

            picker.AddItems(weightedCollection);

            return picker.GetRandomItem();
        }
    }

    public static class FarmAnimalDataDictionaryExtensions
    {
        public static KeyValuePair<string, FarmAnimalData>? GetOriginalPurchaseAnimal(
            this IDictionary<string, FarmAnimalData> animalData,
            string animal
        )
        {
            return animalData.FirstOrDefault(a =>
                a.Key != animal
                && a.Key != null
                && a.Value.AlternatePurchaseTypes != null
                && a.Value.AlternatePurchaseTypes.SelectMany(ap => ap.AnimalIds).Contains(animal)
            );
        }

        public static KeyValuePair<string, FarmAnimalData>? GetOriginalPurchaseAnimal(
            this Dictionary<string, FarmAnimalData> animalData,
            string animal
        )
        {
            return animalData.FirstOrDefault(a =>
                a.Key != animal
                && a.Key != null
                && a.Value.AlternatePurchaseTypes != null
                && a.Value.AlternatePurchaseTypes.SelectMany(ap => ap.AnimalIds).Contains(animal)
            );
        }
    }
}
