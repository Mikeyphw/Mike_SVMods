using System;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters.Base;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters.Creatures;

/// <summary>
/// Fast straight-line walker that pauses to fire a high-speed spore shot when a player enters line of sight.
/// </summary>
internal sealed class Matango : Monster
{
    private enum MatangoState
    {
        Walking = 0,
        Casting = 1,
        Recover = 2,
    }

    private const int VisionTiles = 8;
    private const int CastWindupMs = 450;
    private const int RecoverMs = 250;
    private const int ShotCooldownMs = 2200;

    private readonly NetInt _state = new((int)MatangoState.Walking);
    private readonly NetInt _stateTimerMs = new(0);
    private readonly NetInt _shotCooldownMs = new(0);
    private readonly NetInt _walkDir = new(1); // 0 up, 1 right, 2 down, 3 left
    private readonly NetString _spriteTexture = new();

    internal Matango(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 3,
        string textureName = "Matango",
        int speedToAdd = 0,
        string? customDropData = null
    )
        : base("Matango", position)
    {
        _spriteTexture.Value = $"Characters\\Monsters\\{textureName.Replace(" ", "")}";

        MaxHealth = 120;
        Health = MaxHealth;
        DamageToFarmer = 16;
        resilience.Value = 2;
        Speed = 4;

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
    }

    protected override void initNetFields()
    {
        base.initNetFields();
        NetFields
            .AddField(_state, nameof(_state))
            .AddField(_stateTimerMs, nameof(_stateTimerMs))
            .AddField(_shotCooldownMs, nameof(_shotCooldownMs))
            .AddField(_walkDir, nameof(_walkDir))
            .AddField(_spriteTexture, nameof(_spriteTexture));
    }

    public override void reloadSprite(bool onlyAppearance = false)
    {
        Sprite = new AnimatedSprite(_spriteTexture.Value, 0, 16, 16);
    }

    public override void behaviorAtGameTick(GameTime time)
    {
        base.behaviorAtGameTick(time);

        var elapsedMs = (int)Math.Max(0, time.ElapsedGameTime.TotalMilliseconds);

        if (_shotCooldownMs.Value > 0)
            _shotCooldownMs.Value = Math.Max(0, _shotCooldownMs.Value - elapsedMs);

        if (_stateTimerMs.Value > 0)
            _stateTimerMs.Value = Math.Max(0, _stateTimerMs.Value - elapsedMs);

        switch ((MatangoState)_state.Value)
        {
            case MatangoState.Casting:
                IsWalkingTowardPlayer = false;
                Halt();

                if (_stateTimerMs.Value <= 0 && Game1.IsMasterGame)
                {
                    FireSporeShot();
                    _state.Value = (int)MatangoState.Recover;
                    _stateTimerMs.Value = RecoverMs;
                    _shotCooldownMs.Value = ShotCooldownMs;
                }

                return;

            case MatangoState.Recover:
                IsWalkingTowardPlayer = false;
                Halt();

                if (_stateTimerMs.Value <= 0)
                    _state.Value = (int)MatangoState.Walking;

                return;

            case MatangoState.Walking:
            default:
                break;
        }

        if (Game1.IsMasterGame)
            TryStartCasting();

        WalkStraight();
    }

    protected override void updateMonsterSlaveAnimation(GameTime time)
    {
        if ((MatangoState)_state.Value == MatangoState.Casting)
        {
            Sprite.Animate(time, 16, 4, 120f);
            return;
        }

        if (!isMoving())
        {
            Sprite.StopAnimation();
            return;
        }

        switch (FacingDirection)
        {
            case 0:
                Sprite.AnimateUp(time);
                break;
            case 1:
                Sprite.AnimateRight(time);
                break;
            case 2:
                Sprite.AnimateDown(time);
                break;
            case 3:
                Sprite.AnimateLeft(time);
                break;
        }
    }

    private void TryStartCasting()
    {
        if (_shotCooldownMs.Value > 0)
            return;

        var target = Player;
        if (target is null)
            return;

        if (!IsCenteredInTile())
            return;

        if (!IsPlayerInLineOfSight(target))
            return;

        Halt();
        IsWalkingTowardPlayer = false;

        FaceTargetInLine(target);
        _walkDir.Value = FacingDirection;
        _state.Value = (int)MatangoState.Casting;
        _stateTimerMs.Value = CastWindupMs;

        currentLocation.playSound("debuffSpell");
    }

    private bool IsPlayerInLineOfSight(Farmer target)
    {
        if (target is null)
            return false;

        var myTile = TilePoint;
        var playerTile = target.TilePoint;

        var aligned = myTile.X == playerTile.X || myTile.Y == playerTile.Y;
        if (!aligned)
            return false;

        if (Math.Abs(myTile.X - playerTile.X) > VisionTiles || Math.Abs(myTile.Y - playerTile.Y) > VisionTiles)
            return false;

        return Utility.doesPointHaveLineOfSightInMine(currentLocation, Tile, target.Tile, VisionTiles);
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
        var px = (int)Position.X % 64;
        var py = (int)Position.Y % 64;
        return Math.Abs(px - 32) <= 4 && Math.Abs(py - 32) <= 4;
    }

    private void WalkStraight()
    {
        IsWalkingTowardPlayer = false;

        if (Game1.IsMasterGame && IsBlockedAhead())
            ChooseNewDirection();

        SetMoveDirection(_walkDir.Value);
    }

    private bool IsBlockedAhead()
    {
        var tile = TilePoint;
        var next = tile;

        switch (_walkDir.Value)
        {
            case 0:
                next.Y -= 1;
                break;
            case 1:
                next.X += 1;
                break;
            case 2:
                next.Y += 1;
                break;
            case 3:
                next.X -= 1;
                break;
        }

        return !currentLocation.isTilePassable(new Vector2(next.X, next.Y));
    }

    private void ChooseNewDirection()
    {
        var old = _walkDir.Value;
        var options = new[] { 0, 1, 2, 3 };

        for (var i = 0; i < 8; i++)
        {
            var candidate = options[Game1.random.Next(options.Length)];
            if (candidate == old)
                continue;

            _walkDir.Value = candidate;

            if (!IsBlockedAhead())
                return;
        }

        _walkDir.Value = (old + 2) % 4;
    }

    private void FireSporeShot()
    {
        var dir = _walkDir.Value;
        var v = dir switch
        {
            0 => new Vector2(0f, -1f),
            1 => new Vector2(1f, 0f),
            2 => new Vector2(0f, 1f),
            3 => new Vector2(-1f, 0f),
            _ => Vector2.Zero,
        };

        v.Normalize();
        v *= 12f;

        var projectile = new BasicProjectile(
            DamageToFarmer,
            13,
            0,
            1,
            (float)Math.PI / 32f,
            v.X,
            v.Y,
            Position,
            "hitEnemy",
            "debuffSpell",
            null,
            explode: false,
            damagesMonsters: false,
            currentLocation,
            this
        );

        currentLocation.projectiles.Add(projectile);
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
}
