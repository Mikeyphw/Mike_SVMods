using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.DesertTomb.Diagnostics;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Base;
using EggsInWild.Infrastructure.DesertTomb.Projectiles;

namespace EggsInWild.Infrastructure.DesertTomb.Monsters.Creatures;

/// <summary>
/// ShadowShaman-derived caster that keeps vanilla shaman behavior,
/// but replaces the offensive projectile with a custom spore shot.
/// </summary>
internal sealed class Matango : ShadowShaman
{
    private static int FrameWidth => DebugKnobs.MatangoFrameWidth;
    private static int FrameHeight => DebugKnobs.MatangoFrameHeight;
    private static int VisionDistance => DebugKnobs.MatangoVisionTiles;
    private static int SpellCooldownMs => DebugKnobs.MatangoSpellCooldownMs;
    private static int CastWindupMs => DebugKnobs.MatangoCastWindupMs;
    private static int TileCenterTolerancePx => DebugKnobs.MatangoTileCenterTolerancePx;
    private readonly string _spriteTexture;
    private bool? _previousCasting;
    private bool _previousSpottedPlayer;
    private int _walkDir = 1;

    internal Matango(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 3,
        string textureName = "Matango",
        int speedToAdd = 0,
        string? customDropData = null
    )
        : base(position)
    {
        _spriteTexture = $"Characters\\Monsters\\{textureName.Replace(" ", "")}";

        DamageToFarmer = 16;
        resilience.Value = 2;
        Speed = 1;
        coolDown = SpellCooldownMs;

        TombMonsterBuilder.SetupBasicMonster(
            this,
            multipliers,
            difficulty,
            textureName,
            speedToAdd + 2,
            luckyBonusEnabled: true,
            customDropData: customDropData
        );

        TombMonsterBuilder.SetMonsterNames(this, "Matango");
        reloadSprite();
    }

    public override void reloadSprite(bool onlyAppearance = false)
    {
        Sprite = new AnimatedSprite(_spriteTexture, 0, FrameWidth, FrameHeight);
        Sprite.SpriteHeight = FrameHeight;
        Sprite.UpdateSourceRect();
    }

    public override int takeDamage(
        int damage,
        int xTrajectory,
        int yTrajectory,
        bool isBomb,
        double addedPrecision,
        Farmer who
    )
    {
        var result = base.takeDamage(damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);
        if (result > 0)
            CustomMonsterDebug.Matango(this, $"took damage={result} hp={Health}/{MaxHealth} casting={casting.Value}");
        else if (result < 0)
            CustomMonsterDebug.Matango(this, "attack missed");

        return result;
    }

    protected override void updateAnimation(GameTime time)
    {
        base.updateMonsterSlaveAnimation(time);
        resetAnimationSpeed();
    }

    public override void behaviorAtGameTick(GameTime time)
    {
        LogStateIfChanged();
        CustomMonsterDebug.MatangoHeartbeat(this, "tick", $"heartbeat spotted={spottedPlayer} casting={casting.Value} cooldownMs={coolDown} moving={isMoving()} walkDir={_walkDir} invisible={IsInvisible}");

        if (timeBeforeAIMovementAgain > 0f)
            timeBeforeAIMovementAgain -= time.ElapsedGameTime.Milliseconds;

        if (Player.isRafting && withinPlayerThreshold(4))
        {
            IsWalkingTowardPlayer = false;
            MoveAwayFromPlayer();
            MovePosition(time, Game1.viewport, currentLocation);
            return;
        }

        if (timeBeforeAIMovementAgain <= 0f)
            IsInvisible = false;

        if (casting.Value)
        {
            IsWalkingTowardPlayer = false;
            Halt();
            Sprite.Animate(time, 16, 4, 200f);
            rotationTimer = (float)((double)((float)time.TotalGameTime.Milliseconds * ((float)Math.PI / 128f) / 24f) % (Math.PI * 1024.0));
            coolDown -= time.ElapsedGameTime.Milliseconds;
            CustomMonsterDebug.MatangoHeartbeat(this, "casting_tick", $"casting tick cooldownMs={coolDown}", 10);
            if (coolDown > 0)
                return;

            Scale = 1f;
            FireCustomProjectile();
            casting.Value = false;
            coolDown = SpellCooldownMs;
            spottedPlayer = false;
            IsWalkingTowardPlayer = false;
            CustomMonsterDebug.Matango(this, $"cast resolved -> patrol cooldownMs={coolDown}");
            return;
        }

        coolDown -= time.ElapsedGameTime.Milliseconds;

        if (TryStartCasting())
        {
            return;
        }

        IsWalkingTowardPlayer = false;
        spottedPlayer = false;
        controller = null;
        PatrolStraight(time);
    }

    private bool TryStartCasting()
    {
        if (coolDown > 0)
        {
            CustomMonsterDebug.MatangoHeartbeat(this, "cast_blocked_cooldown", $"cast blocked: cooldownMs={coolDown}", 30);
            return false;
        }

        if (Player is null)
        {
            CustomMonsterDebug.MatangoHeartbeat(this, "cast_blocked_no_target", "cast blocked: no target", 30);
            return false;
        }

        if (!IsCenteredInTile())
        {
            var box = GetBoundingBox();
            CustomMonsterDebug.MatangoHeartbeat(this, "cast_blocked_not_centered", $"cast blocked: not centered centerPx={Mod(box.Center.X, 64)},{Mod(box.Center.Y, 64)}", 20);
            return false;
        }

        if (!IsPlayerInLineOfSight(Player))
        {
            CustomMonsterDebug.MatangoHeartbeat(this, "cast_blocked_los", $"cast blocked: no line of sight targetTile=({Player.TilePoint.X},{Player.TilePoint.Y})", 20);
            return false;
        }

        spottedPlayer = true;
        casting.Value = true;
        controller = null;
        IsWalkingTowardPlayer = false;
        Halt();
        FaceTargetInLine(Player);
        _walkDir = FacingDirection;
        coolDown = CastWindupMs;
        currentLocation.playSound("debuffSpell");
        CustomMonsterDebug.Matango(this, $"start casting targetTile=({Player.TilePoint.X},{Player.TilePoint.Y}) walkDir={_walkDir}");
        return true;
    }

