namespace Mikew.BirdsOfAFeather.EnumsAndStrings;

public static class Enums
{
    public enum LuckyTypes
    {
        SuperUnlucky,
        Unlucky,
        Neutral,
        Lucky,
        SuperLucky,
    }

    public enum LogType
    {
        Normal,
        Verbose,
        Testing,
    }

    public enum ListEditType
    {
        Add,
        Remove,
        Replace,
    }
}

public static class Strings
{
    public static class VanillaAnimals
    {
        public const string WhiteChicken = "White Chicken";
        public const string VoidChicken = "Void Chicken";
        public const string BrownChicken = "Brown Chicken";
        public const string Duck = "Duck";
        public const string Dinosaur = "Dinosaur";
        public const string Ostrich = "Ostrich";
        public const string GoldenChicken = "Golden Chicken";
        public const string BlueChicken = "Blue Chicken";
        public const string WhiteCow = "White Cow";
        public const string BrownCow = "Brown Cow";
        public const string Pig = "Pig";
        public const string Sheep = "Sheep";
        public const string Rabbit = "Rabbit";
        public const string Goat = "Goat";
        public static readonly string[] AnimalKeys =
        {
            WhiteChicken,
            VoidChicken,
            BrownChicken,
            Duck,
            Dinosaur,
            Ostrich,
            GoldenChicken,
            BlueChicken,
            WhiteCow,
            BrownCow,
            Pig,
            Sheep,
            Rabbit,
            Goat,
        };
    }

    public static class VanillaBuildings
    {
        public const string Coop = "Coop";
        public const string BigCoop = "Big Coop";
        public const string DeluxeCoop = "Deluxe Coop";
        public const string Barn = "Barn";
        public const string BigBarn = "Big Barn";
        public const string DeluxeBarn = "Deluxe Barn";
    }

    public static class ItemIds
    {
        public static class CustomItems
        {
            public const string Custom = "Custom";
            public const string FertilizedWhiteEgg = "Meara.Roosters_FertEggWhite";
            public const string FertilizedBrownEgg = "Meara.Roosters_FertEggBrown";
            public const string FertilizedBlueEgg = "Meara.Roosters_FertEggBlue";
            public const string FertilizedVoidEgg = "Meara.Roosters_FertEggVoid";
            public const string FertilizedGoldEgg = "Meara.Roosters_FertEggGold";
            public const string Six480BlueEgg = "6480.blueegg_BlueEgg";
            public const string LyteRoosterEgg = "Blaklyte.CP.Rooster_rooster_egg";
            public const string MearaBlueEgg = "Meara.RoostersBlueEgg";
            public const string MearaLargeBlueEgg = "Meara.RoostersLargeBlueEgg";
            public const string MearaLargeVoidEgg = "Meara.RoostersLargevoidEgg";
            public const string MearaLargeGoldEgg = "Meara.RoostersLargegoldEgg";
        }

        public static class AnimalProducts
        {
            public const string LargeWhiteEgg = "174";
            public const string WhiteEgg = "176";
            public const string BrownEgg = "180";
            public const string LargeBrownEgg = "182";
            public const string DinoEgg = "107";
            public const string VoidEgg = "305";
            public const string DuckEgg = "442";
            public const string GoldenEgg = "928";
            public const string OstrichEgg = "289";
            public const string RedSlimeEgg = "437";
            public const string PurpleSlimeEgg = "439";
            public const string TigerSlimeEgg = "857";
            public const string GreenSlimeEgg = "680";
            public const string BlueSlimeEgg = "413";
            public const string Milk = "184";
            public const string LargeMilk = "186";
            public const string GoatMilk = "436";
            public const string LargeGoatMilk = "438";
            public const string IridiumMilk = "803";
            public const string Wool = "440";
            public const string DuckFeather = "444";
            public const string RabbitFoot = "446";
            public const string GoldenAnimalCracker = "Golden Animal Cracker";
            public static readonly string[] VanillaEggs =
            {
                AnimalProducts.DinoEgg,
                AnimalProducts.LargeWhiteEgg,
                AnimalProducts.WhiteEgg,
                AnimalProducts.BrownEgg,
                AnimalProducts.LargeBrownEgg,
                AnimalProducts.VoidEgg,
                AnimalProducts.OstrichEgg,
                AnimalProducts.BlueSlimeEgg,
                AnimalProducts.RedSlimeEgg,
                AnimalProducts.PurpleSlimeEgg,
                AnimalProducts.DuckEgg,
                AnimalProducts.GreenSlimeEgg,
                AnimalProducts.TigerSlimeEgg,
                AnimalProducts.GoldenEgg,
            };
            public static readonly string[] VanillaMilk =
            {
                AnimalProducts.Milk,
                AnimalProducts.LargeMilk,
                AnimalProducts.GoatMilk,
                AnimalProducts.LargeGoatMilk,
                AnimalProducts.IridiumMilk,
            };
        }

