using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Monsters.Creatures;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Strings;

namespace WildEggsHatching.DesertTomb.Monsters.Base
{
    internal static class TombMonsterUtilities
    {
        internal static readonly string[] ItemsToNotDouble = new string[]
        {
            "64",
            "65",
            "72",
            "74",
            "96",
            "97",
            "98",
            "99",
            "MummyMask",
            "261",
            "124",
            "125",
            "275",
            "791",
            "166",
            IDs.Objects.OsirisScroll,
        };
        internal static Dictionary<TombMonsterType, string> MonsterDropItems { get; set; }

        static TombMonsterUtilities()
        {
            MonsterDropItems = new()
            {
                { TombMonsterType.AssassinBug, "684 .76 157 .02 114 .005 96 .005 99 .001" },
                { TombMonsterType.Bat, "767 .9 767 .4 108 .001 287 .02 96 .005 99 .001" },
                {
                    TombMonsterType.BruteMummy,
                    "769 .99 337 .002 336 .01 335 .02 334 .04 203 .04 108 .003 98 .005 99 .001"
                },
                {
                    TombMonsterType.Duggy,
                    "286 .25 535 .25 280 .03 105 .02 86 .1 72 .01 96 .005 99 .001"
                },
                {
                    TombMonsterType.HauntedSkull,
                    "531 0.013 (W)2 0.013 86 0.013 279 0.0013 288 0.013 725 0.013 581 0.013 534 0.013 582 0.013 768 0.013 769 0.013"
                },
                { TombMonsterType.Ghost, "768 .95 768 .1 156 .08 338 .08 97 .005 99 .001 749 .99" },
                { TombMonsterType.Mummy, "768 .99 428 .2 428 .05 768 .15 243 .04 99 .001 74 .001" },
                {
                    TombMonsterType.RoyalSerpent,
                    "769 .99 769 .15 287 .15 226 .06 446 .008 74 .001"
                },
                {
                    TombMonsterType.Spider,
                    "769 .75 769 .1 337 .002 336 .01 335 .02 334 .04 203 .04 108 .003 98 .005 99 .001 74 .0005"
                },
                { TombMonsterType.Skeleton, "769 .99 881 .5 881 .4 881 .2 579 .005" },
            };
        }

        public static TombMonsterType GetMonsterType(Monster monster)
        {
            var monsterType = monster switch
            {
                TombAssassinBug _ => TombMonsterType.AssassinBug,
                TombBat _ => TombMonsterType.Bat,
                BruteMummy _ => TombMonsterType.BruteMummy,
                TombDuggy _ => TombMonsterType.Duggy,
                TombHauntedSkull _ => TombMonsterType.HauntedSkull,
                TombGhost _ => TombMonsterType.Ghost,
                TombMummy _ => TombMonsterType.Mummy,
                TombSerpent _ => TombMonsterType.RoyalSerpent,
                TombSkeleton _ => TombMonsterType.Skeleton,
                TombSkeletonMage _ => TombMonsterType.SkeletonMage,

                TombSpider _ => TombMonsterType.Spider,
                ThePharaoh _ => TombMonsterType.TheTombPharaoh,
                _ => TombMonsterType.Mummy,
            };
            return monsterType;
        }
    }
}
