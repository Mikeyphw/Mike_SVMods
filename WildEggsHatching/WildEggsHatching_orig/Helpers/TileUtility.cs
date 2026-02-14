using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley.Locations;
using xTile;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.EnumsAndStrings.Strings;

namespace WildEggsHatching.DesertTomb
{
    internal static class TileUtility
    {
        private static readonly HashSet<int> ValidAnimalHouseTileIndexes =
            GetValidAnimalHouseTileIndexes();
        internal static readonly HashSet<int> ValidMineTileIndexes = GetValidMineTileIndexes();

        internal static Vector2? GetRandomTileOnArea(
            MineShaft mineShaft,
            Rectangle area,
            HashSet<Rectangle>? prohibitedAreas = null,
            Random? r = null
        )
        {
            r ??= Game1.random;

            var width = area.Size.X;
            var height = area.Size.Y;
            // Create a list of all tiles in the area
            var tiles = new List<Vector2>();
            for (int x = area.Location.X; x < area.Location.X + width; x++)
            {
                for (int y = area.Location.Y; y < area.Location.Y + height; y++)
                {
                    var tile = new Vector2(x, y);
                    bool isProhibited =
                        prohibitedAreas?.Any(prohibitedArea =>
                            prohibitedArea.Contains((int)tile.X, (int)tile.Y)
                        ) ?? false;
                    if (!isProhibited)
                    {
                        tiles.Add(tile);
                    }
                }
            }

            tiles = tiles.OrderBy(tile => r.Next()).ToList();

            foreach (var tile in tiles)
            {
                if (CheckTileConditions(mineShaft, tile, ValidMineTileIndexes))
                {
                    return tile;
                }
            }
            return null;
        }

        private static bool CheckTileConditions(
            MineShaft location,
            Vector2 tile,
            HashSet<int>? validIndexes
        )
        {
            var point = new Point() { X = (int)tile.X, Y = (int)tile.Y };

            return (
                    validIndexes == null
                    || validIndexes.Contains(location.getTileIndexAt(point, "Back"))
                )
                && location.isTileLocationOpen(tile)
                && location.getTileIndexAt(point, "Front") == -1
                && location.isTileOnMap(tile)
                && !location.IsTileBlockedBy(tile);
        }

        internal static bool IsTileClearForMonsters(Vector2 v, MineShaft mineShaft)
        {
            return !IsTileBlocked(v, mineShaft);
        }

        private static bool IsTileBlocked(Vector2 v, MineShaft mineShaft)
        {
            return mineShaft.tileBeneathLadder.Equals(v)
                || mineShaft.tileBeneathElevator.Equals(v)
                || !mineShaft.CanItemBePlacedHere(
                    v,
                    itemIsPassable: false,
                    CollisionMask.All,
                    CollisionMask.None
                )
                || mineShaft.IsTileOccupiedBy(v, CollisionMask.Characters)
                || mineShaft.IsTileOccupiedBy(
                    v,
                    CollisionMask.Flooring | CollisionMask.TerrainFeatures
                )
                || !mineShaft.isTileOnClearAndSolidGround(v)
                || mineShaft.objects.ContainsKey(v);
        }

        internal static bool IsValidTile(
            MineShaft mineShaft,
            Vector2 tile,
            HashSet<int> validIndexes
        )
        {
            return CheckTileConditions(mineShaft, tile, validIndexes);
        }

        internal static bool IsValidDuggyBackTile(int backTileIndex)
        {
            var validDuggyIndexes = new[] { 165, 181, 183 };
            return validDuggyIndexes.Contains(backTileIndex);
        }

        internal static Vector2? GetRandomTile(
            GameLocation location,
            HashSet<int>? validIndexes,
            HashSet<Rectangle>? excludedAreas = null
        )
        {
            var randomTile = location.getRandomTile();
            if (CheckTileConditions(location, randomTile, validIndexes, excludedAreas))
            {
                return randomTile;
            }
            else
            {
                return null;
            }
        }

        internal static Vector2? GetRandomTile(
            GameLocation location,
            HashSet<string>? includedTerrainTypes = null,
            HashSet<string>? excludedTerrainTypes = null,
            HashSet<Rectangle>? includedAreas = null,
            HashSet<Rectangle>? excludedAreas = null,
            bool debug = false
        )
        {
            var randomTile = location.getRandomTile();
            if (
                CheckTileConditions(
                    location,
                    randomTile,
                    includedTerrainTypes,
                    excludedTerrainTypes,
                    includedAreas,
                    excludedAreas,
                    debug
                )
            )
            {
                return randomTile;
            }
            else
            {
                return null;
            }
        }

        internal static bool CheckTileConditions(
            GameLocation location,
            Vector2 tile,
            HashSet<int>? validIndexes,
            HashSet<Rectangle>? excludedAreas = null
        )
        {
            var point = new Point() { X = (int)tile.X, Y = (int)tile.Y };

            return (
                    validIndexes == null
                    || validIndexes.Contains(location.getTileIndexAt(point, "Back"))
                )
                && location.isTileLocationOpen(tile)
                && location.getTileIndexAt(point, "Front") == -1
                && location.isTileOnMap(tile)
                && !location.IsTileBlockedBy(tile)
                && !(excludedAreas != null && excludedAreas.Any(area => area.Contains(point)));
        }

