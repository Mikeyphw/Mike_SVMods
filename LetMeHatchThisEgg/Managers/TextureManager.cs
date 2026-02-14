// using System.Text.RegularExpressions;
// using LetMeHatchThisEgg.Cloning;
// using LetMeHatchThisEgg.Config;
// using LetMeHatchThisEgg.Helpers;
// using LetMeHatchThisEgg.Models;
// using Microsoft.Xna.Framework.Graphics;
// using Mikew.BirdsOfAFeather.Helpers;
// using StardewModdingAPI;
// using StardewModdingAPI.Events;
// using StardewValley;
// using StardewValley.GameData.Objects;
// using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
// using static LetMeHatchThisEgg.EnumsAndStrings.Strings;
// using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.ItemIds;
// using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;

// using static LetMeHatchThisEgg.ModEntry; namespace LetMeHatchThisEgg.AssetManagement
// {
//     public class TextureManager
//     {
//         public Dictionary<string, int> ItemIndexMap = new();
//         public Dictionary<string, Dictionary<string, Texture2D?>> GrayTextures = new();
//         private readonly SavedColors SavedColors;
//         private readonly ModAssetHandler AssetHandler;
//         private readonly IModHelper Helper;
//         private readonly IMonitor Monitor;
//         private readonly ModConfig Config;
//         private readonly CloningHelper CloningHelper;
//         public Dictionary<
//             string,
//             Dictionary<TextureInfoType, ItemTextureInfo>
//         > ClonedItemsTextureInfo { get; private set; } = new();
//         private int clonedEggsSpriteIndex = -1;

//         public TextureManager(
//             IModHelper helper,
//             IMonitor monitor,
//             ModConfig modConfig,
//             ModAssetHandler assetHandler,
//             SavedColors savedColors,
//             CloningHelper cloningHelper
//         )
//         {
//             Helper = helper;
//             Monitor = monitor;
//             Config = modConfig;
//             AssetHandler = assetHandler;
//             SavedColors = savedColors;
//             ItemIndexMap = GetItemIndexMap();
//             CloningHelper = cloningHelper;
//         }

//         public void SetUpModAssets()
//         {
//             if (AssetHandler == null)
//             {
//                 return;
//             }
//             AssetHandler.AssetsData = new()
//             {
//                 new(typeof(Assets.Object), AssetFolder.Objects, FileExtension.Png, null),
//             };
//             AssetHandler.SetUpAssets();
//         }

//         private Dictionary<string, int> GetItemIndexMap()
//         {
//             var items = new List<string>();
//             foreach (var egg in AnimalProducts.VanillaEggs.OrderBy(x => int.Parse(x)))
//             {
//                 items.Add(egg);
//             }
//             foreach (var milk in AnimalProducts.VanillaMilk.OrderBy(x => int.Parse(x)))
//             {
//                 items.Add(milk);
//             }
//             items.Add(ArtisanGoods.Cloth);
//             items.Add(AnimalProducts.Wool);
//             items.Add(AnimalProducts.DuckFeather);
//             items.Add(AnimalProducts.RabbitFoot);

//             foreach (var item in items)
//             {
//                 ItemIndexMap[item] = items.IndexOf(item);
//             }
//             // clonedEggsSpriteIndex = items.Count;
//             return ItemIndexMap;
//         }

//         public void SetTextureAndSpriteIndex(
//             ObjectData clonedData,
//             ObjectData originalData,
//             string animalKey,
//             string originalID,
//             string clonedID,
//             CloneItemType cloneItemType,
//             bool isFertilized = false // New parameter to indicate if the item is fertilized
//         )
//         {
//             if (ClonedItemsTextureInfo.TryGetValue(clonedID, out var textureInfo))
//             {
//                 var textureInfoCloned = textureInfo[TextureInfoType.Cloned];
//                 clonedData.Texture = textureInfoCloned.TextureName;
//                 clonedData.SpriteIndex = textureInfoCloned.SpriteIndex;
//                 return;
//             }
//             else if (isFertilized && CloningHelper.IsClonedItem(originalID, CloneItemType.Egg))
//             {
//                 ClonedItemsTextureInfo[clonedID] = new(ClonedItemsTextureInfo[originalID]);
//                 ClonedItemsTextureInfo[clonedID][TextureInfoType.Original].ID = originalID;
//                 ClonedItemsTextureInfo[clonedID][TextureInfoType.Cloned].ID = clonedID;
//             }
//             else
//             {
//                 int spriteIndex = 0;
//                 string texture = string.Empty;
//                 bool isColored = false;

