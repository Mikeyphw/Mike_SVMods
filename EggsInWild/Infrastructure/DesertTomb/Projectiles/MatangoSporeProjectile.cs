using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Projectiles;
using StardewValley.TerrainFeatures;

namespace EggsInWild.Infrastructure.DesertTomb.Projectiles;

internal sealed class MatangoSporeProjectile : BasicProjectile
{
    private const string TexturePath = "Characters/Monsters/matango_projectile";
    private const int FrameSize = 16;
    private const int ExplosionFrame = 1;
    private const int ExplosionDurationMs = 140;

    private readonly NetBool _exploding = new(false);
    private readonly NetInt _explosionTimerMs = new(0);

    public MatangoSporeProjectile()
    {
    }

    public MatangoSporeProjectile(
        int damageToFarmer,
        float xVelocity,
        float yVelocity,
        Vector2 startingPosition,
        GameLocation location,
        Character firer)
        : base(
            damageToFarmer,
            spriteIndex: 13,
            bouncesTillDestruct: 0,
            tailLength: 0,
            rotationVelocity: 0f,
            xVelocity: xVelocity,
            yVelocity: yVelocity,
            startingPosition: startingPosition,
            collisionSound: "debuffSpell",
            bounceSound: null,
            firingSound: "debuffSpell",
            explode: false,
            damagesMonsters: false,
            location: location,
            firer: firer)
    {
        IgnoreLocationCollision = false;
        ignoreObjectCollisions.Value = false;
        ignoreCharacterCollisions.Value = false;
        boundingBoxWidth.Value = 16;
    }

    protected override void InitNetFields()
    {
        base.InitNetFields();
        NetFields.AddField(_exploding, nameof(_exploding)).AddField(_explosionTimerMs, nameof(_explosionTimerMs));
    }

    public override bool update(GameTime time, GameLocation location)
    {
        if (_exploding.Value)
        {
            _explosionTimerMs.Value = Math.Max(0, _explosionTimerMs.Value - time.ElapsedGameTime.Milliseconds);
            return _explosionTimerMs.Value <= 0;
        }

        return base.update(time, location);
    }

    public override void draw(SpriteBatch b)
    {
        var texture = Game1.content.Load<Texture2D>(TexturePath);
        var sourceRect = new Rectangle(_exploding.Value ? FrameSize : 0, 0, FrameSize, FrameSize);
        var pixelPosition = position.Value;
        b.Draw(
            texture,
            Game1.GlobalToLocal(Game1.viewport, pixelPosition + new Vector2(32f, 32f)),
            sourceRect,
            Color.White * alpha.Value,
            rotation,
            new Vector2(FrameSize / 2f, FrameSize / 2f),
            4f * localScale,
            SpriteEffects.None,
            (pixelPosition.Y + 96f) / 10000f);
    }

    public override void behaviorOnCollisionWithTerrainFeature(TerrainFeature t, Vector2 tileLocation, GameLocation location)
    {
        t.performUseAction(tileLocation);
        explosionAnimation(location);
        piercesLeft.Value--;
    }

    protected override void explosionAnimation(GameLocation location)
    {
        if (_exploding.Value)
            return;

        _exploding.Value = true;
        _explosionTimerMs.Value = ExplosionDurationMs;
        xVelocity.Value = 0f;
        yVelocity.Value = 0f;
        acceleration.X = 0f;
        acceleration.Y = 0f;
        ignoreCharacterCollisions.Value = true;
        ignoreObjectCollisions.Value = true;
        ignoreLocationCollision.Value = true;
        currentTileSheetIndex.Value = ExplosionFrame;

        if (!string.IsNullOrEmpty(collisionSound.Value))
            location.playSound(collisionSound.Value);

        collisionBehavior?.Invoke(location, getBoundingBox().Center.X, getBoundingBox().Center.Y, GetPlayerWhoFiredMe(location));
    }
}
