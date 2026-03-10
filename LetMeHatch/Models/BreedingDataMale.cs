using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using static LetMeHatch.EnumsAndStrings.Enums;

using static LetMeHatch.ModEntry;

namespace LetMeHatch.Models
{
    public class BreedingDataMale
    {
        public float MaleChance { get; set; }
        public float BoostByMale { get; set; }
        public Dictionary<ItemType, EggIdCandidates> EggsIDs { get; set; }
        private Dictionary<ItemType, string?> FinalEggsIDs { get; set; }
        public bool Enabled { get; set; }
        public bool AdjustableMaleChances { get; set; }
        private int Quantity { get; set; } = 0;

        public BreedingDataMale()
        {
            MaleChance = 0.5f;
            BoostByMale = 0.5f;
            EggsIDs = new Dictionary<ItemType, EggIdCandidates>()
            {
                { ItemType.Normal, EggIdCandidates.Empty },
                { ItemType.Large, EggIdCandidates.Empty },
                { ItemType.FertilizedEgg, EggIdCandidates.Empty }
            };
            FinalEggsIDs = EggsIDs.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Primary,
                EqualityComparer<ItemType>.Default
            );
            Enabled = true;
            AdjustableMaleChances = true;
        }

        public void SetEggID(ItemType type, string? eggID)
        {
            FinalEggsIDs[type] = eggID;
        }

        public string? GetEggID(ItemType type)
        {
            return FinalEggsIDs[type];
        }

        public Dictionary<ItemType, string?> GetEggIDs()
        {
            return FinalEggsIDs;
        }

        public Dictionary<ItemType, string?> GetResolvedEggIDs(Func<IEnumerable<string?>, string?, string?> chooseCandidate)
        {
            if (chooseCandidate is null)
                throw new ArgumentNullException(nameof(chooseCandidate));

            return new Dictionary<ItemType, string?>
            {
                [ItemType.Normal] = chooseCandidate(
                    EggsIDs.TryGetValue(ItemType.Normal, out var normal) ? normal.Values : Array.Empty<string>(),
                    GetEggID(ItemType.Normal)
                ),
                [ItemType.Large] = chooseCandidate(
                    EggsIDs.TryGetValue(ItemType.Large, out var large) ? large.Values : Array.Empty<string>(),
                    GetEggID(ItemType.Large)
                ),
                [ItemType.FertilizedEgg] = GetEggID(ItemType.FertilizedEgg)
            };
        }

        public void SetEggIDs(Dictionary<ItemType, string?> eggIDs)
        {
            FinalEggsIDs = new(eggIDs);
        }

        public Dictionary<ItemType, EggIdCandidates> CloneConfiguredEggs()
        {
            return EggsIDs.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Clone(),
                EqualityComparer<ItemType>.Default
            );
        }

        public void SetQuantity(int quantity)
        {
            Quantity = quantity < 0 ? 0 : quantity;
        }

        public int GetQuantity()
        {
            return Quantity;
        }
    }

    [JsonConverter(typeof(EggIdCandidatesJsonConverter))]
    public sealed class EggIdCandidates
    {
        public static EggIdCandidates Empty { get; } = new();

        public List<string> Values { get; init; } = new();

        public string? Primary
            => Values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?.Trim();

        public bool IsEmptyOrWhitespace()
            => string.IsNullOrWhiteSpace(Primary);

        public EggIdCandidates Clone()
            => new()
            {
                Values = Values
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Select(static value => value.Trim())
                    .ToList()
            };

        public static implicit operator EggIdCandidates(string? value)
            => string.IsNullOrWhiteSpace(value)
                ? Empty
                : new EggIdCandidates { Values = new List<string> { value.Trim() } };

        public static implicit operator EggIdCandidates(string[] values)
            => new()
            {
                Values = values?
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Select(static value => value.Trim())
                    .ToList() ?? new List<string>()
            };
    }

    internal sealed class EggIdCandidatesJsonConverter : JsonConverter<EggIdCandidates>
    {
        public override EggIdCandidates Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return EggIdCandidates.Empty;

            if (reader.TokenType == JsonTokenType.String)
                return reader.GetString();

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var values = new List<string>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    if (reader.TokenType == JsonTokenType.String)
                    {
                        var value = reader.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            values.Add(value.Trim());
                    }
                }

                return new EggIdCandidates { Values = values };
            }

            throw new JsonException($"Unsupported egg id token: {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, EggIdCandidates value, JsonSerializerOptions options)
        {
            var values = value?.Values?
                .Where(static entry => !string.IsNullOrWhiteSpace(entry))
                .Select(static entry => entry.Trim())
                .ToList() ?? new List<string>();

            if (values.Count <= 1)
            {
                writer.WriteStringValue(values.Count == 0 ? string.Empty : values[0]);
                return;
            }

            writer.WriteStartArray();
            foreach (var entry in values)
                writer.WriteStringValue(entry);
            writer.WriteEndArray();
        }
    }
}
