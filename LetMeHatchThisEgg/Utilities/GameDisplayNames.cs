using StardewValley;

namespace LetMeHatchThisEgg.Utilities;

/// <summary>Centralized helpers for parsing tokenized display names (e.g. "[LocalizedText ...]").</summary>
public static class GameDisplayNames
{
    public static string GetObjectDisplayName(string objectId)
    {
        var objectData = Game1.objectData[objectId];
        return objectData.DisplayName.StartsWith('[')
            ? TokenParser.ParseText(objectData.DisplayName)
            : objectData.DisplayName;
    }

    public static string GetAnimalDisplayName(string animalKey)
    {
        var animalData = Game1.farmAnimalData[animalKey];
        return animalData.DisplayName.StartsWith('[')
            ? TokenParser.ParseText(animalData.DisplayName)
            : animalData.DisplayName;
    }
}
