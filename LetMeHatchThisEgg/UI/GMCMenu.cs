using Mikew.BirdsOfAFeather.Helpers;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Framework;
using LetMeHatchThisEgg.GameAssetManagement;
using LetMeHatchThisEgg.Helpers;
using LetMeHatchThisEgg.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TokenizableStrings;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;
using static LetMeHatchThisEgg.ModEntry;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;

namespace LetMeHatchThisEgg.UI
{
    public class GMCMenu
    {
        private readonly SavedColors SavedColors;
        private readonly SavedItemsData SavedItemsData;
        private readonly CloningHelper CloningHelper;
        private readonly Dictionary<
            string,
            Dictionary<CloneItemType, Dictionary<ItemType, string>>
        > DefaultItemRelations;

        private readonly Dictionary<
            string,
            Dictionary<string, BreedingDataFemale>
        > LoadedFemaleBreedingData;
        private readonly Dictionary<CloneItemType, List<string>> AnimalVanillaItemRelationOrder;
        private readonly List<IContentPack> LoadedContentPacks;
        private readonly List<ClonedAnimalInfo> ClonedAnimalsInfoList;
        private readonly IGMCMOptionsHelper ConfigMenu = null!;
        private readonly Dictionary<
            string,
            Dictionary<string, BreedingConfig>
        > BreedingConfigsByPack;
        private const float MAX_CHANCE = 1f;
        private const float MIN_CHANCE = 0f;
        private const float CHANCE_INTERVAL = 0.1f;

        public GMCMenu(
            IGMCMOptionsAPI? configExt,
            IGenericModConfigMenuAPI? configMenuAPI,
            GameAssetManager assetManager
        )
        {
            ConfigMenu = new(
                configExt,
                configMenuAPI,
                ModEntry.TranslationHelper,
                ModEntry.ModManifestInstance
            );
            SavedColors = assetManager.SavedColors;
            SavedItemsData = assetManager.SavedItemsData;
            CloningHelper = assetManager.CloningHelper;
            DefaultItemRelations = assetManager.AnimalItemRelationsManager.DefaultValues;
            LoadedContentPacks = assetManager.LoadedContentPacks;
            LoadedFemaleBreedingData = assetManager.LoadedFemaleBreedingData;
            ClonedAnimalsInfoList = assetManager.ClonedAnimalsInfoList;
            BreedingConfigsByPack = assetManager.BreedingConfigsByPack;
            AnimalVanillaItemRelationOrder = assetManager
                .AnimalItemRelationsManager
                .AnimalVanillaItemRelationOrder;
            ConfigMenu.Register(reset: () => GMCM_reset(), save: () => GMCM_save());
        }

        public void GMCMSetup()
        {
            MenuUpdate();
            AddGeneralSection();
            AddHorizontalSeparator();
            AddBreedingSystemSection();
            AddHorizontalSeparator();
            AddEggListEditsSection();
            AddHorizontalSeparator();
            AddBirthTextEditsSection();
            AddHorizontalSeparator();
            AddClonedEggsEditsSection();
            AddHorizontalSeparator();
            // AddBreedingSystemSection();
            AddEggsSection();
            AddFemalePages();
            AddEggRelationsPage();
            AddEditedListsPage(EditType.EggList);
            AddEditedListsPage(EditType.BirthText);
            //AddEditedListsPage(EditType.BirthText, true);
            AddEggColorsPage();
            AddEggTypePages();
        }

        private void AddEggsSection()
        {
            ConfigMenu.AddPageLink(pageName: "eggsenabled");
            ConfigMenu.AddPageLink(pageName: "eggcolors");
            /* if (Game1.hasStartedDay)
             {*/
            ConfigMenu.AddPageLink(pageName: "eggRelations");
            /*}
            else
            {
                ConfigMenu.AddParagraph(
                    text: () =>
                        $"{TranslationHelper.Get("config.pagelink.cloneEggs.name")} {TranslationHelper.Get("config.notavaliable")}"
                );
            }*/
        }

