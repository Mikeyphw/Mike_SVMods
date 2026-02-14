using System;
using System.Collections.Generic;
using RoosterAddon.Enums;

namespace RoosterAddon.Helpers
{
    public class WeightedRandomPicker
    {
        private static Random? _random { get; set; }
        private Dictionary<Dictionary<RoosterItemDataEntries, string>, int> _weights = new();
        private int _totalWeight = 0;

        public void AddItem(Dictionary<RoosterItemDataEntries, string> produceNumber)
        {
            int weight = Int32.Parse(produceNumber[RoosterItemDataEntries.Probability]);
            _weights.Add(produceNumber, weight);
            _totalWeight += weight; // Keep track of the total weight
        }

        public WeightedRandomPicker(Random rnd)
        {
            _random = rnd;
        }

        public Dictionary<RoosterItemDataEntries, string> GetRandomItem()
        {
            //var chosenAnimal = new Dictionary<RoosterItemDataEntries, string>();
            //if (_totalWeight > 0)
            //{
                int randomNumber = _random!.Next(0, _totalWeight);
                foreach (var weight in _weights)
                {
                    if (randomNumber < weight.Value)
                    {
                        return weight.Key;
                    }

                    randomNumber -= weight.Value;
                }
                return new Dictionary<RoosterItemDataEntries, string>();
            //}
        }
    }
}
