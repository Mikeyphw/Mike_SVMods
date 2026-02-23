using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using WildEggsHatching.Infrastructure.Config;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters.Creatures;
using Mikew.BirdsOfAFeather.Helpers.Config;

namespace WildEggsHatching.Infrastructure.DesertTomb.Diagnostics;

/// <summary>
/// Renders a HUD arrow pointing toward <see cref="ThePharaoh"/> when the boss is off-screen.
/// </summary>
public sealed class PharaohTrackerOverlay
{
    private readonly IModHelper _helper;
    private readonly SmapiConfigProvider<ModConfig> _config;

    private readonly Lazy<Texture2D> _trackerTexture;

    public PharaohTrackerOverlay(IModHelper helper, SmapiConfigProvider<ModConfig> config)
    {
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        _trackerTexture = new Lazy<Texture2D>(() => _helper.ModContent.Load<Texture2D>("assets/mines/tracker.png"));
    }

    public void Render(SpriteBatch spriteBatch)
    {
        if (spriteBatch is null)
            throw new ArgumentNullException(nameof(spriteBatch));

        if (!_config.Get().DesertTombPharaohTrackerEnabled)
            return;

        if (Game1.currentLocation is not MineShaft mineShaft)
            return;

        var pharaoh = mineShaft.characters.FirstOrDefault(c => c is ThePharaoh) as ThePharaoh;
        if (pharaoh is null)
            return;

        var tile = pharaoh.Tile;
        var worldCenterPx = tile * 64f + new Vector2(32f, 32f);

        var viewportWorld = new Rectangle(Game1.viewport.X, Game1.viewport.Y, Game1.viewport.Width, Game1.viewport.Height);
        var screenBounds = Game1.graphics.GraphicsDevice.Viewport.Bounds;

        if (!PharaohTrackerMath.TryComputeArrow(
                worldCenterPx,
                viewportWorld,
                screenBounds,
                marginPx: 64,
                out var screenPos,
                out var rotation))
        {
            return;
        }

        var srcRect = new Rectangle(0, 0, 11, 15);
        const float scale = 6f;
        var renderSize = new Vector2(srcRect.Width * scale, srcRect.Height * scale);

        var safePos = PharaohTrackerMath.ClampToScreen(screenPos, screenBounds, renderSize);

        spriteBatch.Draw(
            _trackerTexture.Value,
            safePos,
            srcRect,
            Color.White,
            rotation,
            new Vector2(2f, 2f),
            scale,
            SpriteEffects.None,
            1f
        );
    }
}