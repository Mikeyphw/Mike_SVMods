using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Calculation;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.Generation;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Enums.Assets;
using static WildEggsHatching.EnumsAndStrings.Enums.TextOptionValues;
using static WildEggsHatching.EnumsAndStrings.Strings;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.DesertTomb.Core
{
    /// <summary>The mod entry point.</summary>
    ///
    internal class DesertTombWrapper
    {
        private MineShaft? _mineshaft;
        private LevelGenerator? LevelGenerator;
        private SpawnConfiguration? _spawnConfiguration;
        private SpawnRateCalculator? _spawnRateCalculator;
        internal bool IsFirstMineEdit;

        //internal readonly List<(int Number, string Asset)> ChosenMineLevels;
        //internal Dictionary<MineLevel, int[]>? TombLevelsRelation { get; set; }
        private int MineCurrentLevel { get; set; }

        internal DesertTombWrapper()
        {
            IsFirstMineEdit = true;
        }

        internal void SetUpTombHelpers(
            MineShaft mineShaft,
            MineLevel mineLevel,
            SpawnConfiguration spawnConfiguration,
            TombDifficulty tombDifficulty
        )
        {
            _mineshaft = mineShaft;
            _spawnConfiguration = spawnConfiguration;
            LevelGenerator = new(_mineshaft, _spawnConfiguration);
            _spawnRateCalculator = new(_spawnConfiguration, mineShaft, mineLevel, tombDifficulty);
        }

        internal void Fire()
        {
            LoggerHelper.Log("Firing!");
            LevelGenerator?.Generate();
            _spawnRateCalculator?.SpawnMonsters();
            LogMonsters(_mineshaft!);
        }

        internal void OnDayStart()
        {
            IsFirstMineEdit = true;
        }

        internal void OnDayEnding()
        {
            _mineshaft = null;
            MineCurrentLevel = 0;
            LevelGenerator = null;
            _spawnRateCalculator = null;
            _spawnConfiguration = null;
            IsFirstMineEdit = true;
        }

        // internal bool ShouldEditMineLevel(GameLocation? location = null)
        // {
        //     if (!Game1.hasStartedDay)
        //     {
        //         return false;
        //     }
        //     if (IsFirstMineEdit)
        //     {
        //         return ModEntry.Config.KeepMineLevel
        //             || Game1.player.hasQuest(IDs.Quests.TheDesertScroll);
        //     }
        //     if (!GameStateQuery.CheckConditions("LOCATION_IS_SKULL_CAVE Here"))
        //     {
        //         return false;
        //     }
        //     if (location == null)
        //     {
        //         return ModEntry.Config.KeepMineLevel
        //             || Game1.player.hasQuest(IDs.Quests.TheDesertScroll);
        //     }
        //     var mineShaft = GetMineShaft(location);
        //     if (mineShaft == null)
        //     {
        //         return false;
        //     }

        //     return
        //             ModEntry.Config.KeepMineLevel
        //             || Game1.player.hasQuest(IDs.Quests.TheDesertScroll)
        //         ;
        // }
        internal bool ShouldEditMineLevel(GameLocation? location = null)
        {
            // LoggerHelper.TestingLog("ShouldEditMineLevel called");

            // Check if the game has started the day
            bool hasStartedDay = Game1.hasStartedDay;
            //  LoggerHelper.TestingLog($"Game has started day: {hasStartedDay}");
            if (!hasStartedDay)
            {
                //   LoggerHelper.TestingLog(
                //      "Returning false because the game has not started the day."
                //  );
                return false;
            }

            // Check if it's the first mine edit
            bool isFirstMineEdit = IsFirstMineEdit;
            if (IsFirstMineEdit)
            {
                //  LoggerHelper.TestingLog($"IsFirstMineEdit: {isFirstMineEdit}. Returning false.");

                return false;
            }
            // Check game state conditions for Skull Cave
            bool conditionsMet = GameStateQuery.CheckConditions("LOCATION_IS_SKULL_CAVE Here");
            LoggerHelper.TestingLog($"Conditions for LOCATION_IS_SKULL_CAVE met: {conditionsMet}");
            if (!conditionsMet)
            {
                LoggerHelper.TestingLog(
                    "Returning false because conditions for LOCATION_IS_SKULL_CAVE are not met."
                );
                return false;
            }

            // Check if location is null
            bool locationIsNull = location == null;
            LoggerHelper.TestingLog($"Location is null: {locationIsNull}");

            // Get the mine shaft
            var mineShaft = GetMineShaft(location);
            bool mineShaftIsNull = mineShaft == null;
            LoggerHelper.TestingLog($"MineShaft is null: {mineShaftIsNull}");
            if (mineShaftIsNull)
            {
                LoggerHelper.TestingLog("Returning false because mine shaft is null.");
                return false;
            }

            // Final evaluation
            bool keepMineLevelFinal = ModEntry.Config.KeepMineLevel;
            bool hasDesertScrollQuestFinal = Game1.player.hasQuest(IDs.Quests.TheDesertScroll);
            bool questLog = Game1.player.questLog.Any(q =>
                q.id.Value == IDs.Quests.TheDesertScroll
            );
            LoggerHelper.TestingLog(
                $"Final KeepMineLevel: {keepMineLevelFinal}, Final Has Desert Scroll Quest: {hasDesertScrollQuestFinal}, QuestLog: {questLog}"
            );
            bool finalResult = keepMineLevelFinal || hasDesertScrollQuestFinal;
            LoggerHelper.TestingLog($"Final Result: {finalResult}");

            return finalResult;
        }

        internal static MineShaft? GetMineShaft(GameLocation? location)
        {
            return location is not MineShaft ? null : (MineShaft)location;
        }

        public Vector2? GetPharaohTile()
        {
            var pharaoh = _mineshaft?.characters.FirstOrDefault(c => c is ThePharaoh);
            return pharaoh?.Tile;
        }

        internal void PharaohDrawArrowTracker(SpriteBatch b)
        {
            var _tile = GetPharaohTile();
            if (!_tile.HasValue)
            {
                return;
            }
            var tile = _tile.Value;
            // If the tracker is already drawn, don't draw it again
            // Only draw the tracker if the Pharaoh is off-screen
            if (!Utility.isOnScreen(tile * 64f + new Vector2(32f, 32f), 64))
            {
                Rectangle vpbounds = Game1.graphics.GraphicsDevice.Viewport.Bounds;
                Vector2 onScreenPosition = default;
                float rotation = 0f;
                if (tile.X * 64f > (float)(Game1.viewport.MaxCorner.X - 64))
                {
                    onScreenPosition.X = vpbounds.Right - 8;
                    rotation = (float)Math.PI / 2f;
                }
                else if (tile.X * 64f < (float)Game1.viewport.X)
                {
                    onScreenPosition.X = 8f;
                    rotation = -(float)Math.PI / 2f;
                }
                else
                {
                    onScreenPosition.X = tile.X * 64f - (float)Game1.viewport.X;
                }
                if (tile.Y * 64f > (float)(Game1.viewport.MaxCorner.Y - 64))
                {
                    onScreenPosition.Y = vpbounds.Bottom - 8;
                    rotation = (float)Math.PI;
                }
                else if (tile.Y * 64f < (float)Game1.viewport.Y)
                {
                    onScreenPosition.Y = 8f;
                }
                else
                {
                    onScreenPosition.Y = tile.Y * 64f - (float)Game1.viewport.Y;
                }
                if (onScreenPosition.X == 8f && onScreenPosition.Y == 8f)
                {
                    rotation += (float)Math.PI / 4f;
                }
                if (onScreenPosition.X == 8f && onScreenPosition.Y == (float)(vpbounds.Bottom - 8))
                {
                    rotation += (float)Math.PI / 4f;
                }
                if (onScreenPosition.X == (float)(vpbounds.Right - 8) && onScreenPosition.Y == 8f)
                {
                    rotation -= (float)Math.PI / 4f;
                }
                if (
                    onScreenPosition.X == (float)(vpbounds.Right - 8)
                    && onScreenPosition.Y == (float)(vpbounds.Bottom - 8)
                )
                {
                    rotation -= (float)Math.PI / 4f;
                }
                Rectangle srcRect = new(0, 0, 11, 15);
                float renderScale = 6f;
                Vector2 safePos = Utility.makeSafe(
                    renderSize: new Vector2(
                        (float)srcRect.Width * renderScale,
                        (float)srcRect.Height * renderScale
                    ),
                    renderPos: onScreenPosition
                );
                var tracker = HelperInstance.ModContent.Load<Texture2D>("assets/Mines/tracker.png");
                b.Draw(
                    tracker,
                    safePos,
                    srcRect,
                    Color.White,
                    rotation,
                    new Vector2(2f, 2f),
                    renderScale,
                    SpriteEffects.None,
                    1f
                );
            }
        }

        private void LogMonsters(GameLocation location)
        {
            var monsters = location
                .characters.Where(c => c is Monster)
                .Select(m => m.Name)
                .Distinct()
                .ToList();
            LoggerHelper.VerboseLog("Number Of Monsters: ");
            foreach (var monster in monsters)
            {
                var counter = location.characters.Count(c => c.Name == monster);
                LoggerHelper.VerboseLog($"{monster}: {counter}");
            }

            /* foreach (TombArea area in TombHandler.Tombs[MineLevelMap].LevelAreas)
            {
                LoggerHelper.VerboseLog($"Tomb Area: {area.Type}");
                LoggerHelper.VerboseLog($"  Area: {area.Area}");
                LoggerHelper.VerboseLog($"  Min Capacity: {area.MinCapacity}");
                LoggerHelper.VerboseLog($"  Max Capacity: {area.MaxCapacity}");
                LoggerHelper.VerboseLog($"  Chosen Capacity: {area.ChosenCapacity}");
                LoggerHelper.VerboseLog($"  Current Occupancy: {area.CurrentOccupancy}");
                LoggerHelper.VerboseLog($"  Number of Valid Tiles: {area.ValidTiles.Count}");
                LoggerHelper.VerboseLog($"  Allowed Monsters:");
                foreach (TombMonsterType monster in area.AllowedMonsters)
                {
                    LoggerHelper.VerboseLog($"    - {monster}");
                }
                LoggerHelper.VerboseLog($"  Monsters:");
                foreach (Monster monster in area.Monsters)
                {
                    LoggerHelper.VerboseLog($"    - {monster.displayName}");
                }
                LoggerHelper.VerboseLog($"  Monster Counter:");
                foreach (KeyValuePair<TombMonsterType, int> counter in area.MonsterCounter)
                {
                    LoggerHelper.VerboseLog($"    - {counter.Key}: {counter.Value}");
                }
            } */
        }
    }
}
