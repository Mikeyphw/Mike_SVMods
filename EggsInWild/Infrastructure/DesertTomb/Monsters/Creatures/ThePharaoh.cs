using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Pathfinding;
using StardewValley.Projectiles;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.DesertTomb.Diagnostics;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Base;
using EggsInWild.Infrastructure.Config;

namespace EggsInWild.Infrastructure.DesertTomb.Monsters.Creatures;

/// <summary>
/// Pharaoh boss: a Mummy revive chassis with Shadow Shaman-style casting decisions.
/// Host authoritative for attacks and state changes; visuals are networked via net fields.
/// </summary>
internal sealed class ThePharaoh : Mummy
{
    private const int FrameWidth = 16;
    private const int FrameHeight = 24;

    private enum PharaohState
    {
        Dormant = 0,
        Chase = 1,
        Casting = 2,
        Recover = 3,
    }

    private enum TeleportPhase
    {
        None = 0,
        Vanishing = 1,
        Reappearing = 2,
    }

    private static int VisionTiles => DebugKnobs.PharaohVisionTiles;
    private static int CastRangeTiles => DebugKnobs.PharaohCastRangeTiles;
    private static int RewrapRangeTiles => DebugKnobs.PharaohRewrapRangeTiles;

    private static int BaseCastCooldownMs => DebugKnobs.PharaohBaseCastCooldownMs;
    private static int EnragedCastCooldownMs => DebugKnobs.PharaohEnragedCastCooldownMs;
    private static int CastWindupMs => DebugKnobs.PharaohCastWindupMs;
    private static int RecoverMs => DebugKnobs.PharaohRecoverMs;

    private static int MicroUnstuckAfterTicks => DebugKnobs.PharaohMicroUnstuckAfterTicks;
    private static int PathfindUnstuckAfterTicks => DebugKnobs.PharaohPathfindUnstuckAfterTicks;
    private static int SandStepAfterTicks => DebugKnobs.PharaohSandStepAfterTicks;
    private static int SandStepCooldownMs => DebugKnobs.PharaohSandStepCooldownMs;

    private static int LeashRadiusTiles => DebugKnobs.PharaohLeashRadiusTiles;
    private static int LeashTicksBeforeReturn => DebugKnobs.PharaohLeashTicksBeforeReturn;
    private static int TeleportAnimStartFrame => DebugKnobs.PharaohTeleportAnimStartFrame;
    private static int TeleportAnimFrames => DebugKnobs.PharaohTeleportAnimFrames;
    private static int TeleportAnimIntervalMs => DebugKnobs.PharaohTeleportAnimIntervalMs;

    private readonly NetBool _spotted = new(false);
    private readonly NetBool _casting = new(false);
    private readonly NetBool _hasRevivedOnce = new(false);
    private readonly NetBool _enraged = new(false);

    private readonly NetInt _state = new((int)PharaohState.Dormant);
    private readonly NetInt _castWindupRemainingMs = new(0);
    private readonly NetInt _recoverRemainingMs = new(0);
    private readonly NetInt _castCooldownMs = new(BaseCastCooldownMs);
    private readonly NetInt _sandStepCooldownMs = new(0);
    private readonly NetInt _teleportPhase = new((int)TeleportPhase.None);
    private readonly NetInt _teleportTargetX = new(0);
    private readonly NetInt _teleportTargetY = new(0);

    private readonly NetString _spriteTexture = new();
    private readonly string _normalSpriteTexture;
    private readonly string _combatSpriteTexture;

    private float _rotationTimer;
    private Vector2 _lastPosition;
    private int _stuckTicks;
    private Point _homeTile;
    private int _outOfLeashTicks;
    private TeleportPhase? _previousTeleportPhase;
    private PharaohState? _previousLoggedState;

