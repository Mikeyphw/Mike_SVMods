namespace WildEggsHatching.EnumsAndStrings;

public static class Enums
{
    public class TextOptionValues
    {
        public enum UpdateRate
        {
            DayStart,
            LocationChange,
        }

        public enum BirthText
        {
            Default,
            Mysterious,
        }

        public enum TombChance
        {
            Low,
            Medium,
            High,
            Always,
        }

        public enum TombDifficulty
        {
            Easy,
            Moderate,
            Hard,
        }
    }

    /*public enum AnimalName
    {
        Chicken,
        Duck,
        Dinosaur,
        AmherstsPheasants,
        AsilChicken,
        BirdbGoose,
        BlackSpanishChicken,
        Canary,
        CayugaDuck,
        ChaboChicken,
        Crow,
        DesertChicken,
        DesiMurgaChicken,
        Dodo,
        DomesticGuineaFowl,
        Emu,
        EveriaDragon,
        FancyChicken,
        FancyDuck,
        GallusVariusChicken,
        Garten,
        Goose,
        Griffin,
        GyftinsBird,
        Hippogriff,
        HohenHeizensTortoise,
        JekksDino,
        KadaknathChicken,
        Kiwi,
        LivipupDino,
        Loon,
        Lovebird,
        LungDragon,
        MandarinDuck,
        MizuTurkey,
        Ostrich,
        Owl,
        OnagadoriChicken,
        OrpingtonChicken,
        Parrot,
        Peacock,
        Peafowl,
        Penguin,
        PepperRex,
        Pigeon,
        Puffin,
        Pufferchick,
        Quail,
        ResourceChicken,
        Rooster,
        RunnerDuck,
        Seagull,
        SmolChicken,
        Swan,
        Tortoise,
        Turkey,
        Velociraptor,
        WildflourCrow,
        Zebrafinch,
        Wyvern,
        Turtle,
        RobotChicken,
        Merc931Duck,
        MythicDragon,
        JojaBeast,
        Munchlax,
        Psyduck,
        Slowbro,
        Slowking,
        Slowpoke,
        Snorlax,
        KinderChicken,
        BabaYaga,
        VoidChicken,
        BatteryRobot
    }*/

    public enum AnimalHome
    {
        Coop,
        Barn,
    }

    public enum LocationType
    {
        Farm,
        Forest,
        Woods,
        Beach,
        Mountain,
        Town,
        Island,
        DeepIsland,
        IslandBeach,
        Cliff,
        Magical,
        Desert,
        BugLand,
        Cave,
        Swamp,
        Clearing,
    }

    public enum TombAreaType
    {
        MummyArea,
        NormalArea,
        SmallArea,
        DuggyArea,
        MummyDefendArea,
    }

    public enum TombBasedLevel
    {
        Level77377,
        Level39,
    }

    public enum TombMonsterType
    {
        Mummy,
        HauntedSkull,
        Ghost,
        AssassinBug,
        Bat,
        RoyalSerpent,
        BruteMummy,
        Skeleton,
        SkeletonMage,
        Duggy,
        Spider,
        TheTombPharaoh,
    }

    public enum EggType
    {
        wildEgg,
        largeWildEgg,
        magicWildEgg,
        largeMagicWildEgg,
    }

    public static class Assets
    {
        public enum Object
        {
            Eggs,
            Note,
            Scroll,
            Elixir,
            Cloth,
            ErrorItem,
        }

        public enum MineMonster
        {
            BruteMummy,
            TombSkeleton,
            SandyDuggy,
            Spinner,
            ThePharaoh,
            DustSpinner,
        }

        public enum MineTexture
        {
            Z_TombTilesheet,
        }

        public enum MineLevel
        {
            Level77377,
            Level39,
            Level14,
            Level19,
            Level28,
        }

        public enum BigCraftable
        {
            WildEggIncubator,
        }

        public enum Misc
        {
            Interrogation,
            Plants,
            DinosaurShopIcon,
            VoidChickenShopIcon,
        }
    }

