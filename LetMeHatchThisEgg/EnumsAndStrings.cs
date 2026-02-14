using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings.ItemIds;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.EnumsAndStrings;

public static class Enums
{
    public enum EditType
    {
        EggList,
        BirthText,
        CloneEggs,
        BreedingSystem
    }

    public enum IntegrationName
    {
        RoosterControlAddon,
        VanillaGame
    }

    public enum ItemType
    {
        Normal,
        Large,
        FertilizedEgg
    }

    public enum TextureInfoType
    {
        Original,
        Cloned
    }

    public enum TextureType
    {
        Adult,
        Baby,
        Harvested,
        Shop
    }

    public enum CloneItemType
    {
        Egg,
        Milk,
        Misc
    }

    public enum ColorOptions
    {
        Saturation,
        Luminance,
        GrayscaleEdit,
        ColorEdit,
        MinLuminance,
        MaxLuminance
    }

    public static class Assets
    {
        public enum Object
        {
            ClonedItems
        }
    }

    public enum AssetFolder
    {
        Objects
    }
}

public static class Strings
{
    // Constants should be static readonly, not static const
    public static readonly int FertilizedPriceMultiplier = 2;
    public static readonly int FertilizedEdibilityIncrement = 4;
    //public static readonly string CustomFieldColorKey = "JollyLlama.PrismaticValleyFramework/Color";
    public static readonly string CustomFieldColorKey = "ClonedColor";
    public static readonly int BASE_PRIORITY = 10000;
    public static readonly string BREEDING_DATA = @"Mods/Mikew.lmh/Data";
    public static readonly int EDIT_BIRTH_TEXT_PRIORITY = BASE_PRIORITY + 3000;
    public static readonly int EDIT_EGG_LIST_PRIORITY = BASE_PRIORITY + 2000;
    public static readonly int BREEDING_SYSTEM_PRIORITY = BASE_PRIORITY + 2500;
    public static readonly int CLONED_EGGS_PRIORITY = BASE_PRIORITY;
    public static readonly int NEW_EGGS_ADD_PRIORITY = BASE_PRIORITY;

    // Define constants for animal prices
    public static readonly int DefaultChickenPrice = 800;
    public static readonly int DefaultDuckPrice = 1200;
    public static readonly int DefaultCowPrice = 800;
    public static readonly int DefaultGoatPrice = 1200;
    public static readonly int DefaultSheepPrice = 800;

    // Define regex patterns as constants
    public static readonly string EggPattern =
        $@"({AnimalProducts.BrownEgg}|{AnimalProducts.WhiteEgg}|{AnimalProducts.LargeWhiteEgg}|{AnimalProducts.LargeBrownEgg})";
    public static readonly string DuckEggPattern = AnimalProducts.DuckEgg;
    public static readonly string VoidEggPattern = AnimalProducts.VoidEgg;
    public static readonly string GoldenEggPattern = AnimalProducts.GoldenEgg;
    public static readonly string OstrichEggPattern = AnimalProducts.OstrichEgg;
    public static readonly string DinoEggPattern = AnimalProducts.DinoEgg;
    public static readonly string SlimeEggPattern =
        $@"({AnimalProducts.BlueSlimeEgg}|{AnimalProducts.RedSlimeEgg}|{AnimalProducts.PurpleSlimeEgg}|{AnimalProducts.GreenSlimeEgg}|{AnimalProducts.TigerSlimeEgg})";

    public static readonly string MilkPattern =
        $@"({AnimalProducts.Milk}|{AnimalProducts.LargeMilk})";
    public static readonly string GoatMilkPattern =
        $@"({AnimalProducts.GoatMilk}|{AnimalProducts.LargeGoatMilk})";
    public static readonly string IridiumMilkPattern = $@"({AnimalProducts.IridiumMilk})";

    public static readonly string WoolPattern = $@"({AnimalProducts.Wool})";
    public static readonly string DuckFeatherPattern = $@"({AnimalProducts.DuckFeather})";

    public static class ColorNames
    {
        public const string Green = "green";
        public const string Blue = "blue";
        public const string Red = "red";
        public const string Purple = "purple";
        public const string Yellow = "yellow";
        public const string Gold = "gold";
        public const string White = "white";
        public const string Black = "black";
        public const string Gray = "gray";
        public const string Pink = "pink";
        public const string Orange = "orange";
        public const string Brown = "brown";
    }

    public static class ColorCategories
    {
        public const string Dark = "dark";
        public const string Bright = "bright";
        public const string Warm = "warm";
        public const string Cool = "cool";
        public const string Muted = "muted";
        public const string Pastel = "pastel";
        public const string Neutral = "neutral";
    }
}