        private void AddEggTypePages()
        {
            foreach (var egg in ModEntry.Config.EggColors.Keys)
            {
                ConfigMenu.AddPage(
                    pageId: $"egg.{egg}",
                    pageTitle: () => GetObjectDisplayName(egg)
                );
                ConfigMenu.AddSectionTitle(sectionName: "colors");
                foreach (
                    var colorName in SavedColors._colorDictionary.Keys.Where(c =>
                        c != ColorNames.Gold
                    )
                )
                {
                    ConfigMenu.AddBoolOption(
                        optionName: $"color.{colorName}",
                        getValue: () => ModEntry.Config.EggColors[egg][colorName],
                        setValue: value => ModEntry.Config.EggColors[egg][colorName] = value
                    );
                }
            }
        }

        private void AddEggColorsPage()
        {
            ConfigMenu.AddPage(pageTitle: "eggcolors");
            ConfigMenu.AddPageLink(pageName: "managechosencolors");
            ConfigMenu.AddSimpleHorizontalSeparator();
            ConfigMenu.AddSectionTitle(sectionName: "randomization");
            foreach (var egg in ModEntry.Config.EggColors.Keys)
            {
                ConfigMenu.AddPageLink(pageId: $"egg.{egg}", text: () => GetObjectDisplayName(egg));
            }
            ConfigMenu.AddPage(pageTitle: "managechosencolors");
            foreach (var colorKey in SavedColors.ItemColors.Keys.OrderBy(e => e.Split(' ').Last()))
            {
                ConfigMenu.AddColorOption(
                    getValue: () => Colors.HexToColor(SavedColors.ItemColors[colorKey]),
                    setValue: value => SavedColors.ItemColors[colorKey] = Colors.ColorToHex(value),
                    name: () => colorKey,
                    showAlpha: false,
                    colorPickerStyle: 0b00000100
                );
            }
        }

        private void AddBreedingSystemSection()
        {
            ConfigMenu.AddSectionTitle("LoadedBreedingSystems");
            AddFemalePageLinks();
        }

        private void AddFemalePageLinks()
        {
            foreach (var contentPackID in LoadedFemaleBreedingData.Keys)
            {
                ConfigMenu.AddPageLink(
                    pageId: contentPackID,
                    text: () => GetContentPackName(contentPackID)
                );
            }
        }