    internal ThePharaoh(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 5,
        string textureName = "ThePharaoh",
        int speedToAdd = 0,
        string? customDropData = null
    )
        : base(position)
    {
        _homeTile = new Point((int)(position.X / 64f), (int)(position.Y / 64f));
        _lastPosition = position;

        var normalizedTextureName = textureName.Replace(" ", "");
        _normalSpriteTexture = $"Characters\\Monsters\\{normalizedTextureName}";
        _combatSpriteTexture = $"Characters\\Monsters\\{normalizedTextureName}_combatmode";
        _spriteTexture.Value = _normalSpriteTexture;

        // Boss baseline stats (scaled further by multipliers + BuffForAdditionalDifficulty).
        MaxHealth = 1200;
        Health = MaxHealth;
        DamageToFarmer = 26;
        resilience.Value = 6;
        moveTowardPlayerThreshold.Value = VisionTiles;
        ExperienceGained = 50;

        TombMonsterBuilder.SetupBasicMonster(
            this,
            multipliers,
            difficulty,
            textureName,
            speedToAdd,
            luckyBonusEnabled: true,
            customDropData: customDropData
        );

        TombMonsterBuilder.SetMonsterNames(this, "Pharaoh");
        reloadSprite();
    }

    protected override void initNetFields()
    {
        base.initNetFields();
        NetFields
            .AddField(_spotted, nameof(_spotted))
            .AddField(_casting, nameof(_casting))
            .AddField(_hasRevivedOnce, nameof(_hasRevivedOnce))
            .AddField(_enraged, nameof(_enraged))
            .AddField(_state, nameof(_state))
            .AddField(_castWindupRemainingMs, nameof(_castWindupRemainingMs))
            .AddField(_recoverRemainingMs, nameof(_recoverRemainingMs))
            .AddField(_castCooldownMs, nameof(_castCooldownMs))
            .AddField(_sandStepCooldownMs, nameof(_sandStepCooldownMs))
            .AddField(_teleportPhase, nameof(_teleportPhase))
            .AddField(_teleportTargetX, nameof(_teleportTargetX))
            .AddField(_teleportTargetY, nameof(_teleportTargetY))
            .AddField(_spriteTexture, nameof(_spriteTexture));
    }

    public override void reloadSprite(bool onlyAppearance = false)
    {
        Sprite = new AnimatedSprite(_spriteTexture.Value, 0, FrameWidth, FrameHeight);
        Sprite.SpriteHeight = FrameHeight;
        Sprite.UpdateSourceRect();
        Sprite.ignoreStopAnimation = true;
    }

    public override void draw(SpriteBatch b)
    {
        base.draw(b);

        if (!_casting.Value)
            return;

        for (var i = 0; i < 8; i++)
        {
            b.Draw(
                Projectile.projectileSheet,
                Game1.GlobalToLocal(Game1.viewport, getStandingPosition()),
                new Rectangle(119, 6, 3, 3),
                Color.White * 0.7f,
                _rotationTimer + i * (float)Math.PI / 4f,
                new Vector2(8f, 48f),
                6f,
                SpriteEffects.None,
                0.95f
            );
        }
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
        if (reviveTimer.Value > 0)
            return isBomb ? base.takeDamage(damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who) : -1;

        var actualDamage = Math.Max(1, damage - resilience.Value);

        if (Game1.random.NextDouble() < missChance.Value - missChance.Value * addedPrecision)
            return -1;

        Slipperiness = 2;
        Health -= actualDamage;
        setTrajectory(xTrajectory, yTrajectory);
        currentLocation.playSound("shadowHit");

        if (_casting.Value && Game1.random.NextBool())
            _castCooldownMs.Value += 250;

        if (Health > 0)
            return actualDamage;

        // One guaranteed revive; final death after that.
        if (isBomb || (who?.CurrentTool is StardewValley.Tools.MeleeWeapon weapon
                && weapon.hasEnchantmentOfType<StardewValley.Enchantments.CrusaderEnchantment>()))
        {
            Health = 0;
            deathAnimation();
            return actualDamage;
        }

        if (!_hasRevivedOnce.Value)
        {
            _hasRevivedOnce.Value = true;
            reviveTimer.Value = Mummy.revivalTime;
            Health = MaxHealth;

            _casting.Value = false;
            _state.Value = (int)PharaohState.Dormant;
            deathAnimation();
            return actualDamage;
        }

        Health = 0;
        deathAnimation();
        return actualDamage;
    }