//                 if (
//                     cloneItemType != CloneItemType.Milk
//                     || (cloneItemType == CloneItemType.Milk && ModEntry.Config.ColorClonedMilk)
//                 )
//                 {
//                     spriteIndex = ++clonedEggsSpriteIndex;
//                     LoggerHelper.VerboseLog($"Sprite Index changed to {clonedEggsSpriteIndex}", LogLevel.Alert);
//                     texture = AssetHandler.GetAssetPath(
//                         Assets.Object.ClonedItems,
//                         AssetPathType.GamePath
//                     );
//                     isColored = true;
//                 }
//                 else if (!Config.ColorClonedMilk)
//                 {
//                     spriteIndex = originalData.SpriteIndex;
//                     texture = originalData.Texture;
//                     isColored = false;
//                 }

//                 ClonedItemsTextureInfo[clonedID] = new()
//                 {
//                     {
//                         TextureInfoType.Original,
//                         new(
//                             originalID,
//                             originalData.SpriteIndex,
//                             cloneItemType,
//                             originalData.Texture,
//                             isColored
//                         )
//                     },
//                     {
//                         TextureInfoType.Cloned,
//                         new(clonedID, spriteIndex, cloneItemType, texture, isColored)
//                     },
//                 };
//             }
//             // Recursion for setting the texture and sprite index for the cloned item
//             SetTextureAndSpriteIndex(
//                 clonedData,
//                 originalData,
//                 animalKey,
//                 originalID,
//                 clonedID,
//                 cloneItemType,
//                 false
//             );

//             Helper.Data.WriteJsonFile("TextureInfonew.json", ClonedItemsTextureInfo);
//         }

//         public void LoadTextureFromContentPack(
//             string assetName,
//             List<ClonedAnimalInfo> clonedAnimalsInfoList,
//             List<IContentPack> loadedContentPacks,
//             AssetRequestedEventArgs e,
//             AssetLoadPriority priority
//         )
//         {
//             foreach (var info in clonedAnimalsInfoList)
//             {
//                 foreach (var (textureType, texture) in info.Textures)
//                 {
//                     if (string.Equals(texture, assetName, StringComparison.OrdinalIgnoreCase))
//                     {
//                         var contentPack = loadedContentPacks.Find(cp =>
//                             cp.Manifest.UniqueID == info.ContentPackID
//                         );

//                         if (contentPack != null)
//                         {
//                             string pattern = @"^Animals/MaleClones/([^/]+)/([^/]+)/([^/]+)$";
//                             Match match = Regex.Match(assetName, pattern, RegexOptions.IgnoreCase);
//                             if (match.Success)
//                             {
//                                 string result =
//                                     $"textures/{info.FemaleKey}/{match.Groups[3].Value}.png";

//                                 try
//                                 {
//                                     var texturePath = contentPack.ModContent.Load<Texture2D>(
//                                         result
//                                     );
//                                     e.LoadFrom(() => texturePath, priority);
//                                 }
//                                 catch (Exception ex)
//                                 {
//                                     LoggerHelper.VerboseLog($"Error loading texture: {ex.Message}");
//                                 }
//                             }
//                         }
//                     }
//                 }
//             }
//         }

//         public void ProcessClonedTexturesWithGrayColor(IDictionary<string, ObjectData> objectData)
//         {
//             var log = new List<(string message, LogLevel level)>();

//             foreach (var (clonedTextureInfo, originalTextureInfo) in GetColoredClonedItems())
//             {
//                 var textureName = originalTextureInfo.TextureName;
//                 var clonedItemID = clonedTextureInfo.ID;

//                 InitializeGrayTexture(textureName, clonedItemID);

//                 if (GrayTextures[textureName][clonedItemID] != null)
//                 {
//                     continue;
//                 }

//                 var originalTexture = LoadOriginalTexture(textureName, log);
//                 if (originalTexture == null)
//                 {
//                     continue;
//                 }

//                 var croppedImage = CropOriginalTexture(originalTexture, originalTextureInfo);
//                 var animal = CloningHelper.GetOriginalItemAndAnimalID(clonedItemID).AnimalKey;

