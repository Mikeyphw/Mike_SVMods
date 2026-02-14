using System.Collections.Generic;

namespace WildEggsHatching.Infrastructure.Content;

/// <summary>
/// Centralized constants for wild egg content editing.
/// </summary>
public static class WildEggConstants
{
    /// <summary>The mod's unique ID.</summary>
    public const string ModId = "Mikew.wildeggshatching";

    /// <summary>Item ID prefix for wild eggs.</summary>
    public const string ItemPrefix = ModId + "_";

    /// <summary>Context tags for wild eggs.</summary>
    public static class Tags
    {
        public const string WildEgg = "wildegg_item";
        public const string ForageItem = "forage_item";
        public const string CategoryEgg = "category_egg";
        public const string WildEggMagic = "wildegg_magic";
        public const string WildEggLarge = "wildegg_large";
        public const string WildEggSpecial = "wildegg_special";
        public const string ElixirItem = "elixir_item";
    }

    /// <summary>Special item IDs.</summary>
    public static class Items
    {
        public const string OsirisScroll = ModId + "_OsirisScroll";
        public const string Elixir = ModId + "_ElixirOfRevitalizedEssence";
        public const string WildEggIncubator = ModId + "_WildEggIncubator";
    }

    /// <summary>Building type constants.</summary>
    public static class Buildings
    {
        public const string Coop = "Coop";
        public const string BigCoop = "Big Coop";
        public const string Barn = "Barn";
        public const string BigBarn = "Big Barn";
    }

    /// <summary>Default prices for wild eggs.</summary>
    public static class Prices
    {
        public const int WildEgg = 100;
        public const int LargeWildEgg = 150;
        public const int MagicWildEgg = 100;
        public const int LargeMagicWildEgg = 200;
    }

    /// <summary>Vanilla animal IDs to modify.</summary>
    public static class VanillaAnimals
    {
        public const string Dinosaur = "Dinosaur";
        public const string VoidChicken = "VoidChicken";
        public const string Ostrich = "Ostrich";
    }

    /// <summary>Texture paths.</summary>
    public static class Textures
    {
        public const string Eggs = "Mods/" + ModId + "/Objects/Eggs";
        public const string Scroll = "Mods/" + ModId + "/Objects/Scroll";
        public const string ElixirTexture = "Mods/" + ModId + "/Objects/Elixir";
        public const string Incubator = "Mods/" + ModId + "/BigCraftables/WildEggIncubator";
        public const string Note = "Mods/" + ModId + "/Objects/Note";
    }

    /// <summary>Helper to create egg ID for location.</summary>
    public static string CreateEggId(string location, bool isMagic, bool isLarge)
    {
        string type = "";
        if (isMagic) type += "M";
        if (isLarge) type += "L";
        type += "WEgg";
        return $"{ItemPrefix}{location}{type}";
    }

    /// <summary>Helper to get context tags for egg type.</summary>
    public static string[] GetEggTags(bool isMagic, bool isLarge)
    {
        var tags = new List<string>
        {
            Tags.WildEgg,
            Tags.ForageItem,
            Tags.CategoryEgg,
            isMagic ? "color_purple" : "color_white"
        };

        if (isMagic) tags.Add(Tags.WildEggMagic);
        if (isLarge) tags.Add(Tags.WildEggLarge);

        return tags.ToArray();
    }

    /// <summary>Mail IDs for progression.</summary>
    public static class MailIds
    {
        public const string SpecialOrderDoneI = "wildEggs_specialOrderDoneI";
        public const string SpecialOrderDoneII = "wildEggs_specialOrderDoneII";
        public const string MarnieThanks = "wildEggs_marnieThanks";
        public const string WizardThanks = "wildEggs_wizardThanks";
        public const string WizardDone = "wildEggs_wizardStudyDone";
        public const string WizardFindings = "wildEggs_wizardFindings";
        public const string MarnieGotIncubator = "wildEggs_marnieIncubator";
    }

    /// <summary>Quest IDs for progression.</summary>
    public static class QuestIds
    {
        public const string SpecialOrderWildEggsI = "wildEggs_specialOrderI";
        public const string SpecialOrderWildEggsII = "wildEggs_specialOrderII";
        public const string WizardStudy = "wildEggs_WizardStudy";
        public const string TheDesertScroll = "wildEggs_theDesertScroll";
    }