    private void PatrolStraight(GameTime time)
    {
        if (IsBlockedAhead())
        {
            var oldDir = _walkDir;
            if (TryReverseDirection())
                CustomMonsterDebug.Matango(this, $"reverse direction old={oldDir} new={_walkDir}");
            else
                ChooseNewDirection(oldDir);
        }

        SetMoveDirection(_walkDir);
        MovePosition(time, Game1.viewport, currentLocation);
    }

    private bool TryReverseDirection()
    {
        var reverseDir = (_walkDir + 2) % 4;
        if (IsDirectionPassable(reverseDir))
        {
            _walkDir = reverseDir;
            return true;
        }

        return false;
    }

    private void ChooseNewDirection(int oldDir)
    {
        foreach (var candidate in new[] { 0, 1, 2, 3 })
        {
            if (candidate == oldDir || candidate == (oldDir + 2) % 4)
                continue;

            if (!IsDirectionPassable(candidate))
                continue;

            _walkDir = candidate;
            CustomMonsterDebug.Matango(this, $"turn direction old={oldDir} new={candidate}");
            return;
        }

        Halt();
        CustomMonsterDebug.Matango(this, $"all directions blocked old={oldDir}");
    }

    private bool IsBlockedAhead()
        => !IsDirectionPassable(_walkDir);

    private bool IsDirectionPassable(int dir)
    {
        var next = nextPosition(dir);
        return currentLocation.isTilePassable(next, Game1.viewport)
            && !currentLocation.isCollidingPosition(next, Game1.viewport, isFarmer: false, DamageToFarmer, isGlider.Value, this);
    }

    private bool IsPlayerInLineOfSight(Farmer target)
    {
        var myTile = TilePoint;
        var playerTile = target.TilePoint;
        var aligned = myTile.X == playerTile.X || myTile.Y == playerTile.Y;
        if (!aligned)
            return false;

        if (Math.Abs(myTile.X - playerTile.X) > VisionDistance || Math.Abs(myTile.Y - playerTile.Y) > VisionDistance)
            return false;

        return Utility.doesPointHaveLineOfSightInMine(currentLocation, Tile, target.Tile, VisionDistance);
    }

    private void FaceTargetInLine(Farmer target)
    {
        var myTile = TilePoint;
        var playerTile = target.TilePoint;
        if (playerTile.X > myTile.X) FacingDirection = 1;
        else if (playerTile.X < myTile.X) FacingDirection = 3;
        else if (playerTile.Y > myTile.Y) FacingDirection = 2;
        else FacingDirection = 0;
    }

    private bool IsCenteredInTile()
    {
        var box = GetBoundingBox();
        var centerPx = Mod(box.Center.X, 64);
        var centerPy = Mod(box.Center.Y, 64);
        return Math.Abs(centerPx - 32) <= TileCenterTolerancePx && Math.Abs(centerPy - 32) <= TileCenterTolerancePx;
    }

    private static int Mod(int value, int divisor)
    {
        var result = value % divisor;
        return result < 0 ? result + divisor : result;
    }

    private void SetMoveDirection(int dir)
    {
        SetMovingUp(false);
        SetMovingDown(false);
        SetMovingLeft(false);
        SetMovingRight(false);
        FacingDirection = dir;

        switch (dir)
        {
            case 0:
                SetMovingUp(true);
                break;
            case 1:
                SetMovingRight(true);
                break;
            case 2:
                SetMovingDown(true);
                break;
            case 3:
                SetMovingLeft(true);
                break;
        }
    }

    private void MoveAwayFromPlayer()
    {
        var monsterPixel = StandingPixel;
        var playerPixel = Player.StandingPixel;
        if (Math.Abs(playerPixel.Y - monsterPixel.Y) > 192)
        {
            if (playerPixel.X - monsterPixel.X > 0)
                SetMovingLeft(true);
            else
                SetMovingRight(true);
        }
        else if (playerPixel.Y - monsterPixel.Y > 0)
        {
            SetMovingUp(true);
        }
        else
        {
            SetMovingDown(true);
        }
    }

    private void LogStateIfChanged()
    {
        if (_previousSpottedPlayer != spottedPlayer)
        {
            CustomMonsterDebug.Matango(this, $"spottedPlayer -> {spottedPlayer}");
            _previousSpottedPlayer = spottedPlayer;
        }

        if (_previousCasting != casting.Value)
        {
            CustomMonsterDebug.Matango(this, $"casting -> {casting.Value} cooldownMs={coolDown}");
            _previousCasting = casting.Value;
        }
    }

    private void FireCustomProjectile()
    {
        var monsterBounds = GetBoundingBox();
        var velocityTowardPlayer = Utility.getVelocityTowardPlayer(monsterBounds.Center, 15f, Player);
        currentLocation.projectiles.Add(new MatangoSporeProjectile(
            DamageToFarmer,
            velocityTowardPlayer.X,
            velocityTowardPlayer.Y,
            new Vector2(monsterBounds.X, monsterBounds.Y),
            currentLocation,
            this));
        CustomMonsterDebug.Matango(this, $"fired spore projectile velocity=({velocityTowardPlayer.X:0.##},{velocityTowardPlayer.Y:0.##})");
    }
}
