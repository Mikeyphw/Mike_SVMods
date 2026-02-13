using StardewValley;
using StardewValley.GameData.FarmAnimals;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.ItemIds;

namespace Mikew.BirdsOfAFeather.Helpers
{
	public class WeightedRandomPicker<T>
	{
		private Dictionary<T, float> _weights = new();
		private float _totalWeight = 0;
		private T? _fallbackItem;
		private bool _useInverseWeights;

		public void AddItem(T item, float weight)
		{
			_weights.Add(item, weight);
			_totalWeight += weight;
		}

		public void AddItems(IEnumerable<(T item, float weight)> items)
		{
			foreach (var (item, weight) in items)
			{
				AddItem(item, weight);
			}
		}

		public WeightedRandomPicker(T? fallbackItem = default, bool useInverseWeights = false)
		{
			_fallbackItem = fallbackItem;
			_useInverseWeights = useInverseWeights;
		}

		public T GetRandomItem()
		{
			if (_totalWeight > 0)
			{
				float randomNumber = Game1.random.NextSingle() * _totalWeight;
				foreach (var weight in _weights)
				{
					if (_useInverseWeights)
					{
						// Use 1 divided by the weight, with epsilon to avoid division by zero
						float inverseWeight = 1f / (weight.Value + float.Epsilon);
						if (randomNumber < inverseWeight)
						{
							return weight.Key;
						}
						randomNumber -= inverseWeight;
					}
					else
					{
						// Original behavior: use the weight directly
						if (randomNumber < weight.Value)
						{
							return weight.Key;
						}
						randomNumber -= weight.Value;
					}
				}
			}
			else
			{
				return _fallbackItem ?? GetFallbackItem();
			}
			return _fallbackItem ?? GetFallbackItem();
		}

		private T GetFallbackItem()
		{
			var items = _weights.Keys.ToList();
			var index = Game1.random.Next(items.Count);
			var fallbackItem = items[index];
			ModEntry.MonitorInstance?.Log($"Returning fallback item: {fallbackItem}");
			return items[index];
		}
	} 
}
  