        public static class Baits
        {
            public const string Bait = "685";
            public const string WildBait = "774";
            public const string MagicBait = "908";
            public const string DeluxeBait = "DeluxeBait";
        }

        public static class MonsterLoot
        {
            public const string BugMeat = "684";
            public const string MonsterMusk = "879";
            public const string BatWing = "767";
            public const string SolarEssence = "768";
            public const string VoidEssence = "769";
        }

        public static class Artifacts
        {
            public const string GoldenMask = "124";
            public const string GoldenRelic = "125";
            public const string TreasureChest = "166";
            public const string OrnateNecklace = "191";
            public const string ArtifactTrove = "275";
            public const string RingofYoba = "524";
            public const string RainTotem = "681";
            public const string ElvishJewelry = "104";
        }

        public static class ArtisanGoods
        {
            public const string Cloth = "428";
            public const string Mayo = "306";
            public const string DuckMayo = "307";
            public const string VoidMayo = "307";
            public const string Cheese = "424";
            public const string GoatCheese = "426";
        }

        public static class Minerals
        {
            public const string GoldBar = "336";
            public const string GoldOre = "384";
            public const string Geode = "535";
            public const string FrozenGeode = "536";
            public const string MagmaGeode = "537";
            public const string OmniGeode = "749";
            public const string Quartz = "80";
            public const string FireQuartz = "82";
            public const string FrozenTear = "84";
            public const string EarthCrystal = "86";
            public const string Alamite = "538";
            public const string Bixite = "539";
            public const string Baryte = "540";
            public const string Aerinite = "541";
            public const string Calcite = "542";
            public const string Dolomite = "543";
            public const string Esperite = "544";
            public const string Fluorapatite = "545";
            public const string Geminite = "546";
            public const string Helvite = "547";
            public const string Jamborite = "548";
            public const string Jagoite = "549";
            public const string Kyanite = "550";
            public const string Lunarite = "551";
            public const string Malachite = "552";
            public const string Neptunite = "553";
            public const string LemonStone = "554";
            public const string Nekoite = "555";
            public const string Orpiment = "556";
            public const string PetrifiedSlime = "557";
            public const string ThunderEgg = "558";
            public const string Pyrite = "559";
            public const string OceanStone = "560";
            public const string GhostCrystal = "561";
            public const string TigersEye = "562";
            public const string Jasper = "563";
            public const string Opal = "564";
            public const string FireOpal = "565";
            public const string Celestine = "566";
            public const string Marble = "567";
            public const string Sandstone = "568";
            public const string Granite = "569";
            public const string Basalt = "570";
            public const string Limestone = "571";
            public const string Soapstone = "572";
            public const string Hematite = "573";
            public const string Mudstone = "574";
            public const string Obsidian = "575";
            public const string Slate = "576";
            public const string FairyStone = "577";
            public const string StarShards = "578";
            public static readonly string[] All = new string[]
            {
                Minerals.Aerinite,
                Minerals.Alamite,
                Minerals.Baryte,
                Minerals.Basalt,
                Minerals.Bixite,
                Minerals.Calcite,
                Minerals.Celestine,
                Minerals.Dolomite,
                Minerals.EarthCrystal,
                Minerals.Esperite,
                Minerals.FairyStone,
                Minerals.FireOpal,
                Minerals.FireQuartz,
                Minerals.FrozenTear,
                Minerals.Geminite,
                Minerals.GhostCrystal,
                Minerals.Granite,
                Minerals.Helvite,
                Minerals.Hematite,
                Minerals.Jagoite,
                Minerals.Jamborite,
                Minerals.Kyanite,
                Minerals.LemonStone,
                Minerals.Limestone,
                Minerals.Lunarite,
                Minerals.Malachite,
                Minerals.Marble,
                Minerals.Mudstone,
                Minerals.Nekoite,
                Minerals.Neptunite,
                Minerals.Obsidian,
                Minerals.OceanStone,
                Minerals.Opal,
                Minerals.Orpiment,
                Minerals.PetrifiedSlime,
                Minerals.Pyrite,
                Minerals.Sandstone,
                Minerals.Slate,
                Minerals.Soapstone,
                Minerals.StarShards,
                Minerals.ThunderEgg,
                Minerals.TigersEye,
            };
        }