        internal static bool CheckTileConditions(
            GameLocation location,
            Vector2 tile,
            HashSet<string>? includedTerrainTypes = null,
            HashSet<string>? excludedTerrainTypes = null,
            HashSet<Rectangle>? includedAreas = null,
            HashSet<Rectangle>? excludedAreas = null,
            bool debug = false
        )
        {
            var point = new Point((int)tile.X, (int)tile.Y);
            ModEntry.LoggerHelper.Log(
                $"Checking tile conditions at {tile} (Point: {point})",
                LogLevel.Debug,
                debug
            );

            if (!IsValidTile(location, point, debug))
                return false;

            if (!CheckIncludedTerrainTypes(location, point, includedTerrainTypes, debug))
                return false;

            if (CheckExcludedTerrainTypes(location, point, excludedTerrainTypes, debug))
                return false;

            return AreTileConditionsMet(location, tile, point, includedAreas, excludedAreas, debug);
        }

        private static bool IsValidTile(GameLocation location, Point point, bool debug = false)
        {
            if (location == null)
            {
                ModEntry.LoggerHelper.Log(
                    "Location is null. Returning false.",
                    LogLevel.Error,
                    debug
                );
                return false;
            }

            if (point.X < 0 || point.Y < 0)
            {
                ModEntry.LoggerHelper.Log(
                    $"Invalid tile coordinates: {point}. Returning false.",
                    LogLevel.Error,
                    debug
                );
                return false;
            }

            ModEntry.LoggerHelper.Log(
                $"Tile coordinates are valid: {point}.",
                LogLevel.Debug,
                debug
            );
            return true;
        }

        private static bool CheckIncludedTerrainTypes(
            GameLocation location,
            Point point,
            HashSet<string>? includedTerrainTypes,
            bool debug = false
        )
        {
            if (includedTerrainTypes == null || includedTerrainTypes.Count == 0)
            {
                ModEntry.LoggerHelper.Log(
                    "No included terrain types specified or empty. Skipping check.",
                    LogLevel.Debug,
                    debug
                );
                return true; // No restrictions on included terrain types
            }

            foreach (var type in includedTerrainTypes)
            {
                if (
                    location.doesEitherTileOrTileIndexPropertyEqual(
                        point.X,
                        point.Y,
                        "Type",
                        "Back",
                        type
                    )
                )
                {
                    ModEntry.LoggerHelper.Log(
                        $"Included terrain type '{type}' found at {point}.",
                        LogLevel.Debug,
                        debug
                    );
                    return true; // At least one included terrain type matches
                }
            }

            ModEntry.LoggerHelper.Log("No included terrain types matched.", LogLevel.Debug, debug);
            return false; // No matches found
        }

        private static bool CheckExcludedTerrainTypes(
            GameLocation location,
            Point point,
            HashSet<string>? excludedTerrainTypes,
            bool debug = false
        )
        {
            if (excludedTerrainTypes == null || excludedTerrainTypes.Count == 0)
            {
                ModEntry.LoggerHelper.Log(
                    "No excluded terrain types specified or empty. Skipping check.",
                    LogLevel.Debug,
                    debug
                );
                return false; // No restrictions on excluded terrain types
            }

            foreach (var type in excludedTerrainTypes)
            {
                if (
                    location.doesEitherTileOrTileIndexPropertyEqual(
                        point.X,
                        point.Y,
                        "Type",
                        "Back",
                        type
                    )
                )
                {
                    ModEntry.LoggerHelper.Log(
                        $"Excluded terrain type '{type}' found at {point}. Returning false.",
                        LogLevel.Warn,
                        debug
                    );
                    return true; // Early return if an excluded type is found
                }
            }

            ModEntry.LoggerHelper.Log("No excluded terrain types matched.", LogLevel.Debug, debug);
            return false; // No matches found
        }

        private static bool AreTileConditionsMet(
            GameLocation location,
            Vector2 tile,
            Point point,
            HashSet<Rectangle>? includedAreas,
            HashSet<Rectangle>? excludedAreas,
            bool debug = false
        )
        {
            bool isTileOpen = IsTileLocationOpen(location, tile);
            bool isTileFrontEmpty = location.getTileIndexAt(point, "Front") == -1;
            bool isTileOnMap = location.isTileOnMap(tile);
            bool isTileBlocked = location.IsTileBlockedBy(tile);
            bool isInIncludedArea =
                includedAreas == null
                || includedAreas.Count == 0
                || includedAreas.Any(area => area.Contains(point));
            bool isInExcludedArea =
                excludedAreas != null
                && excludedAreas.Count > 0
                && excludedAreas.Any(area => area.Contains(point));

            ModEntry.LoggerHelper.Log(
                $"Tile Conditions: Open: {isTileOpen}, Front Empty: {isTileFrontEmpty}, On Map: {isTileOnMap}, Blocked: {isTileBlocked}, In Included Area: {isInIncludedArea}, In Excluded Area: {isInExcludedArea}",
                LogLevel.Debug,
                debug
            );

            return isTileOpen
                && isTileFrontEmpty
                && isTileOnMap
                && !isTileBlocked
                && isInIncludedArea
                && !isInExcludedArea;
        }

