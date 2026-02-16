using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using xTile.Layers;
using WildEggsHatching.DesertTomb.Core.Configuration;

namespace WildEggsHatching.Application.DesertTomb.Generation;

/// <summary>
/// Applies DesertTomb visual and object population changes to a <see cref="MineShaft"/> floor.
/// This is intentionally side-effectful (SDV state), and should be called only when the floor
/// is already determined to be a tomb.
/// </summary>
public sealed class DesertTombLevelGenerator
{
    private readonly SpawnConfiguration _config;

    public DesertTombLevelGenerator(SpawnConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public void Generate(MineShaft mineShaft)
    {
        if (mineShaft is null)
            throw new ArgumentNullException(nameof(mineShaft));

        // Uses a custom tilesheet name so the TMX can reference it consistently.
        // The asset loader is responsible for mapping this key to a mod-file PNG.
        mineShaft.mapImageSource.Value = "Maps/Mines/Z_TombTilesheet";

        mineShaft.isDinoArea = true;
        mineShaft.loadedDarkArea = true;

        ApplyLighting(mineShaft, mineShaft.mineLevel);
        PopulateBreakables(mineShaft);

        mineShaft.tryToAddOreClumps();
        mineShaft.updateMap();
    }

    private static void ApplyLighting(MineShaft mineShaft, int mineLevel)
    {
        mineShaft.fogTime = 0;
        mineShaft.ambientFog = false;
        mineShaft.rainbowLights.Value = false;
        mineShaft.isLightingDark.Value = false;

        Random r = Utility.CreateDaySaveRandom(Game1.stats.DaysPlayed, mineShaft.mineLevel, 4 * mineShaft.mineLevel);

        mineShaft.lighting = new Color(80, 80, 40);
        if (mineShaft.getMineArea(mineLevel) == 80)
            mineShaft.lighting = new Color(100, 100, 50);

        if (r.NextDouble() < 0.3 && mineLevel > 2)
        {
            mineShaft.isLightingDark.Value = true;
            mineShaft.lighting = new Color(120, 120, 40);
            if (r.NextDouble() < 0.3)
                mineShaft.lighting = new Color(150, 150, 60);
        }

        if (r.NextDouble() < 0.15 && mineLevel > 5 && mineLevel != 120)
        {
            mineShaft.isLightingDark.Value = true;
            switch (mineShaft.getMineArea(mineLevel))
            {
                case 0:
                case 10:
                    mineShaft.lighting = new Color(110, 110, 70);
                    break;
                case 40:
                    mineShaft.lighting = Color.Black;
                    break;
                case 80:
                    mineShaft.lighting = new Color(90, 130, 70);
                    break;
            }
        }

        if (mineShaft.isDarkArea() && mineLevel < 120)
        {
            mineShaft.isLightingDark.Value = true;
            mineShaft.lighting = (mineShaft.getMineArea(mineLevel) == 80)
                ? new Color(70, 100, 100)
                : new Color(165, 165, 165);

            if (mineShaft.getMineArea(mineLevel) == 0)
            {
                mineShaft.ambientFog = true;
                mineShaft.fogColor = Color.Black;
            }
        }
    }

    private void PopulateBreakables(MineShaft mineShaft)
    {
        mineShaft.ghostAdded = false;
        mineShaft.stonesLeftOnThisLevel = 0;

        var backLayer = mineShaft.map.RequireLayer("Back");

        int numBarrels = mineShaft.mineRandom.Next(5)
            + (int)(Game1.player.team.AverageDailyLuck(mineShaft) * 20.0);

        numBarrels += backLayer.LayerWidth * backLayer.LayerHeight / 40;

        // Preserve the legacy "edge-to-interior scan" placement style for containers.
        for (int i = 0; i < numBarrels; i++)
        {
            Point p;
            Point motion;

            if (mineShaft.mineRandom.NextDouble() < 0.33)
            {
                p = new Point(mineShaft.mineRandom.Next(backLayer.LayerWidth), 0);
                motion = new Point(0, 1);
            }
            else if (mineShaft.mineRandom.NextBool())
            {
                p = new Point(0, mineShaft.mineRandom.Next(backLayer.LayerHeight));
                motion = new Point(1, 0);
            }
            else
            {
                p = new Point(backLayer.LayerWidth - 1, mineShaft.mineRandom.Next(backLayer.LayerHeight));
                motion = new Point(-1, 0);
            }

            int guard = 0;
            while (mineShaft.isTileOnMap(p.X, p.Y) && guard++ < 512)
            {
                p.X += motion.X;
                p.Y += motion.Y;

                if (!mineShaft.isTileClearForMineObjects(p.X, p.Y) || mineShaft.mineRandom.NextDouble() > 0.25)
                    continue;

                var tile = new Vector2(p.X, p.Y);

                // Prefer barrels; the legacy plant decoration uses a custom TombCosmeticPlant
                // which is ported later as part of the monster/visual system.
                mineShaft.objects[tile] = BreakableContainer.GetBarrelForMines(tile, mineShaft);
                break;
            }
        }
    }
}