//                 if (
//                     !TryApplyColor(objectData, clonedItemID, croppedImage, log, textureName, animal)
//                 )
//                 {
//                     HandleNoColorFound(
//                         clonedItemID,
//                         croppedImage,
//                         log,
//                         textureName,
//                         objectData,
//                         animal
//                     );
//                 }
//             }

//             FinalizeProcessing(log);
//         }

//         private IEnumerable<(
//             ItemTextureInfo cloned,
//             ItemTextureInfo original
//         )> GetColoredClonedItems()
//         {
//             return ClonedItemsTextureInfo
//                 .Values.Select(t => (t[TextureInfoType.Cloned], t[TextureInfoType.Original]))
//                 .Where(t => t.Item1.IsColored);
//         }

//         private void InitializeGrayTexture(string textureName, string clonedItemID)
//         {
//             GrayTextures.TryAdd(textureName, new());
//             GrayTextures[textureName].TryAdd(clonedItemID, null);
//         }

//         private Texture2D? LoadOriginalTexture(
//             string textureName,
//             List<(string message, LogLevel level)> log
//         )
//         {
//             var originalTexture = Helper.GameContent.Load<Texture2D>(textureName);
//             if (originalTexture == null)
//             {
//                 LoggerHelper.VerboseLog($"Failed to load original texture: {textureName}", LogLevel.Error);
//             }
//             return originalTexture;
//         }

//         private Texture2D CropOriginalTexture(
//             Texture2D originalTexture,
//             ItemTextureInfo originalTextureInfo
//         )
//         {
//             return Colors.CropTexture(
//                 originalTexture,
//                 originalTextureInfo.GetCropRectangle(originalTexture.Width)
//             );
//         }

//         private bool TryApplyColor(
//             IDictionary<string, ObjectData> objectData,
//             string clonedItemID,
//             Texture2D croppedImage,
//             List<(string message, LogLevel level)> log,
//             string textureName,
//             string animal
//         )
//         {
//             bool colorFound = false;

//             colorFound |= ApplyCustomFieldColor(
//                 objectData,
//                 clonedItemID,
//                 croppedImage,
//                 log,
//                 textureName
//             );
//             colorFound |= ApplySavedColor(animal, croppedImage, log, textureName, clonedItemID);
//             colorFound |= ApplyColorFromDescription(
//                 animal,
//                 croppedImage,
//                 log,
//                 textureName,
//                 clonedItemID
//             );

//             return colorFound;
//         }

//         private bool ApplyCustomFieldColor(
//             IDictionary<string, ObjectData> objectData,
//             string clonedItemID,
//             Texture2D croppedImage,
//             List<(string message, LogLevel level)> log,
//             string textureName
//         )
//         {
//             if (
//                 objectData.TryGetValue(clonedItemID, out var obj)
//                 && obj.CustomFields?.ContainsKey(CustomFieldColorKey) == true
//             )
//             {
//                 var hexCode = obj.CustomFields[CustomFieldColorKey];
//                 log.Add(
//                     ($"Applying color {hexCode} to cloned item ID: {clonedItemID}", LogLevel.Debug)
//                 );
//                 GrayTextures[textureName][clonedItemID] = Colors.ColorImage(
//                     Colors.ConvertToGrayscale(croppedImage),
//                     hexCode
//                 );
//                 return true;
//             }
//             return false;
//         }

//         private bool ApplySavedColor(
//             string animal,
//             Texture2D croppedImage,
//             List<(string message, LogLevel level)> log,
//             string textureName,
//             string clonedItemID
//         )
//         {
//             if (
//                 !string.IsNullOrEmpty(animal)
//                 && SavedColors.ItemColors.TryGetValue(animal, out var color)
//             )
//             {
//                 var hexCode = color;
//                 log.Add(
//                     ($"Applying color {hexCode} to cloned item ID: {clonedItemID}", LogLevel.Debug)
//                 );
//                 GrayTextures[textureName][clonedItemID] = Colors.ColorImage(
//                     Colors.ConvertToGrayscale(croppedImage),
//                     hexCode
//                 );
//                 return true;
//             }
//             return false;
//         }

