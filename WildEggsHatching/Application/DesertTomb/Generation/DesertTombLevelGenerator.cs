using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using xTile.Layers;
using WildEggsHatching;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.Core.Assets;

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
        mineShaft.mapImageSource.Value = WeggGameAssetNames.DesertTombTilesheet;

        mineShaft.isDinoArea = false;
        mineShaft.loadedDarkArea = true;

        ApplyLighting(mineShaft, mineShaft.mineLevel);
        ApplyTombAtmosphere(mineShaft);
        PopulateBreakables(mineShaft);
        PlaceTombLights(mineShaft);

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

        // Preserve the prior "edge-to-interior scan" placement style for containers.
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

                // Prefer barrels; the prior plant decoration uses a custom TombCosmeticPlant
                // which is ported later as part of the monster/visual system.
                mineShaft.objects[tile] = BreakableContainer.GetBarrelForMines(tile, mineShaft);
                break;
            }
        }
    

        ClusterBreakables(mineShaft);
    }


    

    private static void ClusterBreakables(MineShaft mineShaft)
    {
        // Make placements feel intentional: small urn-like clusters near existing containers.
        var keys = new System.Collections.Generic.List<Vector2>(mineShaft.objects.Keys);
        foreach (var origin in keys)
        {
            if (mineShaft.mineRandom.NextDouble() > 0.35)
                continue;

            for (int i = 0; i < 2; i++)
            {
                int dx = mineShaft.mineRandom.Next(-1, 2);
                int dy = mineShaft.mineRandom.Next(-1, 2);
                if (dx == 0 && dy == 0)
                    continue;

                var t = origin + new Vector2(dx, dy);
                if (mineShaft.objects.ContainsKey(t))
                    continue;

                if (!mineShaft.isTileClearForMineObjects((int)t.X, (int)t.Y))
                    continue;

                mineShaft.objects[t] = BreakableContainer.GetBarrelForMines(t, mineShaft);
            }
        }
    }

private static void ApplyTombAtmosphere(MineShaft mineShaft)
    {
        // Keep this deterministic and subtle: a warm haze reads "ancient tomb" without new assets.
        mineShaft.rainbowLights.Value = false;
        mineShaft.ambientFog = true;
        mineShaft.fogColor = new Color(220, 200, 140) * 0.18f;
        mineShaft.fogTime = 0;

        // Slightly darker, warmer baseline.
        mineShaft.isLightingDark.Value = true;
        mineShaft.lighting = new Color(105, 95, 55);
    }

    private static void PlaceTombLights(MineShaft mineShaft)
    {
        var back = mineShaft.map.RequireLayer("Back");
        var buildings = mineShaft.map.GetLayer("Buildings");

        int target = Math.Clamp(back.LayerWidth * back.LayerHeight / 220, 10, 40);
        int placed = 0;
        int attempts = target * 30;

        for (int i = 0; i < attempts && placed < target; i++)
        {
            int x = mineShaft.mineRandom.Next(2, back.LayerWidth - 2);
            int y = mineShaft.mineRandom.Next(2, back.LayerHeight - 2);

            if (!mineShaft.isTileClearForMineObjects(x, y))
                continue;

            // Avoid blocking entry/ladder areas.
            var tile = new Vector2(x, y);
            if (mineShaft.tileBeneathLadder != Vector2.Zero && Vector2.Distance(tile, mineShaft.tileBeneathLadder) < 4f)
                continue;

            if (mineShaft.tileBeneathElevator != Vector2.Zero && Vector2.Distance(tile, mineShaft.tileBeneathElevator) < 4f)
                continue;

            // Prefer "wall-adjacent" placements so lights read like sconces/braziers.
            if (buildings is not null)
            {
                bool wallNear =
                    buildings.Tiles[x, y - 1] is not null ||
                    buildings.Tiles[x - 1, y] is not null ||
                    buildings.Tiles[x + 1, y] is not null;

                if (!wallNear)
                    continue;
            }

            string id = $"{Constants.ModId}_tomb_light_{mineShaft.mineLevel}_{x}_{y}";
            var pos = new Vector2(x * 64f + 32f, y * 64f + 32f);

            mineShaft.sharedLights.AddLight(new LightSource(
                id,
                4,
                pos,
                1.8f,
                new Color(255, 210, 120) * 0.7f,
                LightSource.LightContext.None,
                0L,
                mineShaft.NameOrUniqueName
            ));

            placed++;
        }
    }

}