using System.Text.RegularExpressions;
using Microsoft.Xna.Framework.Graphics;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley;
using StardewValley.Extensions;
using WildEggsHatching.Framework;
using WildEggsHatching.Managers;
using WildEggsHatching.Managers.GameAssetManagement;
using WildEggsHatching.Models;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.UI
{
    /// <summary>The mod entry point.</summary>
    ///
    internal class GMCMenu
    {
        private readonly ModData _modSaveData;
        private readonly WildListManager _wildAnimalManager;
        private readonly GameAssetManager _gameAssetManager;
        private IGMCMOptionsHelper ConfigMenu { get; set; }

        internal GMCMenu(
            IGMCMOptionsHelper configMenu,
            ModData modData,
            WildListManager wildAnimalManager,
            GameAssetManager gameAssetManager
        )
        {
            _gameAssetManager = gameAssetManager;
            _modSaveData = modData;
            _wildAnimalManager = wildAnimalManager;
            ConfigMenu = configMenu;
            ConfigMenu.Register(reset: () => ModEntry.Config.Reset(), save: SaveConfig);
        }

        private void SaveConfig()
        {
            if (ModEntry.Config.SetRandom || ModEntry.Config.SetZero)
            {
                SetWeights(ModEntry.Config.SetRandom);
            }
            _gameAssetManager.ConfigEdits();
            RefreshMenu();
        }

        internal void RefreshMenu()
        {
            ModEntry.HelperInstance.WriteConfig(ModEntry.Config);
            ConfigMenu.Unregister(ModEntry.ModManifestInstance);
            ConfigMenu.Register(reset: () => ModEntry.Config.Reset(), save: SaveConfig);
            Setup();
        }

        internal void Setup()
        {
            ConfigMenu.AddImage(
                texture: () =>
                    ModEntry.HelperInstance.ModContent.Load<Texture2D>(
                        AssetHandler.GetAssetPath(
                            Assets.Misc.Interrogation,
                            AssetPathType.ModFolderPath
                        )
                    ),
                scale: 1
            );

            //Sections
            AddGeneralSection();
            AddIncubatorSection();
            AddQuestsSection();
            AddAnimalsSection();

            //Pages
            AddCollectionPage();
            AddExclusionsPage();
            AddAnimalWeightsPages(isMagic: false);
            AddAnimalWeightsPages(isMagic: true);
        }

        private void AddGeneralSection()
        {
            ConfigMenu.AddSectionTitle(sectionName: "General");
            ConfigMenu.AddBoolOption(
                optionName: "extradebuginfo",
                tooltip: true,
                getValue: () => ModEntry.Config.ExtraDebugInfo,
                setValue: value => ModEntry.Config.ExtraDebugInfo = value
            );
            ConfigMenu.AddEnumOption(
                optionName: "UpdateRate",
                getValue: () => ModEntry.Config.UpdateRate,
                setValue: value => ModEntry.Config.UpdateRate = value
            );
            if (ModIdEvaluator.Evaluate("Mikew.lmh"))
            {
                ConfigMenu.AddEnumOption(
                    optionName: "BirthText",
                    tooltip: true,
                    getValue: () => ModEntry.Config.BirthText,
                    setValue: value => ModEntry.Config.BirthText = value
                );
            }
            ConfigMenu.AddBoolOption(
                optionName: "ShowCoopAnimal",
                tooltip: true,
                getValue: () => ModEntry.Config.ShowCoopAnimal,
                setValue: value => ModEntry.Config.ShowCoopAnimal = value
            );

            ConfigMenu.AddBoolOption(
                optionName: "ShowBarnAnimal",
                tooltip: true,
                getValue: () => ModEntry.Config.ShowBarnAnimal,
                setValue: value => ModEntry.Config.ShowBarnAnimal = value
            );
            ConfigMenu.AddBoolOption(
                optionName: "ShowMagicCoopAnimal",
                tooltip: true,
                getValue: () => ModEntry.Config.ShowMagicCoopAnimal,
                setValue: value => ModEntry.Config.ShowMagicCoopAnimal = value
            );

            ConfigMenu.AddBoolOption(
                optionName: "ShowMagicBarnAnimal",
                tooltip: true,
                getValue: () => ModEntry.Config.ShowMagicBarnAnimal,
                setValue: value => ModEntry.Config.ShowMagicBarnAnimal = value
            );
            ConfigMenu.AddBoolOption(
                optionName: "wildeggedit",
                tooltip: true,
                getValue: () => ModEntry.Config.WildEggEdit,
                setValue: value => ModEntry.Config.WildEggEdit = value
            );
            ConfigMenu.AddBoolOption(
                optionName: "alternatepotion",
                tooltip: true,
                getValue: () => ModEntry.Config.AlternatePotionSprite,
                setValue: value => ModEntry.Config.AlternatePotionSprite = value
            );

            ConfigMenu.AddBoolOption(
                optionName: "progressionmode",
                tooltip: true,
                getValue: () => ModEntry.Config.ProgressionMode,
                setValue: value => ModEntry.Config.ProgressionMode = value
            );
            ConfigMenu.AddPageLink(pageName: "Exclusions");

            ConfigMenu.AddPageLink(pageName: "Collection");
        }

        private void AddIncubatorSection()
        {
            ConfigMenu.AddSectionTitle(sectionName: "incubator");
            ConfigMenu.AddBoolOption(
                optionName: "hatchdinoeggs",
                tooltip: true,
                getValue: () => ModEntry.Config.HatchDinoEggs,
                setValue: value => ModEntry.Config.HatchDinoEggs = value
            );

            ConfigMenu.AddBoolOption(
                optionName: "dinoexclusivehatch",
                tooltip: true,
                getValue: () => ModEntry.Config.DinoEggsExclusiveHatch,
                setValue: value => ModEntry.Config.DinoEggsExclusiveHatch = value
            );

            ConfigMenu.AddBoolOption(
                optionName: "needsfern",
                tooltip: true,
                getValue: () => ModEntry.Config.NeedsFern,
                setValue: value => ModEntry.Config.NeedsFern = value
            );
        }

        private void AddQuestsSection()
        {
            ConfigMenu.AddSectionTitle(sectionName: "Quest");
            ConfigMenu.AddBoolOption(
                optionName: "ancientfruitinsteadofseed",
                tooltip: true,
                getValue: () => ModEntry.Config.FruitInsteadOfSeed,
                setValue: value => ModEntry.Config.FruitInsteadOfSeed = value
            );
            ConfigMenu.AddBoolOption(
                optionName: "keepminelevel",
                tooltip: true,
                getValue: () => ModEntry.Config.KeepMineLevel,
                setValue: value => ModEntry.Config.KeepMineLevel = value
            );
            ConfigMenu.AddBoolOption(
                optionName: "enablespiders",
                tooltip: true,
                getValue: () => ModEntry.Config.EnableSpiders,
                setValue: value => ModEntry.Config.EnableSpiders = value
            );
            ConfigMenu.AddEnumOption(
                optionName: "tombchance",
                tooltip: true,
                getValue: () => ModEntry.Config.TombChance,
                setValue: value => ModEntry.Config.TombChance = value
            );
            ConfigMenu.AddEnumOption(
                optionName: "TombDifficulty",
                tooltip: true,
                getValue: () => ModEntry.Config.TombDifficulty,
                setValue: value => ModEntry.Config.TombDifficulty = value
            );
        }

        private void AddAnimalsSection()
        {
            ConfigMenu.AddSectionTitle(sectionName: "Animals");
            ConfigMenu.AddTextOption(
                optionName: "dinoprice",
                tooltip: true,
                getValue: () => ModEntry.Config.DinoPrice,
                setValue: value =>
                    ModEntry.Config.DinoPrice = UpdateVanillaAnimalPrices(
                        VanillaAnimals.Dinosaur,
                        value
                    )
            );
            ConfigMenu.AddTextOption(
                optionName: "ostrichprice",
                tooltip: true,
                getValue: () => ModEntry.Config.OstrichPrice,
                setValue: value =>
                    ModEntry.Config.OstrichPrice = UpdateVanillaAnimalPrices(
                        VanillaAnimals.Ostrich,
                        value
                    )
            );
            ConfigMenu.AddTextOption(
                optionName: "voidchickenprice",
                tooltip: true,
                getValue: () => ModEntry.Config.VoidChickenPrice,
                setValue: value =>
                    ModEntry.Config.VoidChickenPrice = UpdateVanillaAnimalPrices(
                        VanillaAnimals.VoidChicken,
                        value
                    )
            );
            ConfigMenu.AddBoolOption(
                optionName: "setRandom",
                tooltip: true,
                getValue: () => ModEntry.Config.SetRandom,
                setValue: value => ModEntry.Config.SetRandom = value
            );
            ConfigMenu.AddBoolOption(
                optionName: "setZero",
                tooltip: true,
                getValue: () => ModEntry.Config.SetZero,
                setValue: value => ModEntry.Config.SetZero = value && !ModEntry.Config.SetRandom
            );
            ConfigMenu.AddPageLink(pageName: "CoopAnimals");
            ConfigMenu.AddPageLink(pageName: "BarnAnimals");
            ConfigMenu.AddPageLink(pageName: "MagicCoopAnimals");
            ConfigMenu.AddPageLink(pageName: "MagicBarnAnimals");
        }

        private void AddCollectionPage()
        {
            ConfigMenu.AddPage(pageTitle: "Collection");
            var animalSprite_Y = Game1.random.NextBool() ? 0 : 32;
            foreach (
                var animal in _wildAnimalManager.ValidWildAnimals.OrderBy(a => a.Name.ToString())
            )
            {
                ConfigMenu.AddSectionTitle(text: () => animal.Name.ToString());
                //ConfigMenu.AddImage(texture: animal.AnimalIcon!, scale: 1);
                var (TexturePath, Rec) = animal.GetAnimalTexturePathAndRec(animalSprite_Y);
                ConfigMenu.RegisterImage(TexturePath, Rec);
                foreach (var farmAnimal in animal.FarmAnimalKeys.Select(a => a.Id))
                {
                    ConfigMenu.AddBoolOption(
                        name: () => farmAnimal.Replace(animal.ModID, "").Replace("_", ""),
                        getValue: () =>
                            _modSaveData != null
                            && _modSaveData.NewAddedAnimals.Any(a =>
                                a.FarmAnimalName == farmAnimal
                            ),
                        setValue: value => _ = value
                    );
                }
            }
        }

        private void AddExclusionsPage()
        {
            ConfigMenu.AddPage(pageTitle: "Exclusions");

            foreach (
                var animalKey in ModEntry.Config.AnimalProgressionMode.Keys.Where(x =>
                    ModEntry.Config.AnimalProgressionMode.Count > 0
                )
            )
            {
                ConfigMenu.AddBoolOption(
                    optionName: animalKey,
                    getValue: () => ModEntry.Config.AnimalProgressionMode[animalKey],
                    setValue: value => ModEntry.Config.AnimalProgressionMode[animalKey] = value
                );
            }
        }

        private void AddAnimalWeightsPages(bool isMagic = false)
        {
            var buildings = new string[] { VanillaBuildings.Coop, VanillaBuildings.Barn };
            foreach (var building in buildings)
            {
                var buildingCheck = building == "Barn";
                if (
                    _wildAnimalManager.ValidWildAnimals.Any(a =>
                        a.LargeEgg == buildingCheck && a.IsMagic == isMagic
                    )
                )
                {
                    CreatePage(building, isMagic);
                }
            }
        }

        private void CreatePage(string building, bool isMagic)
        {
            var pageId = isMagic ? $"Magic{building}Animals" : $"{building}Animals";
            var i18nKeyPart = isMagic ? $"Magic{building}" : $"{building}";

            ConfigMenu.AddPage(pageTitle: $"{i18nKeyPart}Animals");

            ConfigMenu.AddParagraph(paragraphName: "Animals");

            AddAnimalsBuildingSection(building, isMagic, isModded: false);

            AddAnimalsBuildingSection(building, isMagic, isModded: true);
        }

        private void AddAnimalsBuildingSection(string building, bool isMagic, bool isModded)
        {
            ConfigMenu.AddSectionTitle(
                sectionName: $"{building}{(isMagic ? "Magic" : "")}{(isModded ? "Modded" : "Vanilla")}Animals"
            );
            var animalSprite_Y = Game1.random.NextBool() ? 0 : 32;

            foreach (
                var animal in _wildAnimalManager.GetValidAnimals(
                    building,
                    isMagic,
                    moddedAnimals: isModded
                )
            )
            {
                AddAnimalOptions(animal, animalSprite_Y);
            }
        }

        void AddAnimalOptions(WildAnimalData animal, int animalSprite_Y)
        {
            var (TexturePath, Rec) = animal.GetAnimalTexturePathAndRec(animalSprite_Y);
            ConfigMenu.RegisterImage(TexturePath, Rec);
            //ConfigMenu.AddImage(texture: animal.AnimalIcon!, scale: 1);
            var modName = GetModName(animal.ModID);
            var modNameAndAnimalKeys =
                $"Farm Animal Keys:\n{string.Join(",\n", animal.FarmAnimalKeys.Select(ak => ak.Id))}";
            var tooltip = $"ModID: {animal.ModID}\n{modNameAndAnimalKeys}";
            ConfigMenu.AddFloatNumberOption(
                name: CreateWeightDisplayString(animal.Name),
                tooltip: () => tooltip,
                getValue: () => Config.ChosenWeights[animal.Name],
                setValue: value => ModEntry.Config.ChosenWeights[animal.Name] = value,
                min: 0.00f,
                max: 1f,
                interval: 0.15f
            );
        }

        internal string GetModName(string modID)
        {
            var modsRegistry = HelperInstance.ModRegistry.GetAll();
            var foundMod = modsRegistry.FirstOrDefault(m =>
                string.Equals(m.Manifest.UniqueID, modID, StringComparison.OrdinalIgnoreCase)
            );

            return foundMod?.Manifest.Name ?? string.Empty; // Returns empty string if foundMod is null
        }

        internal Func<string> CreateWeightDisplayString(string animalTypeName)
        {
            string formattedAnimalTypeName = Regex.Replace(animalTypeName, "([A-Z])", " $1").Trim();
            return () =>
                $"{TranslationHelper.Get("ModEntry.Config.weight")}: {formattedAnimalTypeName}";
        }

        private void SetWeights(bool setRandom)
        {
            foreach (var animal in _wildAnimalManager.WildAnimals)
            {
                float weight = setRandom && animal.Price != -1 ? CalculateWeight(animal.Price) : 0;
                ModEntry.Config.ChosenWeights[animal.Name] = weight;
            }
        }

        private float CalculateWeight(float price)
        {
            // Calculate the weight as inversely proportional to the price
            var weight = 1f / (price + 1f); // Add 1 to avoid division by zero

            // Scale the weight to fit within the range [0, 1] with intervals of 0.1
            weight = (float)Math.Round(weight * 10f) / 10f;

            // Add a large random offset to introduce bigger variation
            var rand = new Random();
            var offset = (float)rand.NextDouble() * 0.5f; // 50% variability
            weight += offset;

            // Introduce a minimum weight threshold
            var minWeight = 0.1f; // Minimum weight threshold
            weight = Math.Max(weight, minWeight);

            // Clamp the weight to the range [0, 1]
            weight = Math.Min(weight, 1f);

            return weight;
        }

        private string UpdateVanillaAnimalPrices(string animalName, string priceString)
        {
            var animalKey =
                animalName == VanillaAnimals.VoidChicken
                    ? VanillaAnimals.VoidChicken
                    : animalName.ToString();
            var animal = _wildAnimalManager.ValidWildAnimals.First(a => a.Name == animalName);
            if (animal == null || priceString == "default")
            {
                return priceString;
            }
            if (Game1.farmAnimalData[animalKey].PurchasePrice != -1)
            {
                return (Game1.farmAnimalData[animalKey].PurchasePrice * 2).ToString();
            }
            if (int.TryParse(priceString, out int price) && price >= -1)
            {
                animal.Price = price;
                return priceString;
            }
            else
            {
                animal.Price = -1;
                return "-1";
            }
        }
    }
}