        private void AddFemalePages()
        {
            foreach (var (contentPackID, femaleBreedingData) in LoadedFemaleBreedingData)
            {
                ConfigMenu.AddPage(
                    pageId: contentPackID,
                    pageTitle: () => GetContentPackName(contentPackID),
                    true
                );
                ConfigMenu.AddSectionTitle("Females");
                foreach (var (femaleKey, breedingDataGroupList) in femaleBreedingData)
                {
                    ConfigMenu.AddPageLink(
                        pageId: $"{contentPackID}_{femaleKey}",
                        text: () => femaleKey
                    );
                }
                foreach (var (femaleKey, breedingData) in femaleBreedingData)
                {
                    var contentPackConfig = BreedingConfigsByPack[contentPackID][femaleKey];
                    ConfigMenu.AddPage(
                        pageId: $"{contentPackID}_{femaleKey}",
                        pageTitle: () => femaleKey,
                        true
                    );
                    ConfigMenu.AddBoolOption(
                        getValue: () => breedingData.ApplyDailyLuckBoost,
                        setValue: value => breedingData.ApplyDailyLuckBoost = value,
                        optionName: "ApplyDailyBoost",
                        tooltip: true
                    );
                    if (breedingData.AdjustableEggChances)
                    {
                        ConfigMenu.AddSectionTitle("NormalHatching");
                        foreach (
                            var eggType in contentPackConfig.EggChances.Keys.Where(e =>
                                e != ItemType.FertilizedEgg
                            )
                        )
                        {
                            ConfigMenu.AddFloatNumberOption(
                                getValue: () => contentPackConfig.EggChances[eggType],
                                setValue: value => contentPackConfig.EggChances[eggType] = value,
                                optionName: $"{eggType}eggChance",
                                tooltip: true,
                                min: MIN_CHANCE,
                                max: MAX_CHANCE,
                                interval: CHANCE_INTERVAL
                            );
                        }
                        ConfigMenu.AddSectionTitle("FertilizedHatching");
                        ConfigMenu.AddFloatNumberOption(
                            getValue: () => contentPackConfig.EggChances[ItemType.FertilizedEgg],
                            setValue: value =>
                                contentPackConfig.EggChances[ItemType.FertilizedEgg] = value,
                            optionName: $"{ItemType.FertilizedEgg}eggChance",
                            tooltip: true,
                            min: MIN_CHANCE,
                            max: MAX_CHANCE,
                            interval: CHANCE_INTERVAL
                        );
                    }
                    ConfigMenu.AddSectionTitle("Males");
                    foreach (var (maleKey, maleData) in breedingData.Males)
                    {
                        string maleChosenKey = maleKey.EndsWith("_Male") ? $"Cloned Male" : maleKey;
                        ConfigMenu.AddSectionTitle(() => $"{maleChosenKey}");

                        if (Game1.farmAnimalData.ContainsKey(maleKey))
                        {
                            var rectangle = Game1
                                .farmAnimalData[maleKey]
                                .House.Contains("coop", StringComparison.OrdinalIgnoreCase)
                                ? new Rectangle(0, 0, 16, 16)
                                : new Rectangle(0, 0, 32, 32);
                            ConfigMenu.RegisterImage(
                                texPath: Game1.farmAnimalData[maleKey].Texture,
                                texRect: rectangle,
                                scale: 4
                            );
                        }
                        else if (maleKey.EndsWith("_Male"))
                        {
                            var rectangle = Game1
                                .farmAnimalData[femaleKey]
                                .House.Contains("coop", StringComparison.OrdinalIgnoreCase)
                                ? new Rectangle(0, 0, 16, 16)
                                : new Rectangle(0, 0, 32, 32);
                            ConfigMenu.RegisterImage(
                                texPath: Game1.farmAnimalData[femaleKey].Texture,
                                texRect: rectangle,
                                scale: 4
                            );
                        }
                        ConfigMenu.AddBoolOption(
                            getValue: () => contentPackConfig.Enabled[maleKey],
                            setValue: value => contentPackConfig.Enabled[maleKey] = value,
                            optionName: "EnableMale",
                            tooltip: true
                        );
                        if (CloningHelper.IsAClonedAnimal(maleKey))
                        {
                            ConfigMenu.AddBoolOption(
                                getValue: () => contentPackConfig.MaleConfig.AddAsAlternatePurchase,
                                setValue: value =>
                                    contentPackConfig.MaleConfig.AddAsAlternatePurchase = value,
                                optionName: "AddAsAlternatePurchase",
                                tooltip: true
                            );
                        }
                        string output = string.Join(
                            Environment.NewLine,
                            maleData.GetEggIDs().Select(egg => $"Type: {egg.Key}, ID: {egg.Value}")
                        );
                        ConfigMenu.AddParagraph(() => $"Eggs:\n{output}");

                        if (maleData.AdjustableMaleChances)
                        {
                            ConfigMenu.AddFloatNumberOption(
                                getValue: () => contentPackConfig.BoostByMale[maleKey],
                                setValue: value => contentPackConfig.BoostByMale[maleKey] = value,
                                optionName: "BoostByMale",
                                tooltip: true,
                                min: MIN_CHANCE,
                                max: MAX_CHANCE,
                                interval: CHANCE_INTERVAL
                            );

                            ConfigMenu.AddFloatNumberOption(
                                getValue: () => contentPackConfig.MaleChance[maleKey],
                                setValue: value => contentPackConfig.MaleChance[maleKey] = value,
                                optionName: "MaleChance",
                                tooltip: true,
                                min: MIN_CHANCE,
                                max: MAX_CHANCE,
                                interval: CHANCE_INTERVAL
                            );
                        }
                        ConfigMenu.AddHorizontalSeparator();
                    }
                }
            }
        }

