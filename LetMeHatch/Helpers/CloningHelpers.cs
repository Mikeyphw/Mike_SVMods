using System.Text.RegularExpressions;
using LetMeHatch.Framework;
using LetMeHatch.Models;
using Mikew.NestCore.Helpers;
using StardewModdingAPI;
using StardewValley.GameData.Objects;
using static LetMeHatch.EnumsAndStrings.Enums;

using static LetMeHatch.ModEntry;

namespace LetMeHatch.Helpers
{
    public class CloningHelper
    {
        private readonly List<ClonedAnimalInfo> ClonedAnimalsInfoList;
        private readonly SavedItemsData SavedItemsData;

        public CloningHelper(SavedItemsData savedItemsData)
        {
            ClonedAnimalsInfoList = new();
            SavedItemsData = savedItemsData;
        }

        public CloningHelper(SavedItemsData savedItemsData, List<ClonedAnimalInfo> clonedAnimals)
        {
            ClonedAnimalsInfoList = clonedAnimals;
            SavedItemsData = savedItemsData;
        }

        public static bool IsClonedEgg(string clonedEggID)
        {
            return LetMeHatch.Core.Cloning.CloneIdService.IsClonedEgg(clonedEggID);
        }

        private static bool IsClonedEgg(ObjectData originalEggData)
        {
            return originalEggData.ContextTags != null
                && originalEggData.ContextTags.Contains("cloned_egg");
        }

        public string GetFertilizedClonedEggID(string animalKey, string originalEggID)
        {
            if (IsClonedEgg(originalEggID))
            {
                return Regex.Replace(originalEggID, @"_Cloned.*Egg$", "_ClonedFertilizedEgg");
            }
            else
            {
                return GetClonedItemID(
                    animalKey,
                    originalEggID,
                    CloneItemType.Egg,
                    isFertilized: true
                );
            }
        }

        public bool IsClonedItem(
            string clonedItemID,
            CloneItemType? cloneItemType = null,
            bool fertilized = false
        )
        {
            return LetMeHatch.Core.Cloning.CloneIdService.IsClonedItem(
                clonedItemID,
                cloneItemType,
                fertilized
            );
        }

        public string GetClonedItemID(
            string animalKey,
            string originalItemID,
            CloneItemType cloneItemType,
            ItemType itemType = ItemType.Normal,
            bool isFertilized = false
        )
        {
            var uniqueId = ModEntry.ModManifestInstance?.UniqueID;
            if (string.IsNullOrWhiteSpace(uniqueId))
            {
                throw new InvalidOperationException("Mod unique ID is unavailable.");
            }

            return LetMeHatch.Core.Cloning.CloneIdService.GetClonedItemId(
                uniqueId,
                animalKey,
                originalItemID,
                cloneItemType,
                itemType,
                isFertilized
            );
        }
        public static string GetOriginalItemID(
            string input,
            Dictionary<string, Dictionary<TextureInfoType, ItemTextureInfo>> clonedTextInfo
        )
        {
            var textureInfo = clonedTextInfo.Values.FirstOrDefault(t =>
                t[TextureInfoType.Cloned].ID == input
            );
            if (textureInfo == null)
            {
                return string.Empty;
            }
            return textureInfo[TextureInfoType.Original].ID;
        }

        public (string AnimalKey, string OriginalItemID) GetOriginalItemAndAnimalID(string input)
        {
            var patterns = new[]
            {
                new { Pattern = @"^[^#]+#(.*)#(.*)_ClonedFertilizedEgg$" },
                new { Pattern = @"^[^#]+#(.*)#(.*)_Cloned.*Egg$" },
                new { Pattern = @"^[^#]+#(.*)#(.*)_Cloned.*Milk$" },
                new { Pattern = @"^[^#]+#(.*)#(.*)_Cloned.*Misc$" },
            };

            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(input, pattern.Pattern))
                {
                    var regexPattern = new Regex(pattern.Pattern);
                    var match = regexPattern.Match(input);

                    if (match.Success)
                    {
                        try
                        {
                            string animalKey = match.Groups[1].Value;
                            string itemID = match.Groups[2].Value;
                            return (animalKey, itemID);
                        }
                        catch (Exception ex)
                        {
                            LoggerHelper.VerboseLog(
                                $"Failed to decompress egg ID:\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}",
                                LogLevel.Error
                            );
                        }
                    }
                }
            }
            LoggerHelper.VerboseLog(
                $"Failed to get original IDs for input: {input}",
                LogLevel.Error
            );
            return (string.Empty, string.Empty);
        }

        internal static string GetClonedAnimalID(string contentPackID, string originalAnimalKey)
        {
            return $"{contentPackID}_#{originalAnimalKey}#_Male";
        }

        public bool IsAClonedAnimal(string animalKey)
        {
            var clonedInfo = ClonedAnimalsInfoList.Find(x => x.MaleKey == animalKey);
            return clonedInfo != null;
        }
    }
}