    public enum AssetFolder
    {
        Objects,
        AnimalIcons,
        Mines,
        BigCraftables,
        Misc,
    }

    public enum SavedAnimalDataKey
    {
        DisplayName,
        Description,
        Price,
        DaysToMature,
        UnlockCondition,
    }
}

public static class Strings
{
    public const string Vanilla = "Vanilla";

    public const string AnimalDataKey = "wildEggs-animalData";

    public static class IDs
    {
        public const string ModIDPrefix = "wildEggs_";

        public static class Quests
        {
            public const string SpecialOrderWildEggsI = "wildEggs_specialOrderI";
            public const string SpecialOrderWildEggsII = "wildEggs_specialOrderII";
            public const string WizardStudy = "wildEggs_WizardStudy";
            public const string TheDesertScroll = "wildEggs_theDesertScroll";
        }

        public static class Mail
        {
            public const string SpecialOrderDoneI = "wildEggs_specialOrderDoneI";
            public const string SpecialOrderDoneII = "wildEggs_specialOrderDoneII";
            public const string MarnieThanks = "wildEggs_marnieThanks";
            public const string WizardThanks = "wildEggs_wizardThanks";
            public const string WizardDone = "wildEggs_wizardStudyDone";
            public const string WizardFindings = "wildEggs_wizardFindings";
            public const string MarnieGotIncubator = "wildEggs_marnieIncubator";
        }

        public static class Objects
        {
            public const string OsirisScroll = "Mikew.wildeggshatching_OsirisScroll";

            // Farm location types
            public const string FarmWEgg = "yourdorkbrains.cp_wildegg";
            public const string FarmLWEgg = "Mikew.wildeggshatching_farmLWEgg";
            public const string FarmMWEgg = "Mikew.wildeggshatching_farmMWEgg";
            public const string FarmLMWEgg = "Mikew.wildeggshatching_farmLMWegg";

            // Forest location types
            public const string ForestWEgg = "Mikew.wildeggshatching_forestWEgg";
            public const string ForestLWEgg = "Mikew.wildeggshatching_forestLWEgg";
            public const string ForestMWEgg = "Mikew.wildeggshatching_forestMWEgg";
            public const string ForestLMWEgg = "Mikew.wildeggshatching_forestLMWegg";

            // Woods location types
            public const string WoodsWEgg = "Mikew.wildeggshatching_woodsWEgg";
            public const string WoodsLWEgg = "Mikew.wildeggshatching_woodsLWEgg";
            public const string WoodsMWEgg = "Mikew.wildeggshatching_woodsMWEgg";
            public const string WoodsLMWEgg = "Mikew.wildeggshatching_woodsLMWegg";

            // Beach location types
            public const string BeachWEgg = "Mikew.wildeggshatching_beachWEgg";
            public const string BeachLWEgg = "Mikew.wildeggshatching_beachLWEgg";
            public const string BeachMWEgg = "Mikew.wildeggshatching_beachMWEgg";
            public const string BeachLMWEgg = "Mikew.wildeggshatching_beachLMWegg";

            // Mountain location types
            public const string MountainWEgg = "Mikew.wildeggshatching_mountainWEgg";
            public const string MountainLWEgg = "Mikew.wildeggshatching_mountainLWEgg";
            public const string MountainMWEgg = "Mikew.wildeggshatching_mountainMWEgg";
            public const string MountainLMWEgg = "Mikew.wildeggshatching_mountainLMWegg";

            // Town location types
            public const string TownWEgg = "Mikew.wildeggshatching_townWEgg";
            public const string TownLWEgg = "Mikew.wildeggshatching_townLWEgg";
            public const string TownMWEgg = "Mikew.wildeggshatching_townMWEgg";
            public const string TownLMWEgg = "Mikew.wildeggshatching_townLMWegg";