    public override void behaviorAtGameTick(GameTime time)
    {
        base.behaviorAtGameTick(time);
        EnsureSpriteTextureForCurrentState();

        var elapsedMs = (int)Math.Max(0, time.ElapsedGameTime.TotalMilliseconds);

        TickTimers(elapsedMs);
        LogStateIfChanged();
        CustomMonsterDebug.PharaohHeartbeat(this, "tick", $"heartbeat state={(PharaohState)_state.Value} spotted={_spotted.Value} casting={_casting.Value} enraged={_enraged.Value} castCooldownMs={_castCooldownMs.Value} recoverMs={_recoverRemainingMs.Value} stuckTicks={_stuckTicks} leashTicks={_outOfLeashTicks}");

        if (reviveTimer.Value > 0)
        {
            CustomMonsterDebug.PharaohHeartbeat(this, "reviving", $"reviving reviveTimer={reviveTimer.Value}", 20);
            return;
        }

        HandleTeleportVisualTransitions();

        if ((TeleportPhase)_teleportPhase.Value != TeleportPhase.None)
        {
            IsWalkingTowardPlayer = false;
            Halt();
            return;
        }

        if (_hasRevivedOnce.Value && !_enraged.Value)
            TryEnterEnrage();

        var target = GetTargetPlayer();
        if (target is null)
        {
            CustomMonsterDebug.PharaohHeartbeat(this, "no_target", "no target player found", 30);
            defaultMovementBehavior(time);
            return;
        }

        if (!_spotted.Value)
        {
            if (!CanSpotPlayer(target))
            {
                CustomMonsterDebug.PharaohHeartbeat(this, "spot_blocked", $"cannot spot targetTile=({target.TilePoint.X},{target.TilePoint.Y})", 20);
                defaultMovementBehavior(time);
                return;
            }

            _spotted.Value = true;
            _state.Value = (int)PharaohState.Chase;
            controller = null;
            Halt();
            facePlayer(target);
            CustomMonsterDebug.Pharaoh(this, $"spotted player targetTile=({target.TilePoint.X},{target.TilePoint.Y})");

            if (Game1.random.NextDouble() < 0.3)
                currentLocation.playSound("shadowpeep");
        }

        if (HandleLeash(target))
            return;

        UpdateStuckTracking(target);

        if (_casting.Value || (PharaohState)_state.Value == PharaohState.Casting)
        {
            CustomMonsterDebug.PharaohHeartbeat(this, "casting_tick", $"casting tick windupMs={_castWindupRemainingMs.Value}", 10);
            TickCasting(time, elapsedMs, target);
            return;
        }

        if ((PharaohState)_state.Value == PharaohState.Recover)
        {
            IsWalkingTowardPlayer = false;
            Halt();
            CustomMonsterDebug.PharaohHeartbeat(this, "recover_tick", $"recover tick remainingMs={_recoverRemainingMs.Value}", 10);
            return;
        }

        if (TryStartCasting(target))
            return;

        HandleMovement(time, target);
    }

    protected override void updateAnimation(GameTime time)
    {
        UpdateVisualAnimation(time);
        resetAnimationSpeed();
    }

    protected override void updateMonsterSlaveAnimation(GameTime time)
        => UpdateVisualAnimation(time);

