using Microsoft.Xna.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley;
using Mikew.BirdsOfAFeather.Helpers;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using WildEggsHatching.DesertTomb.Calculation;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.TombMonsters.Creatures;
using xTile.Layers;
using static Mikew.BirdsOfAFeather.Helpers.ModAssetHandler;
using static WildEggsHatching.EnumsAndStrings.Enums;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.DesertTomb.Generation
{
    internal class LevelGenerator
    {
        private readonly MineShaft _mineShaft;
        private readonly SpawnConfiguration _spawnConfiguration;

        internal LevelGenerator(MineShaft mineShaft, SpawnConfiguration spawnConfiguration)
        {
            _mineShaft = mineShaft;
            _spawnConfiguration = spawnConfiguration;
            _mineShaft.mapImageSource.Value = AssetHandler.GetAssetPath(
                Assets.MineTexture.Z_TombTilesheet,
                AssetPathType.GamePath
            );
        }

        internal void Generate()
        {
            _mineShaft.isDinoArea = true;
            _mineShaft.loadedDarkArea = true;
            ChooseLevelType(32);
            PopulateLevel();
        }

        private void PopulateLevel(bool forceFirstTime = true)
        {
            _mineShaft.ghostAdded = false;
            _mineShaft.isDinoArea = true;
            _mineShaft.stonesLeftOnThisLevel = 0;
            double stoneChance = (double)_mineShaft.mineRandom.Next(10, 30) / 100.0;
            double itemChance = 0.0025;
            double gemStoneChance = 0.003;
            int numBarrels =
                _mineShaft.mineRandom.Next(5)
                + (int)(Game1.player.team.AverageDailyLuck(_mineShaft) * 20.0);

            numBarrels +=
                _mineShaft.map.Layers[0].LayerWidth * _mineShaft.map.Layers[0].LayerHeight / 40;
            AdjustLevelChances(ref stoneChance, ref itemChance, ref gemStoneChance);
            int barrelsAdded = 0;
            bool firstTime =
                !MineShaft.permanentMineChanges.ContainsKey(_mineShaft.mineLevel) || forceFirstTime;
            float df_barrelExtra = 0f;
            if (_mineShaft.mineLevel >= 121)
            {
                Layer backLayer_ = _mineShaft.map.RequireLayer("Back");

                for (int i = 0; i < numBarrels; i++)
                {
                    Point p;
                    Point motion;
                    if (_mineShaft.mineRandom.NextDouble() < 0.33 + (double)(df_barrelExtra / 2f))
                    {
                        p = new Point(_mineShaft.mineRandom.Next(backLayer_.LayerWidth), 0);
                        motion = new Point(0, 1);
                    }
                    else if (_mineShaft.mineRandom.NextBool())
                    {
                        p = new Point(0, _mineShaft.mineRandom.Next(backLayer_.LayerHeight));
                        motion = new Point(1, 0);
                    }
                    else
                    {
                        p = new Point(
                            backLayer_.LayerWidth - 1,
                            _mineShaft.mineRandom.Next(backLayer_.LayerHeight)
                        );
                        motion = new Point(-1, 0);
                    }
                    while (_mineShaft.isTileOnMap(p.X, p.Y))
                    {
                        p.X += motion.X;
                        p.Y += motion.Y;
                        if (
                            !_mineShaft.isTileClearForMineObjects(p.X, p.Y)
                            || _mineShaft.mineRandom.NextDouble() > 0.25
                        )
                        {
                            continue;
                        }
                        Vector2 objectPos = new Vector2(p.X, p.Y);
                        if (
                            _mineShaft.mineRandom.NextDouble()
                            < _spawnConfiguration.Global.BarrelPlantChance
                        )
                        {
                            _mineShaft.objects.Add(
                                objectPos,
                                BreakableContainer.GetBarrelForMines(objectPos, _mineShaft)
                            );
                        }
                        else
                        {
                            _mineShaft.terrainFeatures.Add(
                                objectPos,
                                new TombCosmeticPlant(_mineShaft.mineRandom.NextBool() ? 0 : 1)
                            );
                        }
                        break;
                    }
                }
            }
            Layer backLayer = _mineShaft.map.RequireLayer("Back");
            for (int i = 0; i < backLayer.LayerWidth; i++)
            {
                for (int j = 0; j < backLayer.LayerHeight; j++)
                {
                    _mineShaft.checkForMapAlterations(i, j);
                    if (_mineShaft.isTileClearForMineObjects(i, j))
                    {
                        if (_mineShaft.mineRandom.NextDouble() <= itemChance)
                        {
                            Vector2 objectPos = new Vector2(i, j);
                            _mineShaft.Objects.Add(objectPos, GetRandomItemForMineShaftLevel());
                        }
                        else if (
                            _mineShaft.mineRandom.NextDouble() <= 0.005
                            && (_mineShaft.GetAdditionalDifficulty() <= 0)
                        )
                        {
                            if (
                                !_mineShaft.isTileClearForMineObjects(i + 1, j)
                                || !_mineShaft.isTileClearForMineObjects(i, j + 1)
                                || !_mineShaft.isTileClearForMineObjects(i + 1, j + 1)
                            )
                            {
                                continue;
                            }
                            Vector2 objectPos = new Vector2(i, j);
                            int whichClump = _mineShaft.mineRandom.Choose(752, 754);
                            if (_mineShaft.getMineArea() == 40)
                            {
                                if (_mineShaft.GetAdditionalDifficulty() > 0)
                                {
                                    whichClump = 600;
                                    if (_mineShaft.mineRandom.NextDouble() < 0.1)
                                    {
                                        whichClump = 602;
                                    }
                                }
                                else
                                {
                                    whichClump = _mineShaft.mineRandom.Choose(756, 758);
                                }
                            }
                            _mineShaft.resourceClumps.Add(
                                new ResourceClump(whichClump, 2, 2, objectPos)
                            );
                        }
                        else if (_mineShaft.GetAdditionalDifficulty() > 0)
                        {
                            if (
                                _mineShaft.mineRandom.NextDouble() < 0.01
                                && _mineShaft.getTileIndexAt(i, j - 1, "Buildings") != -1
                            )
                            {
                                _mineShaft.terrainFeatures.Add(new Vector2(i, j), new Tree("8", 5));
                            }

                            // else if (
                            //getMineArea() == 40
                            //  && mineLevel % 40 < 30
                            //    && mineRandom.NextDouble() < 0.1
                            if (
                                _mineShaft.mineRandom.NextDouble() < 0.1
                                && (
                                    _mineShaft.getTileIndexAt(i, j - 1, "Buildings") != -1
                                    || _mineShaft.getTileIndexAt(i - 1, j, "Buildings") != -1
                                    || _mineShaft.getTileIndexAt(i, j + 1, "Buildings") != -1
                                    || _mineShaft.getTileIndexAt(i + 1, j, "Buildings") != -1
                                    || _mineShaft.terrainFeatures.ContainsKey(new Vector2(i - 1, j))
                                    || _mineShaft.terrainFeatures.ContainsKey(new Vector2(i + 1, j))
                                    || _mineShaft.terrainFeatures.ContainsKey(new Vector2(i, j - 1))
                                    || _mineShaft.terrainFeatures.ContainsKey(new Vector2(i, j + 1))
                                )
                            )
                            {
                                _mineShaft.terrainFeatures.Add(
                                    new Vector2(i, j),
                                    new Grass(
                                        (_mineShaft.mineLevel >= 50) ? 6 : 5,
                                        (_mineShaft.mineLevel >= 50)
                                            ? 1
                                            : _mineShaft.mineRandom.Next(1, 5)
                                    )
                                );
                            }
                            if (
                                _mineShaft.mineRandom.NextDouble() < 0.1
                                && (
                                    _mineShaft.getTileIndexAt(i, j - 1, "Buildings") != -1
                                    || _mineShaft.getTileIndexAt(i - 1, j, "Buildings") != -1
                                    || _mineShaft.getTileIndexAt(i, j + 1, "Buildings") != -1
                                    || _mineShaft.getTileIndexAt(i + 1, j, "Buildings") != -1
                                    || _mineShaft.terrainFeatures.ContainsKey(new Vector2(i - 1, j))
                                    || _mineShaft.terrainFeatures.ContainsKey(new Vector2(i + 1, j))
                                    || _mineShaft.terrainFeatures.ContainsKey(new Vector2(i, j - 1))
                                    || _mineShaft.terrainFeatures.ContainsKey(new Vector2(i, j + 1))
                                )
                            )
                            {
                                _mineShaft.terrainFeatures.Add(
                                    new Vector2(i, j),
                                    new Grass(4, _mineShaft.mineRandom.Next(1, 5))
                                );
                            }
                        }
                    }
                    else if (
                        IsContainerPlatform(i, j)
                        && _mineShaft.CanItemBePlacedHere(new Vector2(i, j))
                        && _mineShaft.mineRandom.NextDouble() < 0.4
                        && (firstTime || CanAdd(0, barrelsAdded))
                    )
                    {
                        Vector2 objectPos = new Vector2(i, j);
                        _mineShaft.objects.Add(
                            objectPos,
                            BreakableContainer.GetBarrelForMines(objectPos, _mineShaft)
                        );
                        barrelsAdded++;
                        if (firstTime)
                        {
                            _mineShaft.updateMineLevelData(0);
                        }
                    }
                }
            }
            if (_mineShaft.stonesLeftOnThisLevel > 70)
            {
                int tries = _mineShaft.stonesLeftOnThisLevel / 70;
                for (int i = 0; i < tries; i++)
                {
                    if (
                        !StardewValley.Utility.TryGetRandom(
                            _mineShaft.objects,
                            out var stone,
                            out var obj
                        ) || !obj.IsBreakableStone()
                    )
                    {
                        continue;
                    }
                    int radius = _mineShaft.mineRandom.Next(3, 8);
                    for (
                        int x = (int)stone.X - radius / 2;
                        (float)x < stone.X + (float)(radius / 2);
                        x++
                    )
                    {
                        for (
                            int y = (int)stone.Y - radius / 2;
                            (float)y < stone.Y + (float)(radius / 2);
                            y++
                        )
                        {
                            Vector2 tile = new Vector2(x, y);
                            if (
                                !_mineShaft.objects.ContainsKey(tile)
                                || !_mineShaft.objects[tile].IsBreakableStone()
                            )
                            {
                                continue;
                            }
                            _mineShaft.objects.Remove(tile);
                            _mineShaft.stonesLeftOnThisLevel--;
                        }
                    }
                }
            }
            _mineShaft.tryToAddAreaUniques();
            if (_mineShaft.EnemyCount <= 1)
            {
                _mineShaft.characters.Add(
                    new TombGhost(_mineShaft.tileBeneathLadder * 64f + new Vector2(256f, 256f))
                );
            }
            _mineShaft.tryToAddOreClumps();
            _mineShaft.updateMap();
        }

        private void ChooseLevelType(int _mineLevel)
        {
            _mineShaft.fogTime = 0;
            _mineShaft.ambientFog = false;
            _mineShaft.rainbowLights.Value = false;
            _mineShaft.isLightingDark.Value = false;
            Random r = StardewValley.Utility.CreateDaySaveRandom(
                Game1.stats.DaysPlayed,
                _mineShaft.mineLevel,
                4 * _mineShaft.mineLevel
            );
            _mineShaft.lighting = new Color(80, 80, 40);
            if (_mineShaft.getMineArea(_mineLevel) == 80)
            {
                _mineShaft.lighting = new Color(100, 100, 50);
            }
            if (r.NextDouble() < 0.3 && _mineLevel > 2)
            {
                _mineShaft.isLightingDark.Value = true;
                _mineShaft.lighting = new Color(120, 120, 40);
                if (r.NextDouble() < 0.3)
                {
                    _mineShaft.lighting = new Color(150, 150, 60);
                }
            }
            if (r.NextDouble() < 0.15 && _mineLevel > 5 && _mineLevel != 120)
            {
                _mineShaft.isLightingDark.Value = true;
                switch (_mineShaft.getMineArea(_mineLevel))
                {
                    case 0:
                    case 10:
                        _mineShaft.lighting = new Color(110, 110, 70);
                        break;
                    case 40:
                        _mineShaft.lighting = Color.Black;
                        break;
                    case 80:
                        _mineShaft.lighting = new Color(90, 130, 70);
                        break;
                }
            }
            if (_mineShaft.isDarkArea() && _mineLevel < 120)
            {
                _mineShaft.isLightingDark.Value = true;
                _mineShaft.lighting = (
                    (_mineShaft.getMineArea(_mineLevel) == 80)
                        ? new Color(70, 100, 100)
                        : new Color(165, 165, 165)
                );
                if (_mineShaft.getMineArea(_mineLevel) == 0)
                {
                    _mineShaft.ambientFog = true;
                    _mineShaft.fogColor = Color.Black;
                }
            }
        }

        private void AdjustLevelChances(
            ref double stoneChance,
            ref double itemChance,
            ref double gemStoneChance
        )
        {
            double equation(double value) =>
                value
                * (1 + Game1.player.team.AverageDailyLuck(_mineShaft))
                * _spawnConfiguration.Global.MineMultiplier;
            gemStoneChance = equation(gemStoneChance);
            itemChance = equation(itemChance);
            stoneChance = equation(stoneChance);
        }

        private bool CanAdd(int typeOfFeature, int numberSoFar)
        {
            if (MineShaft.permanentMineChanges.TryGetValue(_mineShaft.mineLevel, out var changes))
            {
                switch (typeOfFeature)
                {
                    case 0:
                        return changes.platformContainersLeft > numberSoFar;
                    case 1:
                        return changes.chestsLeft > numberSoFar;
                    case 2:
                        return changes.coalCartsLeft > numberSoFar;
                    case 3:
                        return changes.elevator == 0;
                }
            }
            return true;
        }

        private bool IsContainerPlatform(int x, int y)
        {
            var validIndexes = new int[] { 257, 263 };
            var backTile = _mineShaft.getTileIndexAt(x, y, "Back");
            var back2Tile = _mineShaft.getTileIndexAt(x, y, "Back2");
            return validIndexes.Contains(backTile) || validIndexes.Contains(back2Tile);
        }

        private StardewValley.Object GetRandomItemForMineShaftLevel()
        {
            var items = new List<(string Id, int Price)>
            {
                (Id: "563", Price: 150), //Jasper
                (Id: "568", Price: 60), //Sandstone
                (Id: "570", Price: 175), //Basalt
                (Id: "571", Price: 15), //Limestone
                (Id: "573", Price: 150), //Hematite
                (Id: "575", Price: 200), //Obsidian
                (Id: "559", Price: 120), //Pyrite
                (Id: "556", Price: 80), //Orpiment
                (Id: "60", Price: 250), //Emerald
                (Id: "62", Price: 180), //Aquamarine
                (Id: "64", Price: 250), //Ruby
                (Id: "66", Price: 100), //Amethyst
                (Id: "68", Price: 80), //Topaz
                (Id: "70", Price: 200), //Jade
                (Id: "72", Price: 750), //Diamond
                (Id: "74", Price: 2000), //Prismatic Shard
                (Id: "275", Price: 1000), //Artifact Trove
                (Id: "166", Price: 5000), //Treasure Chest
            };
            var item = items.GetRandomItem(i => i.Price, useInverseWeights: true);
            try
            {
                return new StardewValley.Object(item.Id, 1) { IsSpawnedObject = true };
            }
            catch (System.Exception)
            {
                if (item.Id == null)
                {
                    LoggerHelper.Log("Couldn't get random item");
                }
                else
                {
                    LoggerHelper.Log("Couldn't get random item unknow");
                }
                return new StardewValley.Object("72", 1) { IsSpawnedObject = true };
            }
        }
    }
}