        private void AddClonedEggsEditsSection()
        {
            ConfigMenu.AddSectionTitle(sectionName: "clonedeggsedits");
            ConfigMenu.AddBoolOption(
                optionName: "clonevanillaeggs",
                tooltip: true,
                getValue: () => ModEntry.Config.CloneVanillaEggs,
                setValue: value => ModEntry.Config.CloneVanillaEggs = value
            );
            ConfigMenu.AddBoolOption(
                optionName: "dinoeggsasartifact",
                tooltip: true,
                getValue: () => ModEntry.Config.DinoEggAsArtifact,
                setValue: value => ModEntry.Config.DinoEggAsArtifact = value
            );
            ConfigMenu.AddBoolOption(
                optionName: "clonemachinerules",
                tooltip: true,
                getValue: () => ModEntry.Config.CloneMachineRules,
                setValue: value => ModEntry.Config.CloneMachineRules = value
            );
            ConfigMenu.AddBoolOption(
                optionName: "changeeggcolors",
                tooltip: true,
                getValue: () => ModEntry.Config.ChangeEggColors,
                setValue: value => ModEntry.Config.ChangeEggColors = value
            );
            ConfigMenu.AddBoolOption(
                optionName: "ColorMilk",
                tooltip: true,
                getValue: () => ModEntry.Config.ColorClonedMilk,
                setValue: value => ModEntry.Config.ColorClonedMilk = value
            );
            ConfigMenu.AddBoolOption(
                optionName: "RabbitWool",
                tooltip: true,
                getValue: () => ModEntry.Config.ExclusiveRabbitWool,
                setValue: value => ModEntry.Config.ExclusiveRabbitWool = value
            );

            ConfigMenu.AddBoolOption(
                optionName: "linkanimalcolortoeggs",
                tooltip: true,
                getValue: () => ModEntry.Config.LinkAnimalColorToEgg,
                setValue: value => ModEntry.Config.LinkAnimalColorToEgg = value
            );
        }

        private void AddHorizontalSeparator()
        {
            ConfigMenu.AddSimpleHorizontalSeparator();
        }

        private void AddBirthTextEditsSection()
        {
            ConfigMenu.AddSectionTitle(sectionName: "birthtextedits");
            ConfigMenu.AddBoolOption(
                optionName: "editbirthtext",
                tooltip: true,
                getValue: () => ModEntry.Config.EditBirthText,
                setValue: value => ModEntry.Config.EditBirthText = value
            );
            ConfigMenu.AddBoolOption(
                optionName: "AlternateBirthText",
                tooltip: true, //"Show all edits made to animal names and birth texts",
                getValue: () => ModEntry.Config.AlternateBirthText,
                setValue: value => ModEntry.Config.AlternateBirthText = value
            );
            if (Game1.hasStartedDay)
            {
                ConfigMenu.AddPageLink(pageName: $"{EditType.BirthText}");
            }
            else
            {
                ConfigMenu.AddParagraph(
                    text: () =>
                        $"{TranslationHelper.Get($"config.pagelink.{EditType.BirthText}.name")} {TranslationHelper.Get("config.notavaliable")}"
                );
            }
        }

        private void AddEggListEditsSection()
        {
            ConfigMenu.AddSectionTitle(sectionName: "egglistedits");

            ConfigMenu.AddBoolOption(
                optionName: "editegglistbyname",
                tooltip: true,
                getValue: () => ModEntry.Config.EditEggListByName,
                setValue: value => ModEntry.Config.EditEggListByName = value
            );
            if (Game1.hasStartedDay)
            {
                ConfigMenu.AddPageLink(pageName: $"{EditType.EggList}");
            }
            else
            {
                ConfigMenu.AddParagraph(
                    text: () =>
                        $"{TranslationHelper.Get($"config.pagelink.{EditType.EggList}.name")} {TranslationHelper.Get("config.notavaliable")}"
                );
            }
        }