    private void UpdateVisualAnimation(GameTime time)
    {
        if ((TeleportPhase)_teleportPhase.Value != TeleportPhase.None)
            return;

        if (_casting.Value)
        {
            Sprite.Animate(time, 16, 4, 200f);
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

    private void TickTimers(int elapsedMs)
    {
        if (_castCooldownMs.Value > 0)
            _castCooldownMs.Value = Math.Max(0, _castCooldownMs.Value - elapsedMs);

        if (_sandStepCooldownMs.Value > 0)
            _sandStepCooldownMs.Value = Math.Max(0, _sandStepCooldownMs.Value - elapsedMs);

        if (_recoverRemainingMs.Value > 0)
        {
            _recoverRemainingMs.Value = Math.Max(0, _recoverRemainingMs.Value - elapsedMs);
            if (_recoverRemainingMs.Value == 0)
            {
                _state.Value = (int)PharaohState.Chase;
                IsWalkingTowardPlayer = true;
                EnsureSpriteTextureForCurrentState();
            }
        }

        if (_castWindupRemainingMs.Value > 0)
            _castWindupRemainingMs.Value = Math.Max(0, _castWindupRemainingMs.Value - elapsedMs);
    }

    private void TryEnterEnrage()
    {
        _enraged.Value = true;
        _castCooldownMs.Value = Math.Min(_castCooldownMs.Value, DebugKnobs.PharaohEnrageCooldownCapMs);
        currentLocation?.playSound("furnace");
        EnsureSpriteTextureForCurrentState();
        CustomMonsterDebug.Pharaoh(this, $"enraged castCooldownMs={_castCooldownMs.Value}");
    }

    private Farmer? GetTargetPlayer()
    {
        if (Player is not null)
            return Player;

        if (currentLocation is null)
            return null;

        Farmer? best = null;
        var bestDist = float.MaxValue;

        foreach (var farmer in currentLocation.farmers)
        {
            if (farmer is null || farmer.hidden.Value)
                continue;

            var dist = Vector2.DistanceSquared(farmer.Position, Position);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = farmer;
            }
        }

        return best ?? Game1.player;
    }

    private bool CanSpotPlayer(Farmer player)
    {
        if (player is null)
            return false;

        return Utility.couldSeePlayerInPeripheralVision(player, this)
               && Utility.doesPointHaveLineOfSightInMine(currentLocation, Tile, player.Tile, VisionTiles);
    }

    private bool TryStartCasting(Farmer target)
    {
        if (!Game1.IsMasterGame)
            return false;

        if (_castCooldownMs.Value > 0)
        {
            CustomMonsterDebug.PharaohHeartbeat(this, "cast_blocked_cooldown", $"cast blocked: cooldownMs={_castCooldownMs.Value}", 20);
            return false;
        }

        if (!withinPlayerThreshold(CastRangeTiles))
        {
            CustomMonsterDebug.PharaohHeartbeat(this, "cast_blocked_range", $"cast blocked: out of range castRangeTiles={CastRangeTiles}", 20);
            return false;
        }

        if (!Utility.doesPointHaveLineOfSightInMine(currentLocation, Tile, target.Tile, VisionTiles))
        {
            CustomMonsterDebug.PharaohHeartbeat(this, "cast_blocked_los", $"cast blocked: no line of sight targetTile=({target.TilePoint.X},{target.TilePoint.Y})", 20);
            return false;
        }

        if (Game1.random.NextDouble() > 0.12)
        {
            CustomMonsterDebug.PharaohHeartbeat(this, "cast_blocked_rng", "cast blocked: rng gate failed", 20);
            return false;
        }

        _casting.Value = true;
        _state.Value = (int)PharaohState.Casting;
        controller = null;
        IsWalkingTowardPlayer = false;
        Halt();
        facePlayer(target);
        EnsureSpriteTextureForCurrentState();

        _castWindupRemainingMs.Value = CastWindupMs;
        _recoverRemainingMs.Value = 0;

        currentLocation.playSound("shadowpeep");
        CustomMonsterDebug.Pharaoh(this, $"start casting targetTile=({target.TilePoint.X},{target.TilePoint.Y}) windupMs={CastWindupMs}");
        return true;
    }

    private void TickCasting(GameTime time, int elapsedMs, Farmer target)
    {
        IsWalkingTowardPlayer = false;
        Halt();

        _rotationTimer = (float)(time.TotalGameTime.TotalSeconds * Math.PI * 0.6);

        if (_castWindupRemainingMs.Value > 0)
            return;

        if (Game1.IsMasterGame)
            PerformCastAction(target);

        _casting.Value = false;
        _state.Value = (int)PharaohState.Recover;
        EnsureSpriteTextureForCurrentState();

        _recoverRemainingMs.Value = RecoverMs;
        _castCooldownMs.Value = _enraged.Value ? EnragedCastCooldownMs : BaseCastCooldownMs;
        IsWalkingTowardPlayer = true;
        CustomMonsterDebug.Pharaoh(this, $"cast complete -> recover recoverMs={RecoverMs} nextCooldownMs={_castCooldownMs.Value}");
    }

    private void PerformCastAction(Farmer target)
    {
        if (TryRoyalRewrap(target))
            return;

        CustomMonsterDebug.Pharaoh(this, "cast action -> bandage curse");
        CastBandageCurse(target);
    }

    private bool TryRoyalRewrap(Farmer target)
    {
        if (Game1.random.NextDouble() > DebugKnobs.PharaohRewrapChance)
            return false;

        Monster? best = null;
        double bestScore = double.MinValue;

        foreach (var character in currentLocation.characters)
        {
            if (character is not Mummy mummy || mummy == this)
                continue;

            if (!IsWithinTiles(mummy.Tile, Tile, RewrapRangeTiles))
                continue;

            var score = mummy.reviveTimer.Value > 0 ? 1000d : 0d;
            score += 1.0 - (double)mummy.Health / Math.Max(1, mummy.MaxHealth);

            if (score > bestScore)
            {
                bestScore = score;
                best = mummy;
            }
        }

        if (best is not Mummy targetMummy)
            return false;

        if (targetMummy.reviveTimer.Value > 0)
        {
            targetMummy.reviveTimer.Value = Math.Min(targetMummy.reviveTimer.Value, DebugKnobs.PharaohReviveTimerMax);
            CustomMonsterDebug.Pharaoh(this, $"rewrap revived mummy targetTile=({targetMummy.TilePoint.X},{targetMummy.TilePoint.Y}) reviveTimer={targetMummy.reviveTimer.Value}");
        }
        else
        {
            targetMummy.Health = Math.Min(targetMummy.MaxHealth, targetMummy.Health + (_enraged.Value ? DebugKnobs.PharaohHealEnraged : DebugKnobs.PharaohHealNormal));
            CustomMonsterDebug.Pharaoh(this, $"rewrap healed mummy targetTile=({targetMummy.TilePoint.X},{targetMummy.TilePoint.Y}) targetHp={targetMummy.Health}/{targetMummy.MaxHealth}");
        }

        currentLocation.playSound("healSound");
        Game1.Multiplayer.broadcastSprites(
            currentLocation,
            new TemporaryAnimatedSprite(
                "TileSheets\\animations",
                new Rectangle(0, 256, 64, 64),
                40f,
                8,
                0,
                targetMummy.Position + new Vector2(32f, 64f),
                flicker: false,
                flipped: false
            )
        );

        return true;
    }

    private void CastBandageCurse(Farmer target)
    {
        var monsterBounds = GetBoundingBox();
        var v = Utility.getVelocityTowardPlayer(monsterBounds.Center, 15f, target);

        currentLocation.projectiles.Add(
            new DebuffingProjectile(
                "14",
                7,
                4,
                4,
                (float)Math.PI / 16f,
                v.X,
                v.Y,
                new Vector2(monsterBounds.X, monsterBounds.Y),
                currentLocation,
                this
            )
        );
    }

    private void HandleMovement(GameTime time, Farmer target)
    {
        IsWalkingTowardPlayer = true;

        if (controller is null && !Utility.doesPointHaveLineOfSightInMine(currentLocation, Tile, target.Tile, VisionTiles))
        {
            CustomMonsterDebug.PharaohHeartbeat(this, "pathfind_start", $"starting pathfind targetTile=({target.TilePoint.X},{target.TilePoint.Y})", 20);
            controller = new PathFindController(this, currentLocation, target.TilePoint, -1, null, 300);
            if (controller?.pathToEndPoint is null || controller.pathToEndPoint.Count == 0)
            {
                CustomMonsterDebug.PharaohHeartbeat(this, "pathfind_fail", "pathfind failed or empty", 20);
                controller = null;
            }
        }

        if (_stuckTicks >= SandStepAfterTicks && _sandStepCooldownMs.Value <= 0)
        {
            CustomMonsterDebug.Pharaoh(this, $"sand step trigger stuckTicks={_stuckTicks}");
            TrySandStepAwayFrom(target);
            return;
        }

        if (_stuckTicks >= PathfindUnstuckAfterTicks)
        {
            CustomMonsterDebug.Pharaoh(this, $"pathfind unstuck trigger stuckTicks={_stuckTicks}");
            TryPathfindUnstuck(target);
            return;
        }

        if (_stuckTicks >= MicroUnstuckAfterTicks)
        {
            CustomMonsterDebug.PharaohHeartbeat(this, "micro_unstuck", $"micro unstuck trigger stuckTicks={_stuckTicks}", 15);
            TryMicroUnstuck(target);
        }
    }

    private void UpdateStuckTracking(Farmer target)
    {
        var moved = Vector2.DistanceSquared(Position, _lastPosition) > 0.5f;

        if (!moved && IsWalkingTowardPlayer)
            _stuckTicks++;
        else if (moved)
            _stuckTicks = 0;

        _lastPosition = Position;
    }

    private void TryMicroUnstuck(Farmer target)
    {
        if (!Game1.IsMasterGame)
            return;

        var bestDir = GetBestEscapeDirection(target);
        if (bestDir is null)
            return;

        controller = null;
        IsWalkingTowardPlayer = false;
        Halt();
        SetMoveDirection(bestDir.Value);
    }

    private void TryPathfindUnstuck(Farmer target)
    {
        if (!Game1.IsMasterGame)
            return;

        var escapeTile = FindEscapeTile(target, radiusTiles: 6);
        if (escapeTile is null)
            return;

        controller = new PathFindController(this, currentLocation, escapeTile.Value, -1, null, 300);
        if (controller?.pathToEndPoint is null || controller.pathToEndPoint.Count == 0)
            controller = null;
    }

    private void TrySandStepAwayFrom(Farmer target)
    {
        if (!Game1.IsMasterGame)
            return;

        if (_sandStepCooldownMs.Value > 0)
            return;

        var destination = FindEscapeTile(target, radiusTiles: 8);
        if (destination is null)
            return;

        StartTeleport(destination.Value, consumeSandStepCooldown: true);
        CustomMonsterDebug.Pharaoh(this, $"sand step targetTile=({destination.Value.X},{destination.Value.Y})");
    }

    private bool HandleLeash(Farmer target)
    {
        if (!Game1.IsMasterGame)
            return false;

        var tile = TilePoint;
        var dx = tile.X - _homeTile.X;
        var dy = tile.Y - _homeTile.Y;

        if (Math.Abs(dx) > LeashRadiusTiles || Math.Abs(dy) > LeashRadiusTiles)
            _outOfLeashTicks++;
        else
            _outOfLeashTicks = 0;

        if (_outOfLeashTicks < LeashTicksBeforeReturn)
            return false;

        _outOfLeashTicks = 0;

        var destination = FindLeashReturnTile();
        StartTeleport(destination, consumeSandStepCooldown: false);
        CustomMonsterDebug.Pharaoh(this, $"leash return targetTile=({destination.X},{destination.Y})");
        return true;
    }

    private Point FindLeashReturnTile()
    {
        var center = _homeTile;

        for (var i = 0; i < 30; i++)
        {
            var candidate = new Point(
                center.X + Game1.random.Next(-3, 4),
                center.Y + Game1.random.Next(-3, 4)
            );

            if (currentLocation.isTilePassable(new Vector2(candidate.X, candidate.Y)))
                return candidate;
        }

        return center;
    }

    private Point? GetBestEscapeDirection(Farmer target)
    {
        var tile = TilePoint;
        var candidates = new[]
        {
            new Point(0, -1), // up
            new Point(1, 0),  // right
            new Point(0, 1),  // down
            new Point(-1, 0), // left
        };

        double bestScore = double.MinValue;
        Point? best = null;

        foreach (var dir in candidates)
        {
            var candidate = new Point(tile.X + dir.X, tile.Y + dir.Y);
            if (!currentLocation.isTilePassable(new Vector2(candidate.X, candidate.Y)))
                continue;

            var passableNeighbors = CountPassableNeighbors(candidate);
            var distToPlayer = DistanceTiles(candidate, target.TilePoint);

            var score = passableNeighbors * 10 + distToPlayer;
            if (score > bestScore)
            {
                bestScore = score;
                best = dir;
            }
        }

        return best;
    }

    private Point? FindEscapeTile(Farmer target, int radiusTiles)
    {
        var origin = TilePoint;
        var playerTile = target.TilePoint;

        Point? best = null;
        double bestScore = double.MinValue;

        for (var i = 0; i < 24; i++)
        {
            var candidate = new Point(
                origin.X + Game1.random.Next(-radiusTiles, radiusTiles + 1),
                origin.Y + Game1.random.Next(-radiusTiles, radiusTiles + 1)
            );

            if (!currentLocation.isTilePassable(new Vector2(candidate.X, candidate.Y)))
                continue;

            var distToPlayer = DistanceTiles(candidate, playerTile);
            if (distToPlayer < 3)
                continue;

            var score = distToPlayer + CountPassableNeighbors(candidate) * 2;
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private static bool IsWithinTiles(Vector2 a, Vector2 b, int tiles)
        => Vector2.DistanceSquared(a, b) <= tiles * tiles;

    private static double DistanceTiles(Point a, Point b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private int CountPassableNeighbors(Point tile)
    {
        var offsets = new[]
        {
            new Point(0, -1),
            new Point(1, 0),
            new Point(0, 1),
            new Point(-1, 0),
        };

        var count = 0;
        foreach (var o in offsets)
        {
            var t = new Vector2(tile.X + o.X, tile.Y + o.Y);
            if (currentLocation.isTilePassable(t))
                count++;
        }

        return count;
    }

    private void SetMoveDirection(Point dir)
    {
        SetMovingUp(false);
        SetMovingDown(false);
        SetMovingLeft(false);
        SetMovingRight(false);

        if (dir.X > 0) SetMovingRight(true);
        else if (dir.X < 0) SetMovingLeft(true);
        else if (dir.Y > 0) SetMovingDown(true);
        else if (dir.Y < 0) SetMovingUp(true);
    }

    private void StartTeleport(Point destination, bool consumeSandStepCooldown)
    {
        if (!Game1.IsMasterGame)
            return;

        if ((TeleportPhase)_teleportPhase.Value != TeleportPhase.None)
            return;

        _teleportTargetX.Value = destination.X;
        _teleportTargetY.Value = destination.Y;
        _teleportPhase.Value = (int)TeleportPhase.Vanishing;

        if (consumeSandStepCooldown)
            _sandStepCooldownMs.Value = SandStepCooldownMs;

        _stuckTicks = 0;
        controller = null;
        IsWalkingTowardPlayer = false;
        Halt();
        currentLocation.playSound("wand");
        CustomMonsterDebug.Pharaoh(this, $"teleport start phase=Vanishing targetTile=({destination.X},{destination.Y}) consumeSandStepCooldown={consumeSandStepCooldown}");
    }

    private void HandleTeleportVisualTransitions()
    {
        var currentPhase = (TeleportPhase)_teleportPhase.Value;
        if (_previousTeleportPhase is null)
        {
            _previousTeleportPhase = currentPhase;
            if (currentPhase != TeleportPhase.None)
                Sprite.setCurrentAnimation(BuildTeleportAnimation(reverse: currentPhase == TeleportPhase.Reappearing));
            return;
        }

        if (_previousTeleportPhase.Value == currentPhase)
            return;

        if (currentPhase == TeleportPhase.None)
        {
            Sprite.CurrentAnimation = null;
            _previousTeleportPhase = currentPhase;
            return;
        }

        Sprite.setCurrentAnimation(BuildTeleportAnimation(reverse: currentPhase == TeleportPhase.Reappearing));
        Sprite.loop = false;
        _previousTeleportPhase = currentPhase;
        CustomMonsterDebug.Pharaoh(this, $"teleport phase -> {currentPhase}");
    }

    private List<FarmerSprite.AnimationFrame> BuildTeleportAnimation(bool reverse)
    {
        var frames = new List<FarmerSprite.AnimationFrame>(TeleportAnimFrames);

        if (!reverse)
        {
            for (var i = 0; i < TeleportAnimFrames - 1; i++)
                frames.Add(new FarmerSprite.AnimationFrame(TeleportAnimStartFrame + i, TeleportAnimIntervalMs));

            frames.Add(new FarmerSprite.AnimationFrame(
                TeleportAnimStartFrame + TeleportAnimFrames - 1,
                TeleportAnimIntervalMs,
                0,
                false,
                false,
                OnTeleportOutComplete));
            return frames;
        }

        for (var i = TeleportAnimFrames - 1; i > 0; i--)
            frames.Add(new FarmerSprite.AnimationFrame(TeleportAnimStartFrame + i, TeleportAnimIntervalMs));

        frames.Add(new FarmerSprite.AnimationFrame(
            TeleportAnimStartFrame,
            TeleportAnimIntervalMs,
            0,
            false,
            false,
            OnTeleportInComplete));
        return frames;
    }

    private void OnTeleportOutComplete(Farmer who)
    {
        Sprite.CurrentAnimation = null;
        if (!Game1.IsMasterGame || (TeleportPhase)_teleportPhase.Value != TeleportPhase.Vanishing)
            return;

        Position = new Vector2(_teleportTargetX.Value * 64f, _teleportTargetY.Value * 64f);
        _teleportPhase.Value = (int)TeleportPhase.Reappearing;
        CustomMonsterDebug.Pharaoh(this, $"teleport moved targetTile=({_teleportTargetX.Value},{_teleportTargetY.Value})");
    }

    private void OnTeleportInComplete(Farmer who)
    {
        Sprite.CurrentAnimation = null;
        if (Game1.IsMasterGame && (TeleportPhase)_teleportPhase.Value == TeleportPhase.Reappearing)
            _teleportPhase.Value = (int)TeleportPhase.None;

        EnsureSpriteTextureForCurrentState();
        CustomMonsterDebug.Pharaoh(this, "teleport complete");
    }

    private void EnsureSpriteTextureForCurrentState()
    {
        var desiredTexture = ShouldUseCombatSprite() ? _combatSpriteTexture : _normalSpriteTexture;
        if (string.Equals(_spriteTexture.Value, desiredTexture, StringComparison.OrdinalIgnoreCase))
            return;

        _spriteTexture.Value = desiredTexture;
        reloadSprite();
    }

    private bool ShouldUseCombatSprite()
        => _enraged.Value
           || _casting.Value
           || (PharaohState)_state.Value is PharaohState.Casting or PharaohState.Recover or PharaohState.Chase;

    private void LogStateIfChanged()
    {
        var currentState = (PharaohState)_state.Value;
        if (_previousLoggedState == currentState)
            return;

        CustomMonsterDebug.Pharaoh(this, $"state -> {currentState} casting={_casting.Value} enraged={_enraged.Value} castCooldownMs={_castCooldownMs.Value} recoverMs={_recoverRemainingMs.Value}");
        _previousLoggedState = currentState;
    }
}