        public static class Gemstones
        {
            public const string Emerald = "60";
            public const string Aquamarine = "62";
            public const string Ruby = "64";
            public const string Amethyst = "66";
            public const string Topaz = "68";
            public const string Jade = "70";
            public const string Diamond = "72";
            public const string PrismaticShard = "74";
            public static readonly string[] All = new string[]
            {
                Gemstones.Amethyst,
                Gemstones.Aquamarine,
                Gemstones.Emerald,
                Gemstones.Jade,
                Gemstones.Ruby,
                Gemstones.Topaz,
                Gemstones.PrismaticShard,
            };
        }

        public static class Fruits
        {
            public const string Blueberry = "258";
            public const string CrystalFruit = "414";
            public const string AncientFruit = "454";
            public const string GoldenPumpkin = "373";
            public const string GoldenCoconut = "791";
        }

        public static class Misc
        {
            public const string GoldenBobber = "GoldenBobber";
            public const string GoldenMysteryBox = "GoldenMysteryBox";
            public const string MysteryBox = "MysteryBox";
            public const string SpiritTorch = "94";
            public const string VoidGhostPendant = "808";
        }

        public static class Crops
        {
            public const string BlueJazz = "597";
            public const string MixedSeeds = "770";
            public const string MixedFlowerSeeds = "MixedFlowerSeeds";
        }
    }

    public static class GameContextTagColors
    {
        public static readonly string[] ContexTagsColors =
        {
            "aquamarine",
            "black",
            "blue",
            "brown",
            "copper",
            "cyan",
            "gray",
            "green",
            "gold",
            "iridium",
            "iron",
            "jade",
            "lime",
            "orange",
            "pale_violet_red",
            "pink",
            "poppyseed",
            "prismatic",
            "purple",
            "red",
            "salmon",
            "sand",
            "white",
            "yellow",
            "yellow_green",
            "light_cyan",
            "dark_blue",
            "dark_brown",
            "dark_cyan",
            "dark_gray",
            "dark_green",
            "dark_pink",
            "dark_purple",
            "dark_red",
            "dark_yellow",
        };
    }

    public static class AssetNames
    {
        public const string BigCraftables = "Data/BigCraftables";
        public const string BigCraftablesInformation = "Data/BigCraftablesInformation";
        public const string CraftingRecipes = "Data/CraftingRecipes";
        public const string FarmAnimals = "Data/FarmAnimals";
        public const string Locations = "Data/Locations";
        public const string Machines = "Data/Machines";
        public const string Mail = "Data/Mail";
        public const string Monsters = "Data/Monsters";
        public const string Mines = "Data/Mines";
        public const string Objects = "Data/Objects";
        public const string ObjectInformation = "Data/ObjectInformation";
        public const string Quests = "Data/Quests";
        public const string SecretNotes = "Data/SecretNotes";
        public const string SpecialOrders = "Data/SpecialOrders";
        public const string SpringObjects = "maps/springobjects";
    }
}