    /// <summary>Special Order IDs.</summary>
    public static class SpecialOrderIds
    {
        public const string WildEggsI = "wildEggs_specialOrderI";
        public const string WildEggsII = "wildEggs_specialOrderII";
    }

    /// <summary>Vanilla item IDs used as ingredients.</summary>
    public static class VanillaItems
    {
        // Basic resources
        public const string Stone = "388";
        public const string GoldBar = "336";
        public const string IridiumBar = "338";
        
        // Machines/Items
        public const string Crystalarium = "102";
        public const string Incubator = "101";
        public const string OstrichIncubator = "254";
        
        // Animal products
        public const string DinosaurEgg = "107";
        public const string Egg = "194";
        public const string LargeEgg = "195";
        public const string DuckEgg = "430";
        
        // Ferns (for incubators)
        public const string Fern = "259";
        
        // Cooking ingredients
        public const string Wine = "118";
        public const string Pickles = "734";
        public const string Seeds = "114";
        public const string Fruit = "454";
        public const string LifeElixir = "107";
    }

    /// <summary>Vanilla quest types.</summary>
    public static class QuestTypes
    {
        public const string SpecialOrder = "SpecialOrder";
        public const string Collect = "Collect";
        public const string Artifact = "Artifact";
    }

    #region i18n Key Helpers

    /// <summary>
    /// Helper class for i18n translation keys.
    /// </summary>
    public static class I18nKeys
    {
        // Object keys
        public static class Objects
        {
            public const string WildEgg = "wildEgg";
            public const string LargeWildEgg = "largeWildEgg";
            public const string MagicWildEgg = "magicWildEgg";
            public const string LargeMagicWildEgg = "largeMagicWildEgg";
            
            public static string GetEggKey(bool isMagic, bool isLarge)
            {
                if (isMagic && isLarge) return LargeMagicWildEgg;
                if (isMagic) return MagicWildEgg;
                if (isLarge) return LargeWildEgg;
                return WildEgg;
            }
        }

        // Recipe keys
        public static class Recipes
        {
            public const string WildEggIncubator = "wildEggIncubator";
            public const string Elixir = "elixir";
            
            public static string GetDescriptionKey(string recipeKey) => $"recipe.{recipeKey}.description";
            public static string GetNameKey(string recipeKey) => $"recipe.{recipeKey}.name";
        }

        // Mail keys
        public static class Mail
        {
            public const string MarnieThanks = "mail.MarnieThanks";
            public const string WizardDone = "mail.WizardDone";
            public const string SpecialOrderDoneI = "mail.SpecialOrderDoneI";
            public const string SpecialOrderDoneII = "mail.SpecialOrderDoneII";
            public const string WizardThanks = "mail.WizardThanks";
            public const string WizardFindings = "mail.WizardFindings";
            public const string MarnieIncubator = "mail.MarnieIncubator";
            
            // Animal notification keys
            public const string AnimalAvailable0 = "mail.animalAvaliable0";
            public const string AnimalAvailable1 = "mail.animalAvaliable1";
            public const string AnimalAvailable2 = "mail.animalAvaliable2";
            public const string AnimalNew = "mail.animalAvaliable.newanimal";
        }

        // Special Order keys
        public static class SpecialOrders
        {
            public const string WildEggsI = "specialOrder.WildEggIncubatorI";
            public const string WildEggsII = "specialOrder.WildEggIncubatorII";
            
            public static string GetTextKey(string orderKey) => $"{orderKey}.text";
            public static string GetNameKey(string orderKey) => $"{orderKey}.name";
            public static string GetObjectiveKey(string orderKey, int index) => $"{orderKey}.objective.{index}";
        }

        // Junimo Note keys
        public static class JunimoNote
        {
            public const string Name = "object.JunimoNote.name";
            public const string Description = "object.JunimoNote.description";
        }

        // Other object keys
        public static class OtherObjects
        {
            public const string OsirisScroll = "object.OsirisScroll";
            public const string Elixir = "object.ElixirOfRevitalizedEssence";
            public const string WildEggIncubator = "object.WildEggIncubator";
            
            public static string GetNameKey(string objectKey) => $"{objectKey}.name";
            public static string GetDescriptionKey(string objectKey) => $"{objectKey}.description";
        }
    }

    #endregion
}