        private void AddGeneralSection()
        {
            ConfigMenu.AddSectionTitle(sectionName: "general");
            ConfigMenu.AddBoolOption(
                optionName: "showedits",
                tooltip: true,
                getValue: () => ModEntry.Config.ShowEdits,
                setValue: value => ModEntry.Config.ShowEdits = value
            );
            ConfigMenu.AddBoolOption(
                optionName: "ShowAnimalKeys",
                tooltip: true,
                getValue: () => ModEntry.Config.ShowAnimalKeys,
                setValue: value => ModEntry.Config.ShowAnimalKeys = value
            );
            ConfigMenu.AddBoolOption(
                getValue: () => ModEntry.Config.OnlyFertilizedEggs,
                setValue: value => ModEntry.Config.OnlyFertilizedEggs = value,
                optionName: "OnlyFertilizedEggs",
                tooltip: true
            );
        }

        private void AddEditedListsPage(EditType editType)
        {
            var EditedList = ModEntry.Config.EditedAnimals[editType];

            ConfigMenu.AddPage(pageTitle: editType.ToString());

            var enumerable = EditedList
                .Where(e => !VanillaAnimals.AnimalKeys.Contains(e.Key))
                .OrderBy(e => e.Key.Split(' ').Last());
            foreach (var entry in enumerable)
            {
                ConfigMenu.AddBoolOption(
                    name: () => entry.Key,
                    getValue: () => EditedList[entry.Key], // Use the original dictionary
                    setValue: value => EditedList[entry.Key] = value // Use the original dictionary
                );
            }
        }

        private void AddEggRelationsPage()
        {
            var cloneItemTypes = new CloneItemType[]
            {
                CloneItemType.Egg,
                CloneItemType.Milk,
                CloneItemType.Misc
            };
            ConfigMenu.AddPage(pageTitle: "eggRelations");
            foreach (var itemType in cloneItemTypes)
            {
                ConfigMenu.AddPageLink(itemType.ToString());
            }
            foreach (var itemType in cloneItemTypes)
            {
                AddEggRelationsByTypePage(itemType);
            }
        }

