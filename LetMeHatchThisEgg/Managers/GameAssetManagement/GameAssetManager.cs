using System.Collections.Generic;
using System.IO;
using System.Linq;
using BirdsOfAFeather.Helpers;
using ContentPatcher;
using LetMeHatchThisEgg.Breeding;
using LetMeHatchThisEgg.Cloning;
using LetMeHatchThisEgg.Core.Cloning;
using LetMeHatchThisEgg.Infrastructure.Cloning;
using LetMeHatchThisEgg.Infrastructure.GameData;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Framework;
using Mikew.BirdsOfAFeather.Framework;
using LetMeHatchThisEgg.Helpers;
using LetMeHatchThisEgg.Models;
using Microsoft.Xna.Framework.Graphics;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.FarmAnimals;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.GameAssetManagement
{
    public class GameAssetManager
    {
        #region Fields and Properties
        // Dependencies
        private readonly IContentPatcherAPI _contentPatcherAPI;
        private readonly IWildEggsApi _wildEggsAPI;
        public SavedItemsData SavedItemsData { get; private set; }
        public SavedColors SavedColors { get; private set; }
        public Dictionary<string, Dictionary<string, BreedingConfig>> BreedingConfigsByPack
        {
            get;
            private set;
        }
        public BreedingDataLoader BreedingDataLoader { get; private set; }
        public AnimalItemRelationsManager AnimalItemRelationsManager { get; private set; }
        public ItemManager ItemManager { get; private set; }
        public CloningHelper CloningHelper { get; private set; }
        internal TextureManager TextureManager { get; private set; }
        public MachineEditor MachineEditor { get; private set; }
        public EggListEditor EggListEditor { get; private set; }
        public BirthTextEditor BirthTextEditor { get; private set; }
        public AnimalCloner AnimalCloner { get; private set; }
        public FarmAnimalCloneBatchApplier FarmAnimalCloneBatchApplier { get; private set; }
        public IFarmAnimalAssetAdapter FarmAnimalAssetAdapter { get; private set; }

        public bool HasAddedClonedItems =>
            Game1.objectData.Keys.Any(k => CloningHelper.IsClonedItem(k));
        public bool HasAddedMaleClones =>
            Game1.farmAnimalData.Keys.Any(CloningHelper.IsAClonedAnimal);
        public bool HasAddedFertilizedEggs =>
            Game1.objectData.Keys.Any(k =>
                CloningHelper.IsClonedItem(k, CloneItemType.Egg, fertilized: true)
            );
        public HashSet<string> ClonedItems =>
            Game1.objectData.Keys.Where(k => CloningHelper.IsClonedItem(k)).ToHashSet();

        //Shortcuts
        public Dictionary<string, Dictionary<string, BreedingDataFemale>> LoadedFemaleBreedingData
        {
            get;
            private set;
        }
        public List<ClonedAnimalInfo> ClonedAnimalsInfoList { get; private set; }
        public Dictionary<
            string,
            Dictionary<CloneItemType, Dictionary<string, ObjectData>>
        > ItemsToAdd { get; private set; }
        public List<OutputItemCloneInfo> OutpuItemsToClone { get; private set; }
        public Dictionary<string, ObjectData> FertilizedEggsToAdd { get; private set; }
        public Dictionary<
            string,
            Dictionary<TextureInfoType, ItemTextureInfo>
        > ClonedItemsTextureInfo { get; private set; }
        public List<IContentPack> LoadedContentPacks { get; private set; }

        #endregion

        #region Constructor

        public GameAssetManager(IContentPatcherAPI contentPatcherAPI, IWildEggsApi wildEggsApi)
        {
            _contentPatcherAPI = contentPatcherAPI;
            _wildEggsAPI = wildEggsApi;

            SavedColors = LoadData<SavedColors>();
            SavedItemsData = LoadData<SavedItemsData>();
            BreedingConfigsByPack = LoadData<
                Dictionary<string, Dictionary<string, BreedingConfig>>
            >("BreedingConfigsByPack");

            LoadedFemaleBreedingData = new();
            ClonedAnimalsInfoList = new();
            ClonedItemsTextureInfo = new();
            LoadedContentPacks = new();
            ItemsToAdd = new();
            OutpuItemsToClone = new();
            FertilizedEggsToAdd = new();

            BreedingDataLoader = new BreedingDataLoader(
                _wildEggsAPI,
                LoadedFemaleBreedingData,
                LoadedContentPacks,
                ClonedAnimalsInfoList,
                SavedItemsData
            );
            CloningHelper = new CloningHelper(SavedItemsData, ClonedAnimalsInfoList);
            AnimalItemRelationsManager = new AnimalItemRelationsManager(SavedItemsData);
            TextureManager = new TextureManager(SavedColors, CloningHelper, ClonedItemsTextureInfo);
            ItemManager = new ItemManager(
                TextureManager,
                SavedItemsData,
                SavedColors,
                CloningHelper,
                LoadedFemaleBreedingData,
                ItemsToAdd,
                OutpuItemsToClone,
                FertilizedEggsToAdd
            );
            AnimalCloner = new AnimalCloner(
                new ContentPackStringEvaluator(_contentPatcherAPI, MonitorInstance),
                new CloneTextureBookkeeper()
            );
            FarmAnimalCloneBatchApplier = new FarmAnimalCloneBatchApplier(
                AnimalCloner,
                new CloneMappingService()
            );
            FarmAnimalAssetAdapter = new SmapiFarmAnimalAssetAdapter();
            EggListEditor = new EggListEditor(CloningHelper);
            BirthTextEditor = new BirthTextEditor(_wildEggsAPI);
            MachineEditor = new MachineEditor(ClonedItemsTextureInfo, OutpuItemsToClone);
        }

        #endregion

        #region Load and Save Data

        public static T LoadData<T>(string? fileName = null)
            where T : class, new()
        {
            string path = $"data/{fileName ?? typeof(T).Name}.json";
            string fullPath = Path.Combine(ModEntry.HelperInstance.DirectoryPath, path);

            if (!File.Exists(fullPath))
            {
                SaveData(new T(), fileName);
            }

            return ModEntry.HelperInstance.Data.ReadJsonFile<T>(path) ?? new T();
        }

        public static void SaveData<T>(T data, string? fileName = null)
            where T : class, new()
        {
            string path = $"data/{fileName ?? typeof(T).Name}.json";
            ModEntry.HelperInstance.Data.WriteJsonFile(path, data);
        }

        #endregion

        #region Asset Editing Methods

        public void DayEndingReset()
        {
            ItemsToAdd.Clear();
            FertilizedEggsToAdd.Clear();
            OutpuItemsToClone.Clear();
            MachineEditor.MachineRules.Clear();
            TextureManager.GrayTextures.Clear();
            ClonedItemsTextureInfo.Clear();
        }

        public void HandleClonedEggsAsset(AssetRequestedEventArgs e)
        {
            if (TextureManager.GrayTextures.Count > 0)
            {
                e.LoadFromModFile<Texture2D>(
                    "assets/Objects/clonedeggs.png",
                    AssetLoadPriority.Exclusive
                );

                e.Edit(TextureManager.ProcessClonedItemsTextures, AssetEditPriority.Late);

            }
        }
        #endregion
    }
}