//         private bool ApplyColorFromDescription(
//             string animal,
//             Texture2D croppedImage,
//             List<(string message, LogLevel level)> log,
//             string textureName,
//             string clonedItemID
//         )
//         {
//             if (!SavedColors.ItemColors.ContainsKey(animal))
//             {
//                 log.Add(($"Coloring Item from animal {animal}", LogLevel.Warn));
//                 var stringToExtractFrom =
//                     $"{Game1.farmAnimalData[animal].ShopDescription ?? string.Empty} {Game1.farmAnimalData[animal].DisplayName} {animal}";
//                 ItemManager.ColorTheItem(
//                     SavedColors,
//                     Config,
//                     stringToExtractFrom,
//                     animal,
//                     clonedItemID
//                 );
//                 var hexCode = SavedColors.ItemColors[animal];
//                 log.Add(($"Set Color {hexCode}", LogLevel.Warn));
//                 GrayTextures[textureName][clonedItemID] = Colors.ColorImage(
//                     Colors.ConvertToGrayscale(croppedImage),
//                     hexCode
//                 );
//                 return true;
//             }
//             return false;
//         }

//         private void HandleNoColorFound(
//             string clonedItemID,
//             Texture2D croppedImage,
//             List<(string message, LogLevel level)> log,
//             string textureName,
//             IDictionary<string, ObjectData> objectData,
//             string animal
//         )
//         {
//             string reason = "No color found. ";
//             // Detailed logging for debugging
//             if (!objectData.TryGetValue(clonedItemID, out var obj))
//             {
//                 reason += $"Object data not found for cloned item ID: {clonedItemID}. ";
//             }
//             else if (obj.CustomFields == null || !obj.CustomFields.ContainsKey(CustomFieldColorKey))
//             {
//                 reason += "Custom fields not found or missing color key. ";
//             }
//             if (string.IsNullOrEmpty(animal) || !SavedColors.ItemColors.ContainsKey(animal))
//             {
//                 reason += $"Animal key {animal} is invalid or no color associated with it. ";
//             }

//             log.Add(
//                 (
//                     $"No color found for cloned item ID: {clonedItemID}. Reason: {reason}",
//                     LogLevel.Debug
//                 )
//             );
//             GrayTextures[textureName][clonedItemID] = croppedImage;
//         }

//         private void FinalizeProcessing(List<(string message, LogLevel level)> log)
//         {
//             Helper.GameContent.InvalidateCache(
//                 AssetHandler.GetAssetPath(Assets.Object.ClonedItems, AssetPathType.GamePath)
//             );
//             Helper.Data.WriteJsonFile("gray/log.json", string.Join(Environment.NewLine, log));
//             Helper.Data.WriteJsonFile("grayKeys.json", GrayTextures.Keys);
//         }

//         public void ProcessClonedItemsTextures(IAssetData asset)
//         {
//             var editor = asset.AsImage();

//             Helper.Data.WriteJsonFile("clonedeggtextureinfo.json", ClonedItemsTextureInfo);

//             foreach (
//                 var info in ClonedItemsTextureInfo
//                     .Where(t => t.Value[TextureInfoType.Cloned].IsColored)
//                     .Select(ti => ti.Value)
//             )
//             {
//                 var clonedItemID = info[TextureInfoType.Cloned].ID;

//                 var textureName = info[TextureInfoType.Original].TextureName;

//                 // Check for gray texture

//                 if (!GrayTextures.ContainsKey(textureName))
//                 {
//                     LogMissingGrayTexture(textureName);

//                     continue;
//                 }

//                 var grayTexture = GrayTextures[textureName][clonedItemID];

//                 if (grayTexture == null)
//                 {
//                     LoggerHelper.VerboseLog($"Null gray texture for {textureName}!", LogLevel.Error);

//                     return;
//                 }

//                 // Process the texture rectangles

//                 ProcessTextureRectangles(editor, info, grayTexture);
//             }
//         }

//         private void LogMissingGrayTexture(string textureName)
//         {
//             var objectsWithTexture = Game1
//                 .objectData.Where(o => o.Value.Texture != null && o.Value.Texture == textureName)
//                 .Select(o => o.Key);

//             LoggerHelper.VerboseLog(
//                 $"Missing gray texture for texture {textureName}. Objects with this texture:\n{string.Join(", \n", objectsWithTexture)}",
//                 LogLevel.Error
//             );
//         }

//         private void ProcessTextureRectangles(
//             IAssetDataForImage editor,
//             Dictionary<TextureInfoType, ItemTextureInfo> info,
//             Texture2D grayTexture
//         )
//         {
//             var targetRectangle = info[TextureInfoType.Cloned].GetCropRectangle(editor.Data.Width);