            // Island location types
            public const string IslandWEgg = "Mikew.wildeggshatching_islandWEgg";
            public const string IslandLWEgg = "Mikew.wildeggshatching_islandLWEgg";
            public const string IslandMWEgg = "Mikew.wildeggshatching_islandMWEgg";
            public const string IslandLMWEgg = "Mikew.wildeggshatching_islandLMWegg";

            // DeepIsland location types
            public const string DeepIslandWEgg = "Mikew.wildeggshatching_deepIslandWEgg";
            public const string DeepIslandLWEgg = "Mikew.wildeggshatching_deepIslandLWEgg";
            public const string DeepIslandMWEgg = "Mikew.wildeggshatching_deepIslandMWEgg";
            public const string DeepIslandLMWEgg = "Mikew.wildeggshatching_deepIslandLMWegg";

            // IslandBeach location types
            public const string IslandBeachWEgg = "Mikew.wildeggshatching_islandBeachWEgg";
            public const string IslandBeachLWEgg = "Mikew.wildeggshatching_islandBeachLWEgg";
            public const string IslandBeachMWEgg = "Mikew.wildeggshatching_islandBeachMWEgg";
            public const string IslandBeachLMWEgg = "Mikew.wildeggshatching_islandBeachLMWegg";

            // Cliff location types
            public const string CliffWEgg = "Mikew.wildeggshatching_cliffWEgg";
            public const string CliffLWEgg = "Mikew.wildeggshatching_cliffLWEgg";
            public const string CliffMWEgg = "Mikew.wildeggshatching_cliffMWEgg";
            public const string CliffLMWEgg = "Mikew.wildeggshatching_cliffLMWegg";

            // Magical location types
            public const string MagicalWEgg = "Mikew.wildeggshatching_magicalWEgg";
            public const string MagicalLWEgg = "Mikew.wildeggshatching_magicalLWEgg";
            public const string MagicalMWEgg = "Mikew.wildeggshatching_magicalMWEgg";
            public const string MagicalLMWEgg = "Mikew.wildeggshatching_magicalLMWegg";

            // Desert location types
            public const string DesertWEgg = "Mikew.wildeggshatching_desertWEgg";
            public const string DesertLWEgg = "Mikew.wildeggshatching_desertLWEgg";
            public const string DesertMWEgg = "Mikew.wildeggshatching_desertMWEgg";
            public const string DesertLMWEgg = "Mikew.wildeggshatching_desertLMWegg";

            // BugLand location types
            public const string BugLandWEgg = "Mikew.wildeggshatching_bugLandWEgg";
            public const string BugLandLWEgg = "Mikew.wildeggshatching_bugLandLWEgg";
            public const string BugLandMWEgg = "Mikew.wildeggshatching_bugLandMWEgg";
            public const string BugLandLMWEgg = "Mikew.wildeggshatching_bugLandLMWegg";

            // Underground location types
            public const string UndergroundWEgg = "Mikew.wildeggshatching_undergroundWEgg";
            public const string UndergroundLWEgg = "Mikew.wildeggshatching_undergroundLWEgg";
            public const string UndergroundMWEgg = "Mikew.wildeggshatching_undergroundMWEgg";
            public const string UndergroundLMWEgg = "Mikew.wildeggshatching_undergroundLMWegg";

            // Underground location types
            public const string SwampWEgg = "Mikew.wildeggshatching_swampWEgg";
            public const string SwampLWEgg = "Mikew.wildeggshatching_swampLWEgg";
            public const string SwampMWEgg = "Mikew.wildeggshatching_swampMWEgg";
            public const string SwampLMWEgg = "Mikew.wildeggshatching_swampLMWegg";

            // Elixir
            public const string Elixir = "Mikew.wildeggshatching_ElixirOfRevitalizedEssence";
            public const string ElixirCrafting = "Elixir Of Revitalized Essence";
        }

        public static class BigCraftables
        {
            public const string WildEggIncubator = "Mikew.wildeggshatching_WildEggIncubator";
            public const string WEIncubatorCrafting = "Wild Egg Incubator";
            public const string Incubator = "101";
            public const string OstrichIncubator = "254";
        }
    }
}