        private void AddEggRelationsByTypePage(CloneItemType itemType)
        {
            ConfigMenu.AddPage(itemType.ToString(), true);
            foreach (
                var (animal, items) in SavedItemsData
                    .AnimalItemRelation.Where(k =>
                        Game1.farmAnimalData.ContainsKey(k.Key) && k.Value[itemType] != null
                    )
                    .OrderBy(x => AnimalVanillaItemRelationOrder[itemType].IndexOf(x.Key))
                    .Select(x => (x.Key, x.Value[itemType]!))
            )
            {
                if (!ModEntry.Config.ItemToClone.ContainsKey(animal))
                {
                    ModEntry.Config.ItemToClone[animal] = new()
                    {
                        [itemType] = new()
                        {
                            [ItemType.Normal] = items[ItemType.Normal],
                            [ItemType.Large] = items[ItemType.Large],
                        }
                    };
                }
                else if (!ModEntry.Config.ItemToClone[animal].ContainsKey(itemType))
                {
                    ModEntry.Config.ItemToClone[animal][itemType] = new()
                    {
                        [ItemType.Normal] = items[ItemType.Normal],
                        [ItemType.Large] = items[ItemType.Large],
                    };
                }
                ConfigMenu.AddSectionTitle(() => animal);

                List<(Func<string?> label, Texture2D texture, Rectangle? rect)> testImageData =
                    new();
                foreach (var item in SavedItemsData.FoundItems[itemType])
                {
                    string label;
                    string texturePath;
                    Texture2D image;
                    Rectangle rec;
                    if (string.IsNullOrEmpty(item))
                    {
                        label = TranslationHelper.Get("Config.Off");
                        texturePath = "loosesprites/cursors";
                        image = ModEntry.HelperInstance.GameContent.Load<Texture2D>(texturePath);
                        rec = new Rectangle(320, 496, 16, 16);
                        testImageData.Insert(
                            SavedItemsData.FoundItems[itemType].IndexOf(item),
                            new(() => label, image, rec)
                        );
                    }
                    else
                    {
                        label = GetObjectDisplayName(item);
                        texturePath = Game1.objectData[item].Texture ?? "maps/springobjects";
                        image = ModEntry.HelperInstance.GameContent.Load<Texture2D>(texturePath);
                        rec = GetCropRectangle(image.Width, Game1.objectData[item].SpriteIndex);
                        testImageData.Insert(
                            SavedItemsData.FoundItems[itemType].IndexOf(item),
                            new(() => label, image, rec)
                        );
                    }
                }
                var test = testImageData.ToArray();
                ConfigMenu.AddImageOption(
                    getValue: () => GetValueMethod(animal, itemType, ItemType.Normal),
                    setValue: v => SetValueMethod(v, animal, itemType, ItemType.Normal),
                    optionName: "NormalEgg",
                    getMaxValue: () => (uint)(test.Length - 1),
                    maxImageHeight: () => 64,
                    maxImageWidth: () => 64,
                    drawImage: (v, b, pos) =>
                    {
                        b.Draw(
                            test[v].texture,
                            pos,
                            test[v].rect,
                            Color.White,
                            0,
                            Vector2.Zero,
                            4,
                            SpriteEffects.None,
                            0
                        );
                    },
                    label: (v) => test[v].label(),
                    imageTooltipText: null,
                    arrowLocation: (int)IGMCMOptionsAPI.ImageOptionArrowLocation.Bottom,
                    labelLocation: (int)IGMCMOptionsAPI.ImageOptionLabelLocation.Top
                );
                ConfigMenu.AddImageOption(
                    getValue: () => GetValueMethod(animal, itemType, ItemType.Large),
                    setValue: v => SetValueMethod(v, animal, itemType, ItemType.Large),
                    optionName: "LargeEgg",
                    getMaxValue: () => (uint)(test.Length - 1),
                    maxImageHeight: () => 64,
                    maxImageWidth: () => 64,
                    drawImage: (v, b, pos) =>
                    {
                        b.Draw(
                            test[v].texture,
                            pos,
                            test[v].rect,
                            Color.White,
                            0,
                            Vector2.Zero,
                            4,
                            SpriteEffects.None,
                            0
                        );
                    },
                    label: (v) => test[v].label(),
                    imageTooltipText: null,
                    arrowLocation: (int)IGMCMOptionsAPI.ImageOptionArrowLocation.Bottom,
                    labelLocation: (int)IGMCMOptionsAPI.ImageOptionLabelLocation.Top
                );
            }
        }

        private uint GetValueMethod(string key, CloneItemType itemType, ItemType eggType)
        {
            var saved = ModEntry.Config.ItemToClone[key][itemType][eggType];
            var index = SavedItemsData.FoundItems[itemType].IndexOf(saved);
            if (index == -1)
            {
                ModEntry.Config.ItemToClone[key][itemType][eggType] = string.Empty;
                return 0;
            }
            return (uint)index;
        }

        private void SetValueMethod(
            uint value,
            string key,
            CloneItemType itemType,
            ItemType eggType
        )
        {
            if (value > SavedItemsData.FoundItems[itemType].Count)
            {
                SavedItemsData.AnimalItemRelation[key][itemType] ??= new();
                SavedItemsData.AnimalItemRelation[key][itemType]![eggType] = string.Empty;
            }
            else
            {
                SavedItemsData.AnimalItemRelation[key][itemType] ??= new();
                SavedItemsData.AnimalItemRelation[key][itemType]![eggType] =
                    SavedItemsData.FoundItems[itemType][(int)value];
                ModEntry.Config.ItemToClone[key][itemType][eggType] = SavedItemsData.FoundItems[
                    itemType
                ][(int)value];
            }
        }