        public static bool IsTileLocationOpen(GameLocation gameLocation, Vector2 tileLocation)
        {
            int x = (int)tileLocation.X;
            int y = (int)tileLocation.Y;

            var map = gameLocation.Map;

            // Check if the coordinates are within bounds
            if (x < 0 || x >= map.DisplayWidth || y < 0 || y >= map.DisplayHeight)
            {
                return false;
            }

            // Check for "Buildings" and any additional "Buildings(number)" layers
            for (int i = 0; ; i++)
            {
                string layerName = i == 0 ? "Buildings" : $"Buildings{i + 1}";
                var layer = map.GetLayer(layerName);
                if (layer == null)
                    break; // No more layers to check
                if (layer.GetTileIndexAt(x, y) != -1)
                    return false; // Tile is occupied
            }

            // Check if it's a water tile
            if (gameLocation.isWaterTile(x, y))
            {
                return false;
            }

            // Check for "Front" and any additional "Front(number)" layers
            for (int i = 0; ; i++)
            {
                string layerName = i == 0 ? "Front" : $"Front{i + 1}";
                var layer = map.GetLayer(layerName);
                if (layer == null)
                    break; // No more layers to check
                if (layer.GetTileIndexAt(x, y) != -1)
                    return false; // Tile is occupied
            }

            // Check for "AlwaysFront" and any additional "AlwaysFront(number)" layers
            for (int i = 0; ; i++)
            {
                string layerName = i == 0 ? "AlwaysFront" : $"AlwaysFront{i + 1}";
                var layer = map.GetLayer(layerName);
                if (layer == null)
                    break; // No more layers to check
                if (layer.GetTileIndexAt(x, y) != -1)
                    return false; // Tile is occupied in this layer
            }

            return true; // Tile is open if no layers occupied
        }

        internal static HashSet<Vector2> GetTombAreaValidTiles(
            Rectangle area,
            TombAreaType areaType,
            MineShaft mineShaft
        )
        {
            var validTiles = new HashSet<Vector2>();

            for (int x = area.X; x < area.X + area.Width; x++)
            {
                for (int y = area.Y; y < area.Y + area.Height; y++)
                {
                    var tile = new Vector2(x, y);

                    if (
                        IsValidTile(mineShaft, tile, ValidMineTileIndexes)
                        && IsTileClearForMonsters(tile, mineShaft)
                        && !mineShaft.IsTileOccupiedBy(tile)
                    )
                    {
                        if (areaType == TombAreaType.DuggyArea)
                        {
                            var backTileIndex = mineShaft.getTileIndexAt(
                                (int)tile.X,
                                (int)tile.Y,
                                "Back"
                            );
                            if (IsValidDuggyBackTile(backTileIndex))
                            {
                                validTiles.Add(tile);
                            }
                        }
                        else
                        {
                            validTiles.Add(tile);
                        }
                    }
                }
            }

            return validTiles;
        }

        internal static Vector2 GetRandomTileIncubator(GameLocation location)
        {
            var objectsAtLocation = location.objects.Values;
            var wildIncubator = objectsAtLocation.FirstOrDefault(o =>
                o.ItemId == IDs.BigCraftables.WildEggIncubator
            );
            var referenceTile =
                wildIncubator == null
                    ? location.getRandomTile(Game1.random)
                    : wildIncubator.TileLocation;
            var nearestTile = Utility.getRandomAdjacentOpenTile(referenceTile, location);
            //FindNearestEmptyTile(location, wildIncubatorTile, validIndexes);
            return nearestTile;
        }

        private static HashSet<int> GetValidAnimalHouseTileIndexes()
        {
            return new()
            {
                12,
                13,
                14,
                15,
                17,
                18,
                19,
                21,
                22,
                23,
                36,
                37,
                52,
                53,
                54,
                55,
                56,
                65,
                66,
                25,
                46,
            };
        }

        private static HashSet<int> GetValidMineTileIndexes()
        {
            return new()
            {
                137,
                138,
                139,
                140,
                149,
                150,
                151,
                152,
                153,
                154,
                155,
                165,
                166,
                167,
                168,
                169,
                170,
                171,
                172,
                181,
                182,
                183,
                184,
                185,
                186,
                187,
                188,
                198,
                199,
                200,
                201,
                202,
                203,
                204,
                209,
                210,
                211,
                212,
                225,
                226,
                227,
                228,
                217,
                218,
                219,
                233,
                234,
                235,
                240,
                241,
                242,
                256,
                257,
                258,
                272,
                273,
                274,
                259,
                260,
                275,
                276,
            };
        }
    }
}
