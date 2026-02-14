using StardewValley;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley.GameData.Objects;
using WildEggsHatching.Framework;
using WildEggsHatching.Models;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Strings;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Managers.GameAssetManagement
{
    /// <summary>The mod entry point.</summary>
    internal class ObjectEditor
    {
        private readonly ModData _modSaveData;
        internal readonly HashSet<WildEgg> _wildEggs;
        internal readonly WildListManager _wildListManager;
        internal readonly Dictionary<string, ObjectData> ItemsToAdd;

        // Constants for asset keys
        private const string PrismaticValleyColorKey = "JollyLlama.PrismaticValleyFramework/Color";
        private const string PrismaticValleyPaletteKey =
            "JollyLlama.PrismaticValleyFramework/Palette";
        private const string CustomPalette = "Custom Palette";

        // Constants for context tags
        private const string ForageContextTag = "forage_item";
        private const string EggCategoryContextTag = "category_egg";
        private const string WildEggContextTag = "wildegg_item";

        internal ObjectEditor(
            HashSet<WildEgg> wildEggs,
            WildListManager wildListManager,
            ModData modData
        )
        {
            ItemsToAdd = new();
            _wildEggs = wildEggs;
            _wildListManager = wildListManager;
            _modSaveData = modData;
        }

        private void CreateModObjects()
        {
            // Create and add various mod objects
            CreateWildEggsForAllLocations();
            CreateModObject(CreateOsirisScroll());
            CreateModObject(CreateElixir());
        }

        public void CreateWildEggsForAllLocations()
        {
            foreach (LocationType location in Enum.GetValues(typeof(LocationType)))
            {
                CreateObjectsForLocation(location);
            }
        }

        private void CreateObjectsForLocation(LocationType location)
        {
            // Define locations that do not support certain egg types
            var locationsWithoutNormalEgg = new List<LocationType> { LocationType.Magical };
            var locationsWithoutLargeEgg = new List<LocationType> { LocationType.Magical };
            var locationsWithoutMagicNormalEgg = new List<LocationType>();
            var locationsWithoutMagicLargeEgg = new List<LocationType>();

            string prefix = $"{ModManifestInstance.UniqueID}_{location}";
            LoggerHelper.Log($"Creating objects for location: {location}");

            // Define egg IDs based on location
            string normalEggID =
                location != LocationType.Farm ? $"{prefix}WEgg" : IDs.Objects.FarmWEgg;
            string largeEggID = $"{prefix}LWEgg";
            string magicNormalEggID = $"{prefix}MWEgg";
            string magicLargeEggID = $"{prefix}LMWEgg";

            LoggerHelper.Log(
                $"Normal Egg ID: {normalEggID}, Large Egg ID: {largeEggID}, "
                    + $"Magic Normal Egg ID: {magicNormalEggID}, Magic Large Egg ID: {magicLargeEggID}"
            );

            // Create WildEgg instances
            var eggs = new List<WildEgg>();

            // Helper method to add eggs based on conditions
            void AddEggIfValid(bool condition, string id, bool isMagic, bool isLarge)
            {
                if (condition)
                {
                    var egg = new WildEgg(location, id, isMagic, isLarge, _wildListManager);
                    eggs.Add(egg);
                    LoggerHelper.Log(
                        $"Created egg: {egg.ID}, IsMagic: {egg.IsMagic}, IsLarge: {egg.IsLarge}"
                    );
                }
                else
                {
                    LoggerHelper.Log(
                        $"Condition not met for egg ID: {id}, IsMagic: {isMagic}, IsLarge: {isLarge}"
                    );
                }
            }

            // Add eggs based on location conditions
            AddEggIfValid(
                !locationsWithoutNormalEgg.Contains(location) && location != LocationType.Farm,
                normalEggID,
                false,
                false
            );
            AddEggIfValid(!locationsWithoutLargeEgg.Contains(location), largeEggID, false, true);
            AddEggIfValid(
                !locationsWithoutMagicNormalEgg.Contains(location),
                magicNormalEggID,
                true,
                false
            );
            AddEggIfValid(
                !locationsWithoutMagicLargeEgg.Contains(location),
                magicLargeEggID,
                true,
                true
            );

            // Add eggs to the HashSet and create mod objects based on their properties
            foreach (var egg in eggs)
            {
                if (!egg.IsValid)
                {
                    LoggerHelper.Log($"Egg {egg.ID} is not valid.");
                    continue;
                }
                _wildEggs.Add(egg);
                LoggerHelper.Log($"Adding valid egg: {egg.ID} to the collection.");

                CreateModObjectBasedOnEgg(egg, location);
            }

            LoggerHelper.Log($"Finished creating objects for location: {location}");
        }

        // Helper method to create mod objects based on egg properties
        private void CreateModObjectBasedOnEgg(WildEgg egg, LocationType location)
        {
            if (!egg.IsMagic && !egg.IsLarge && location != LocationType.Farm)
            {
                CreateModObject(CreateWildEgg(egg.ID, location));
            }
            else if (!egg.IsMagic && egg.IsLarge)
            {
                CreateModObject(CreateLargeWildEgg(egg.ID, location));
            }
            else if (egg.IsMagic && !egg.IsLarge)
            {
                CreateModObject(CreateMagicWildEgg(egg.ID, location));
            }
            else if (egg.IsMagic && egg.IsLarge)
            {
                CreateModObject(CreateLargeMagicWildEgg(egg.ID, location));
            }
        }

        private void CreateJunimoNotes()
        {
            if (_modSaveData.FarmAnimalNames.Any())
            {
                foreach (var animal in _modSaveData.FarmAnimalNames)
                {
                    string animalKeyNoSpace = animal.Replace(" ", "");
                    string noteID = $"Mikew.wildeggshatching_junimoNote_{animalKeyNoSpace}";
                    if (ItemsToAdd.ContainsKey(noteID))
                    {
                        continue;
                    }
                    var junimoNote = CreateJunimoNote(animal);
                    ItemsToAdd.Add(noteID, junimoNote);
                }
            }
        }

        private void CreateModObject(ObjectData objectData)
        {
            string id = objectData.Name;
            ItemsToAdd.TryAdd(id, objectData);
        }

        internal ObjectData CreateJunimoNote(string animalKey)
        {
            var animalDisplayName = GameAssetManager.GetDisplayName(animalKey);
            var junimoNote = CreateObjectData(
                TranslationHelper.Get("object.JunimoNote.description"),
                $"{TranslationHelper.Get("object.JunimoNote.name")}: {animalDisplayName}",
                animalKey,
                Assets.Object.Note,
                new Dictionary<string, string>
                {
                    { PrismaticValleyColorKey, CustomPalette },
                    { PrismaticValleyPaletteKey, " #34C759, #8BC34A, #2E865F" },
                    { "Animal", animalKey },
                }
            );
            LoggerHelper.Log($"Created Junimo Note: {animalKey} - {animalDisplayName}");
            return junimoNote;
        }

        internal ObjectData CreateOsirisScroll()
        {
            var osirisScroll = CreateObjectData(
                TranslationHelper.Get("object.OsirisScroll.description"),
                TranslationHelper.Get("object.OsirisScroll.name"),
                IDs.Objects.OsirisScroll,
                Assets.Object.Scroll,
                new Dictionary<string, string>
                {
                    { PrismaticValleyColorKey, CustomPalette },
                    { PrismaticValleyPaletteKey, " #FFC080, #E67E73, #342B1F" },
                }
            );
            LoggerHelper.Log($"Created Osiris Scroll: {osirisScroll.Name}");
            return osirisScroll;
        }

        internal ObjectData CreateElixir()
        {
            var elixir = CreateObjectData(
                TranslationHelper.Get("object.ElixirOfRevitalizedEssence.Description"),
                TranslationHelper.Get("object.ElixirOfRevitalizedEssence.name"),
                IDs.Objects.Elixir,
                Assets.Object.Elixir,
                new Dictionary<string, string>
                {
                    { PrismaticValleyColorKey, CustomPalette },
                    { PrismaticValleyPaletteKey, " #FFC080, #E67E73, #342B1F" },
                },
                ModEntry.Config.AlternatePotionSprite ? 1 : 0
            );
            LoggerHelper.Log($"Created Elixir: {elixir.Name}");
            return elixir;
        }

        internal ObjectData CreateWildEgg(
            string eggID,
            LocationType locationType,
            EggType eggType,
            int spriteIndex,
            int price,
            string colorTag
        )
        {
            var location = locationType.ToString();
            var wildEgg = CreateObjectData(
                TranslationHelper.Get($"object.{eggType}.description"),
                TranslationHelper.Get($"object.{eggType}.name"),
                eggID,
                Assets.Object.Eggs,
                contextTags: new List<string>
                {
                    colorTag,
                    ForageContextTag,
                    EggCategoryContextTag,
                    WildEggContextTag,
                    "wildegg_" + eggType.ToString(),
                },
                spriteIndex: spriteIndex,
                price: price
            );
            wildEgg.CustomFields ??= new();
            wildEgg.CustomFields["wildEggLocation"] = location;
            // LoggerHelper.Log($"Created {eggType}: {wildEgg.Name}");
            return wildEgg;
        }

        internal ObjectData CreateWildEgg(string eggID, LocationType locationType)
        {
            return CreateWildEgg(eggID, locationType, EggType.wildEgg, 0, 100, "color_white");
        }

        internal ObjectData CreateLargeWildEgg(string eggID, LocationType locationType)
        {
            return CreateWildEgg(eggID, locationType, EggType.largeWildEgg, 1, 150, "color_white");
        }

        internal ObjectData CreateMagicWildEgg(string eggID, LocationType locationType)
        {
            return CreateWildEgg(eggID, locationType, EggType.magicWildEgg, 2, 100, "color_purple");
        }

        internal ObjectData CreateLargeMagicWildEgg(string eggID, LocationType locationType)
        {
            return CreateWildEgg(
                eggID,
                locationType,
                EggType.largeMagicWildEgg,
                3,
                200,
                "color_purple"
            );
        }

        private ObjectData CreateObjectData(
            string description,
            string displayName,
            string name,
            Assets.Object asset,
            Dictionary<string, string>? customFields = null,
            int spriteIndex = 0,
            int price = 0,
            List<string>? contextTags = null
        )
        {
            var objectData = new ObjectData
            {
                Description = description,
                DisplayName = displayName,
                Name = name,
                Type = "AnimalProduct",
                Category = -5,
                Price = price,
                Texture = AssetHandler.GetAssetPath(asset, AssetPathType.GamePath),
                SpriteIndex = spriteIndex,
                Edibility = 15,
                IsDrink = false,
                ExcludeFromRandomSale = true,
                ContextTags = contextTags ?? new List<string>(),
                CustomFields = customFields,
            };
            LoggerHelper.Log($"Created Object Data: {objectData.Name}");
            return objectData;
        }

        internal static bool HasItem(string itemId)
        {
            bool hasItem = Utility.doesItemExistAnywhere(itemId);
            //LoggerHelper.Log($"Has item: {itemId} - {hasItem}",);
            return hasItem;
        }

        internal static bool HasItemOnThisLocation(GameLocation location, string itemId)
        {
            bool hasItem = Utility.ForEachItemIn(
                location,
                item => item.ItemId.EqualsIgnoreCase(itemId)
            );
            //LoggerHelper.Log($"Has item on this location: {itemId} - {hasItem}");
            return hasItem;
        }

        internal void ChangeElixirSprite()
        {
            if (ItemsToAdd.TryGetValue(IDs.Objects.Elixir, out var objectData))
            {
                objectData.SpriteIndex = ModEntry.Config.AlternatePotionSprite ? 0 : 1;
                LoggerHelper.Log($"Changed Elixir sprite: {objectData.Name}");
            }
        }

        internal void AddItems(IDictionary<string, ObjectData> data)
        {
            GameAssetManager.PopulateIfEmpty(ItemsToAdd, () => CreateModObjects());
            CreateJunimoNotes();
            data.TryAddMany(ItemsToAdd);
            LoggerHelper.Log("Items added successfully.");
        }

        internal void EditWildEgg(IDictionary<string, ObjectData> data)
        {
            data[IDs.Objects.FarmWEgg].ContextTags.Add(WildEggContextTag);
            data[IDs.Objects.FarmWEgg].Texture = AssetHandler.GetAssetPath(
                Assets.Object.Eggs,
                AssetPathType.GamePath
            );
            data[IDs.Objects.FarmWEgg].SpriteIndex = 0;
            LoggerHelper.Log("Wild Egg edited successfully.");
        }
    }
}