//             var sourceRectangle = info[TextureInfoType.Original]
//                 .GetCropRectangle(grayTexture.Width);

//             targetRectangle.Width = sourceRectangle.Width;

//             targetRectangle.Height = sourceRectangle.Height;

//             editor.ExtendImage(
//                 editor.Data.Width,
//                 Math.Max(editor.Data.Height, targetRectangle.Y + 16)
//             );

//             try
//             {
//                 editor.PatchImage(grayTexture, null, targetRectangle, PatchMode.Replace);
//             }
//             catch (Exception ex)
//             {
//                 LoggerHelper.VerboseLog($"tagetRec: ({targetRectangle.X},{targetRectangle.Y})");
//             }
//         }
//     }
// }
using System.Text.RegularExpressions;
using LetMeHatchThisEgg.Cloning;
using LetMeHatchThisEgg.Config;
using LetMeHatchThisEgg.Helpers;
using LetMeHatchThisEgg.Models;
using Microsoft.Xna.Framework.Graphics;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using static LetMeHatchThisEgg.EnumsAndStrings.Strings;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.ItemIds;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;

using static LetMeHatchThisEgg.ModEntry; namespace LetMeHatchThisEgg.GameAssetManagement
{
    public class TextureManager
    {
        // Dictionary to map item names to their indices
        public Dictionary<string, int> ItemIndexMap { get; private set; } = new();

        // Dictionary to hold gray textures for cloned items
        public Dictionary<string, Dictionary<string, Texture2D?>> GrayTextures
        {
            get;
            private set;
        } = new();

        // Dependencies
        private readonly SavedColors _savedColors;
        private readonly CloningHelper _cloningHelper;
        private readonly Dictionary<
            string,
            Dictionary<TextureInfoType, ItemTextureInfo>
        > _clonedItemsTextureInfo;

        private int _clonedEggsSpriteIndex = -1;

        public TextureManager(
            SavedColors savedColors,
            CloningHelper cloningHelper,
            Dictionary<string, Dictionary<TextureInfoType, ItemTextureInfo>> clonedItemsTextureInfo
        )
        {
            _savedColors = savedColors;
            _cloningHelper = cloningHelper;
            _clonedItemsTextureInfo = clonedItemsTextureInfo;
            ItemIndexMap = InitializeItemIndexMap();
        }

        public void SetUpModAssets()
        {
            AssetHandler.AddAssets(
                new() { new(typeof(Assets.Object), AssetFolder.Objects, FileExtension.Png, null), }
            );
            AssetHandler.SetUpAssets();
        }

        private Dictionary<string, int> InitializeItemIndexMap()
        {
            var items = new List<string>();

            // Collecting vanilla eggs and milk
            items.AddRange(AnimalProducts.VanillaEggs.OrderBy(x => int.Parse(x)));
            items.AddRange(AnimalProducts.VanillaMilk.OrderBy(x => int.Parse(x)));

            // Adding additional items
            items.Add(ArtisanGoods.Cloth);
            items.Add(AnimalProducts.Wool);
            items.Add(AnimalProducts.DuckFeather);
            items.Add(AnimalProducts.RabbitFoot);

            // Mapping items to their indices
            return items
                .Select((item, index) => new { item, index })
                .ToDictionary(x => x.item, x => x.index);
        }

        public void SetTextureAndSpriteIndex(
            ObjectData clonedData,
            ObjectData originalData,
            string animalKey,
            string originalID,
            string clonedID,
            CloneItemType cloneItemType,
            bool isFertilized = false
        )
        {
            if (_clonedItemsTextureInfo.TryGetValue(clonedID, out var textureInfo))
            {
                // Set texture and sprite index for cloned item
                var textureInfoCloned = textureInfo[TextureInfoType.Cloned];
                clonedData.Texture = textureInfoCloned.TextureName;
                clonedData.SpriteIndex = textureInfoCloned.SpriteIndex;
                return;
            }

            // Handle fertilized cloned items
            if (isFertilized && _cloningHelper.IsClonedItem(originalID, CloneItemType.Egg))
            {
                CloneTextureInfo(originalID, clonedID);
                return;
            }

            // Handle normal cloning
            HandleNormalCloning(originalData, originalID, clonedID, cloneItemType);
            // Recursively set texture for the cloned item
            SetTextureAndSpriteIndex(
                clonedData,
                originalData,
                animalKey,
                originalID,
                clonedID,
                cloneItemType,
                false
            );

            // Save cloned items texture info
            ModEntry.HelperInstance.Data.WriteJsonFile(
                "TextureInfonew.json",
                _clonedItemsTextureInfo
            );
        }

        private void CloneTextureInfo(string originalID, string clonedID)
        {
            _clonedItemsTextureInfo[clonedID] = new(_clonedItemsTextureInfo[originalID]);
            _clonedItemsTextureInfo[clonedID][TextureInfoType.Original].ID = originalID;
            _clonedItemsTextureInfo[clonedID][TextureInfoType.Cloned].ID = clonedID;
        }

        private void HandleNormalCloning(
            ObjectData originalData,
            string originalID,
            string clonedID,
            CloneItemType cloneItemType
        )
        {
            int spriteIndex = 0;
            string texture = string.Empty;
            bool isColored = false;

            if (
                cloneItemType != CloneItemType.Milk
                || (cloneItemType == CloneItemType.Milk && ModEntry.Config.ColorClonedMilk)
            )
            {
                spriteIndex = ++_clonedEggsSpriteIndex;
                // LoggerHelper.Log.Log($"Sprite Index changed to {_clonedEggsSpriteIndex}", LogLevel.Alert);
                texture = AssetHandler.GetAssetPath(
                    Assets.Object.ClonedItems,
                    AssetPathType.GamePath
                );
                isColored = true;
            }
            else if (!ModEntry.Config.ColorClonedMilk)
            {
                spriteIndex = originalData.SpriteIndex;
                texture = originalData.Texture;
                isColored = false;
            }

            _clonedItemsTextureInfo[clonedID] = new()
            {
                {
                    TextureInfoType.Original,
                    new(
                        originalID,
                        originalData.SpriteIndex,
                        cloneItemType,
                        originalData.Texture,
                        isColored
                    )
                },
                {
                    TextureInfoType.Cloned,
                    new(clonedID, spriteIndex, cloneItemType, texture, isColored)
                },
            };
        }

        public void LoadTextureFromContentPack(
            string assetName,
            List<ClonedAnimalInfo> clonedAnimalsInfoList,
            List<IContentPack> loadedContentPacks,
            AssetRequestedEventArgs e,
            AssetLoadPriority priority
        )
        {
            foreach (var info in clonedAnimalsInfoList)
            {
                foreach (var (textureType, texture) in info.Textures)
                {
                    if (string.Equals(texture, assetName, StringComparison.OrdinalIgnoreCase))
                    {
                        var contentPack = loadedContentPacks.Find(cp =>
                            cp.Manifest.UniqueID == info.ContentPackID
                        );

                        if (contentPack != null)
                        {
                            string pattern = @"^Animals/MaleClones/([^/]+)/([^/]+)/([^/]+)$";
                            Match match = Regex.Match(assetName, pattern, RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                string result =
                                    $"textures/{info.FemaleKey}/{match.Groups[3].Value}.png";

                                try
                                {
                                    var texturePath = contentPack.ModContent.Load<Texture2D>(
                                        result
                                    );
                                    e.LoadFrom(() => texturePath, priority);
                                }
                                catch (Exception ex)
                                {
                                    LoggerHelper.VerboseLog($"Error loading texture: {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ProcessClonedTexturesWithGrayColor(IDictionary<string, ObjectData> objectData)
        {
            var log = new List<(string message, LogLevel level)>();

            foreach (var (clonedTextureInfo, originalTextureInfo) in GetColoredClonedItems())
            {
                var textureName = originalTextureInfo.TextureName;
                var clonedItemID = clonedTextureInfo.ID;

                InitializeGrayTexture(textureName, clonedItemID);

                if (GrayTextures[textureName][clonedItemID] != null)
                {
                    continue;
                }

                var originalTexture = LoadOriginalTexture(textureName, log);
                if (originalTexture == null)
                {
                    continue;
                }

                var croppedImage = CropOriginalTexture(originalTexture, originalTextureInfo);
                var animal = _cloningHelper.GetOriginalItemAndAnimalID(clonedItemID).AnimalKey;

                if (
                    !TryApplyColor(objectData, clonedItemID, croppedImage, log, textureName, animal)
                )
                {
                    HandleNoColorFound(
                        clonedItemID,
                        croppedImage,
                        log,
                        textureName,
                        objectData,
                        animal
                    );
                }
            }

            FinalizeProcessing(log);
        }

        private IEnumerable<(
            ItemTextureInfo cloned,
            ItemTextureInfo original
        )> GetColoredClonedItems()
        {
            return _clonedItemsTextureInfo
                .Values.Select(t => (t[TextureInfoType.Cloned], t[TextureInfoType.Original]))
                .Where(t => t.Item1.IsColored);
        }

        private void InitializeGrayTexture(string textureName, string clonedItemID)
        {
            GrayTextures.TryAdd(textureName, new());
            GrayTextures[textureName].TryAdd(clonedItemID, null);
        }

        private Texture2D? LoadOriginalTexture(
            string textureName,
            List<(string message, LogLevel level)> log
        )
        {
            var originalTexture = ModEntry.HelperInstance.GameContent.Load<Texture2D>(textureName);
            if (originalTexture == null)
            {
                LoggerHelper.VerboseLog($"Failed to load original texture: {textureName}", LogLevel.Error);
            }
            return originalTexture;
        }

        private Texture2D CropOriginalTexture(
            Texture2D originalTexture,
            ItemTextureInfo originalTextureInfo
        )
        {
            return Colors.CropTexture(
                originalTexture,
                originalTextureInfo.GetCropRectangle(originalTexture.Width)
            );
        }

        private bool TryApplyColor(
            IDictionary<string, ObjectData> objectData,
            string clonedItemID,
            Texture2D croppedImage,
            List<(string message, LogLevel level)> log,
            string textureName,
            string animal
        )
        {
            bool colorFound = false;

            colorFound |= ApplyCustomFieldColor(
                objectData,
                clonedItemID,
                croppedImage,
                log,
                textureName
            );
            colorFound |= ApplySavedColor(animal, croppedImage, log, textureName, clonedItemID);
            colorFound |= ApplyColorFromDescription(
                animal,
                croppedImage,
                log,
                textureName,
                clonedItemID
            );

            return colorFound;
        }

        private bool ApplyCustomFieldColor(
            IDictionary<string, ObjectData> objectData,
            string clonedItemID,
            Texture2D croppedImage,
            List<(string message, LogLevel level)> log,
            string textureName
        )
        {
            if (
                objectData.TryGetValue(clonedItemID, out var obj)
                && obj.CustomFields?.ContainsKey(CustomFieldColorKey) == true
            )
            {
                var hexCode = obj.CustomFields[CustomFieldColorKey];
                log.Add(
                    ($"Applying color {hexCode} to cloned item ID: {clonedItemID}", LogLevel.Debug)
                );
                GrayTextures[textureName][clonedItemID] = Colors.ColorImage(
                    Colors.ConvertToGrayscale(croppedImage),
                    hexCode
                );
                return true;
            }
            return false;
        }

        private bool ApplySavedColor(
            string animal,
            Texture2D croppedImage,
            List<(string message, LogLevel level)> log,
            string textureName,
            string clonedItemID
        )
        {
            if (
                !string.IsNullOrEmpty(animal)
                && _savedColors.ItemColors.TryGetValue(animal, out var color)
            )
            {
                var hexCode = color;
                log.Add(
                    ($"Applying color {hexCode} to cloned item ID: {clonedItemID}", LogLevel.Debug)
                );
                GrayTextures[textureName][clonedItemID] = Colors.ColorImage(
                    Colors.ConvertToGrayscale(croppedImage),
                    hexCode
                );
                return true;
            }
            return false;
        }

        private bool ApplyColorFromDescription(
            string animal,
            Texture2D croppedImage,
            List<(string message, LogLevel level)> log,
            string textureName,
            string clonedItemID
        )
        {
            if (!_savedColors.ItemColors.ContainsKey(animal))
            {
                log.Add(($"Coloring Item from animal {animal}", LogLevel.Warn));
                var stringToExtractFrom =
                    $"{Game1.farmAnimalData[animal].ShopDescription ?? string.Empty} {Game1.farmAnimalData[animal].DisplayName} {animal}";
                ItemManager.ColorTheItem(
                    _savedColors,
                    ModEntry.Config,
                    stringToExtractFrom,
                    animal,
                    clonedItemID
                );
                var hexCode = _savedColors.ItemColors[animal];
                log.Add(($"Set Color {hexCode}", LogLevel.Warn));
                GrayTextures[textureName][clonedItemID] = Colors.ColorImage(
                    Colors.ConvertToGrayscale(croppedImage),
                    hexCode
                );
                return true;
            }
            return false;
        }

        private void HandleNoColorFound(
            string clonedItemID,
            Texture2D croppedImage,
            List<(string message, LogLevel level)> log,
            string textureName,
            IDictionary<string, ObjectData> objectData,
            string animal
        )
        {
            string reason = "No color found. ";
            // Detailed logging for debugging
            if (!objectData.TryGetValue(clonedItemID, out var obj))
            {
                reason += $"Object data not found for cloned item ID: {clonedItemID}. ";
            }
            else if (obj.CustomFields == null || !obj.CustomFields.ContainsKey(CustomFieldColorKey))
            {
                reason += "Custom fields not found or missing color key. ";
            }
            if (string.IsNullOrEmpty(animal) || !_savedColors.ItemColors.ContainsKey(animal))
            {
                reason += $"Animal key {animal} is invalid or no color associated with it. ";
            }

            log.Add(
                (
                    $"No color found for cloned item ID: {clonedItemID}. Reason: {reason}",
                    LogLevel.Debug
                )
            );
            GrayTextures[textureName][clonedItemID] = croppedImage;
        }

        private void FinalizeProcessing(List<(string message, LogLevel level)> log)
        {
            ModEntry.HelperInstance.GameContent.InvalidateCache(
                AssetHandler.GetAssetPath(Assets.Object.ClonedItems, AssetPathType.GamePath)
            );
            ModEntry.HelperInstance.Data.WriteJsonFile(
                "gray/log.json",
                string.Join(Environment.NewLine, log)
            );
            ModEntry.HelperInstance.Data.WriteJsonFile("grayKeys.json", GrayTextures.Keys);
        }

        public void ProcessClonedItemsTextures(IAssetData asset)
        {
            var editor = asset.AsImage();

            ModEntry.HelperInstance.Data.WriteJsonFile(
                "clonedeggtextureinfo.json",
                _clonedItemsTextureInfo
            );

            foreach (
                var info in _clonedItemsTextureInfo
                    .Where(t => t.Value[TextureInfoType.Cloned].IsColored)
                    .Select(ti => ti.Value)
            )
            {
                var clonedItemID = info[TextureInfoType.Cloned].ID;

                var textureName = info[TextureInfoType.Original].TextureName;

                // Check for gray texture

                if (!GrayTextures.ContainsKey(textureName))
                {
                    LogMissingGrayTexture(textureName);

                    continue;
                }

                var grayTexture = GrayTextures[textureName][clonedItemID];

                if (grayTexture == null)
                {
                    LoggerHelper.VerboseLog($"Null gray texture for {textureName}!", LogLevel.Error);

                    return;
                }

                // Process the texture rectangles

                ProcessTextureRectangles(editor, info, grayTexture);
            }
        }

        private void LogMissingGrayTexture(string textureName)
        {
            var objectsWithTexture = Game1
                .objectData.Where(o => o.Value.Texture != null && o.Value.Texture == textureName)
                .Select(o => o.Key);

            LoggerHelper.VerboseLog(
                $"Missing gray texture for texture {textureName}. Objects with this texture:\n{string.Join(", \n", objectsWithTexture)}",
                LogLevel.Error
            );
        }

        private void ProcessTextureRectangles(
            IAssetDataForImage editor,
            Dictionary<TextureInfoType, ItemTextureInfo> info,
            Texture2D grayTexture
        )
        {
            var targetRectangle = info[TextureInfoType.Cloned].GetCropRectangle(editor.Data.Width);

            var sourceRectangle = info[TextureInfoType.Original]
                .GetCropRectangle(grayTexture.Width);

            targetRectangle.Width = sourceRectangle.Width;

            targetRectangle.Height = sourceRectangle.Height;

            editor.ExtendImage(
                editor.Data.Width,
                Math.Max(editor.Data.Height, targetRectangle.Y + 16)
            );

            try
            {
                editor.PatchImage(grayTexture, null, targetRectangle, PatchMode.Replace);
            }
            catch (Exception ex)
            {
                LoggerHelper.VerboseLog($"tagetRec: ({targetRectangle.X},{targetRectangle.Y})");
            }
        }
    }
}