        internal Rectangle GetCropRectangle(int imageWidth, int spriteIndex)
        {
            int cropX = 0;
            int cropY = 0;

            int spriteWidth = Math.Min(imageWidth, 16);
            int spriteHeight = Math.Min(imageWidth, 16); // assuming image is square

            try
            {
                cropX = (spriteIndex % (imageWidth / spriteWidth)) * spriteWidth;
                cropY = (spriteIndex / (imageWidth / spriteWidth)) * spriteHeight;
            }
            catch (System.Exception)
            {
                LoggerHelper.VerboseLog($"Error, width: {imageWidth}");
            }

            // Check if cropX is outside the image bounds
            if (cropX + spriteWidth > imageWidth)
            {
                cropX = (cropX + spriteWidth) % imageWidth;
                cropY += spriteHeight; // Move to the next row
            }

            return new Rectangle(cropX, cropY, spriteWidth, spriteHeight);
        }

        public static string GetObjectDisplayName(string objectID)
        {
            var objectData = Game1.objectData[objectID];
            if (objectData.DisplayName.StartsWith('['))
            {
                return TokenParser.ParseText(objectData.DisplayName);
            }
            else
            {
                return objectData.DisplayName;
            }
        }

        public static string GetAnimalDisplayName(string animalKey)
        {
            var animalData = Game1.farmAnimalData[animalKey];
            if (animalData.DisplayName.StartsWith('['))
            {
                return TokenParser.ParseText(animalData.DisplayName);
            }
            else
            {
                return animalData.DisplayName;
            }
        }

        private void MenuUpdate()
        {
            GMCM_save();
            ConfigMenu.Unregister(ModEntry.ModManifestInstance);
            ConfigMenu.Register(reset: () => GMCM_reset(), save: () => GMCM_save());
        }

        private void GMCM_save()
        {
            ModEntry.HelperInstance.WriteConfig(ModEntry.Config);
            GameAssetManager.SaveData(SavedColors);
            GameAssetManager.SaveData(SavedItemsData);
            GameAssetManager.SaveData(BreedingConfigsByPack, "BreedingConfigsByPack");
            foreach (var clonedAnimalInfo in ClonedAnimalsInfoList)
            {
                var maleConfig = BreedingConfigsByPack[clonedAnimalInfo.ContentPackID][
                    clonedAnimalInfo.FemaleKey
                ].MaleConfig;
                clonedAnimalInfo.AddAsAlternatePurchase = maleConfig.AddAsAlternatePurchase;
            }
            // LetMeHatchApi.Instance.SetClonedAnimalsInfo(ClonedAnimalsInfoList);
        }

        private void GMCM_reset()
        {
            ModEntry.Config.Reset();
            foreach (var (contentPackID, breedingConfigDic) in BreedingConfigsByPack)
            {
                foreach (var femaleKey in breedingConfigDic.Keys)
                {
                    var contentPackData = LoadedFemaleBreedingData.FirstOrDefault(d =>
                        d.Key == contentPackID
                    );
                    if (contentPackData.Value == null)
                    {
                        LoggerHelper.VerboseLog("Did not found content pack data", LogLevel.Error);
                        break;
                    }
                    var loadedFemaleData = contentPackData.Value[femaleKey];
                    breedingConfigDic[femaleKey] = new(loadedFemaleData);
                }
            }
            SavedItemsData.ResetRelations(DefaultItemRelations);
            ModEntry.HelperInstance.Data.WriteJsonFile("data/savedModData.json", SavedItemsData);
        }

        private string GetContentPackName(string contentPackID)
        {
            var contentPack = LoadedContentPacks.Find(cp => cp.Manifest.UniqueID == contentPackID);
            if (contentPack != null)
            {
                return contentPack.Manifest.Name;
            }
            return contentPackID;
        }
    }
}
