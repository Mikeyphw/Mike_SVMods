using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.DesertTomb.Diagnostics;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Base;
using EggsInWild.Infrastructure.Config;

namespace EggsInWild.Infrastructure.DesertTomb.Monsters.Creatures;

/// <summary>
/// Wind spirit that moves normally until it locks into a short suction burst.
/// </summary>
internal sealed class Hurricane : Monster
{
    private enum HurricaneState
    {
        Normal = 0,
        Sucking = 1,
    }

    private static int AlignTiles => DebugKnobs.HurricaneAlignTiles;
    private static int SuckDistanceTiles => DebugKnobs.HurricaneSuckDistanceTiles;
    private static int SuckMs => DebugKnobs.HurricaneSuckMs;
    private static int FrameWidth => DebugKnobs.HurricaneFrameWidth;
    private static int FrameHeight => DebugKnobs.HurricaneFrameHeight;
    private static int SpinAnimStartFrame => DebugKnobs.HurricaneSpinAnimStartFrame;
    private static int SpinAnimFrames => DebugKnobs.HurricaneSpinAnimFrames;
    private static int SuckLoopStartFrame => DebugKnobs.HurricaneSuckLoopStartFrame;
    private static int SuckLoopFrames => DebugKnobs.HurricaneSuckLoopFrames;
    private static int SpinAnimIntervalMs => DebugKnobs.HurricaneSpinAnimIntervalMs;
    private static int SuckLoopIntervalMs => DebugKnobs.HurricaneSuckLoopIntervalMs;
    private static int DeathAnimStartFrame => DebugKnobs.HurricaneDeathAnimStartFrame;
    private static int DeathAnimFrames => DebugKnobs.HurricaneDeathAnimFrames;
    private static int DefaultFacingFrame => DebugKnobs.HurricaneDefaultFacingFrame;
    private static int FacingFrameStride => DebugKnobs.HurricaneFacingFrameStride;
    private static int SuckLaneToleranceTiles => DebugKnobs.HurricaneSuckLaneToleranceTiles;

    private readonly NetInt _state = new((int)HurricaneState.Normal);
    private readonly NetInt _stateTimerMs = new(0);
    private readonly NetString _spriteTexture = new();
    private readonly string _normalSpriteTexture;

    private HurricaneState? _previousState;
    private HurricaneState? _previousLoggedState;
    private bool _resumeSuckLoopAfterTransition;
    private bool _deathAnimationStarted;
    private bool _deathAnimationCompleted;

    internal Hurricane(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 3,
        string textureName = "Hurricane",
        int speedToAdd = 0,
        string? customDropData = null
    )
        : base("Hurricane", position)
    {
        var normalizedTextureName = textureName.Replace(" ", "");
        _normalSpriteTexture = $"Characters\\Monsters\\{normalizedTextureName}";
        _spriteTexture.Value = _normalSpriteTexture;

        MaxHealth = 160;
        Health = MaxHealth;
        DamageToFarmer = 18;
        resilience.Value = 4;
        Speed = 2;

        TombMonsterBuilder.SetupBasicMonster(
            this,
            multipliers,
            difficulty,
            textureName,
            speedToAdd,
            luckyBonusEnabled: true,
            customDropData: customDropData
        );

        TombMonsterBuilder.SetMonsterNames(this, "Hurricane");
        reloadSprite();
    }

    protected override void initNetFields()
    {
        base.initNetFields();
        NetFields
            .AddField(_state, nameof(_state))
            .AddField(_stateTimerMs, nameof(_stateTimerMs))
            .AddField(_spriteTexture, nameof(_spriteTexture));
    }

    public override void reloadSprite(bool onlyAppearance = false)
    {
        Sprite = new AnimatedSprite(_spriteTexture.Value, 0, FrameWidth, FrameHeight);
        Sprite.SpriteHeight = FrameHeight;
        Sprite.UpdateSourceRect();
    }

    public override void behaviorAtGameTick(GameTime time)
    {
        base.behaviorAtGameTick(time);

        if (Health <= 0)
        {
            StartDeathAnimationIfNeeded();
            return;
        }

        HandleStateVisualTransitions();

        var elapsedMs = (int)Math.Max(0, time.ElapsedGameTime.TotalMilliseconds);

        if (_stateTimerMs.Value > 0)
            _stateTimerMs.Value = Math.Max(0, _stateTimerMs.Value - elapsedMs);

        LogStateIfChanged();
        CustomMonsterDebug.HurricaneHeartbeat(this, "tick", $"heartbeat state={(HurricaneState)_state.Value} timerMs={_stateTimerMs.Value} moving={isMoving()} suckingRangeTiles={GetEffectiveSuctionRangeTiles()}");

        switch ((HurricaneState)_state.Value)
        {
            case HurricaneState.Sucking:
                if (_stateTimerMs.Value <= 0)
                {
                    CustomMonsterDebug.Hurricane(this, "suck finished -> normal");
                    _state.Value = (int)HurricaneState.Normal;
                    return;
                }

                IsWalkingTowardPlayer = false;
                Halt();

                if (Game1.IsMasterGame && ShouldApplySuction())
                {
                    CustomMonsterDebug.HurricaneHeartbeat(this, "apply_suction", "applying suction", 10);
                    ApplySuction();
                }
                else if (Game1.IsMasterGame)
                {
                    CustomMonsterDebug.HurricaneHeartbeat(this, "suction_no_target", "sucking but target outside effective range", 15);
                }

                return;

            case HurricaneState.Normal:
            default:
                break;
        }

        IsWalkingTowardPlayer = true;

        if (Game1.IsMasterGame)
        {
            if (TryEnterSuck())
                return;
        }
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
        if (Health <= 0)
        {
            StartDeathAnimationIfNeeded();
            return;
        }

        if (Sprite.CurrentAnimation is not null)
            return;

        if ((HurricaneState)_state.Value == HurricaneState.Sucking)
        {
            StartSuckLoop();
            return;
        }
        
        if (!isMoving())
        {
            Sprite.StopAnimation();
            ShowFacingFrame();
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

    private void ApplySuction()
    {
        var target = Player;
        if (target is null)
            return;

        var toMonster = Position - target.Position;
        if (toMonster.LengthSquared() < 1f)
            return;

        toMonster.Normalize();
        var pull = toMonster * 2.0f;

        // Movement is host authoritative; clients will receive corrected positions.
        target.Position += pull;
    }

    private bool ShouldApplySuction()
    {
        if ((HurricaneState)_state.Value != HurricaneState.Sucking)
            return false;

        var target = Player;
        if (target is null)
            return false;

        var myTile = TilePoint;
        var playerTile = target.TilePoint;
        return Math.Max(Math.Abs(myTile.X - playerTile.X), Math.Abs(myTile.Y - playerTile.Y)) <= GetEffectiveSuctionRangeTiles();
    }

    private static int GetEffectiveSuctionRangeTiles()
        => Math.Max(1, SuckDistanceTiles - 1);

    private bool TryEnterSuck()
    {
        var target = Player;
        if (target is null)
        {
            CustomMonsterDebug.HurricaneHeartbeat(this, "suck_blocked_no_target", "suck blocked: no target", 30);
            return false;
        }

        var myTile = TilePoint;
        var playerTile = target.TilePoint;
        var deltaX = playerTile.X - myTile.X;
        var deltaY = playerTile.Y - myTile.Y;
        var absDx = Math.Abs(deltaX);
        var absDy = Math.Abs(deltaY);

        var nearAligned = absDx <= SuckLaneToleranceTiles || absDy <= SuckLaneToleranceTiles;
        if (!nearAligned)
        {
            CustomMonsterDebug.HurricaneHeartbeat(this, "suck_blocked_alignment", $"suck blocked: not near-aligned targetTile=({playerTile.X},{playerTile.Y}) dx={deltaX} dy={deltaY}", 20);
            return false;
        }

        if (Math.Max(absDx, absDy) > AlignTiles)
        {
            CustomMonsterDebug.HurricaneHeartbeat(this, "suck_blocked_distance", $"suck blocked: outside alignTiles={AlignTiles} targetTile=({playerTile.X},{playerTile.Y}) dx={deltaX} dy={deltaY}", 20);
            return false;
        }

        if (!Utility.doesPointHaveLineOfSightInMine(currentLocation, Tile, target.Tile, AlignTiles))
        {
            CustomMonsterDebug.HurricaneHeartbeat(this, "suck_blocked_los", $"suck blocked: no line of sight targetTile=({playerTile.X},{playerTile.Y})", 20);
            return false;
        }

        FaceTargetByDominantAxis(target);
        _state.Value = (int)HurricaneState.Sucking;
        _stateTimerMs.Value = SuckMs;
        currentLocation.playSound("wind");
        CustomMonsterDebug.Hurricane(this, $"start sucking targetTile=({target.TilePoint.X},{target.TilePoint.Y}) dx={deltaX} dy={deltaY} durationMs={SuckMs}");
        return true;
    }

    private void HandleStateVisualTransitions()
    {
        var currentState = (HurricaneState)_state.Value;
        if (_previousState is null)
        {
            _previousState = currentState;
            EnsureTexture(_normalSpriteTexture);
            if (currentState == HurricaneState.Sucking)
                StartSuckLoop();
            return;
        }

        if (_previousState.Value == currentState)
            return;

        if (_previousState.Value == HurricaneState.Sucking && currentState != HurricaneState.Sucking)
        {
            EnsureTexture(_normalSpriteTexture);
            _resumeSuckLoopAfterTransition = false;
            Sprite.setCurrentAnimation(BuildSpinTransitionAnimation(reverse: true));
            Sprite.loop = false;
        }
        else if (_previousState.Value != HurricaneState.Sucking && currentState == HurricaneState.Sucking)
        {
            EnsureTexture(_normalSpriteTexture);
            _resumeSuckLoopAfterTransition = true;
            Sprite.setCurrentAnimation(BuildSpinTransitionAnimation(reverse: false));
            Sprite.loop = false;
        }
        else
        {
            EnsureTexture(_normalSpriteTexture);
            Sprite.StopAnimation();
            Sprite.CurrentAnimation = null;
        }

        _previousState = currentState;
    }

    private List<FarmerSprite.AnimationFrame> BuildSpinTransitionAnimation(bool reverse)
    {
        var frames = new List<FarmerSprite.AnimationFrame>();

        if (!reverse)
        {
            for (var i = 0; i < SpinAnimFrames; i++)
                frames.Add(new FarmerSprite.AnimationFrame(SpinAnimStartFrame + i, SpinAnimIntervalMs));

            frames.Add(new FarmerSprite.AnimationFrame(
                SpinAnimStartFrame + SpinAnimFrames - 1,
                SpinAnimIntervalMs,
                0,
                false,
                false,
                OnSpinTransitionComplete));
        }
        else
        {
            for (var i = SpinAnimFrames - 1; i >= 0; i--)
                frames.Add(new FarmerSprite.AnimationFrame(SpinAnimStartFrame + i, SpinAnimIntervalMs));

            frames.Add(new FarmerSprite.AnimationFrame(
                SpinAnimStartFrame,
                SpinAnimIntervalMs,
                0,
                false,
                false,
                OnSpinTransitionComplete));
        }

        return frames;
    }

    private void OnSpinTransitionComplete(Farmer who)
    {
        Sprite.CurrentAnimation = null;
        if (_resumeSuckLoopAfterTransition)
        {
            _resumeSuckLoopAfterTransition = false;
            CustomMonsterDebug.Hurricane(this, "spin transition complete -> suck loop");
            StartSuckLoop();
            return;
        }

        CustomMonsterDebug.Hurricane(this, "reverse spin transition complete -> normal");
        ShowFacingFrame();
    }

    private void EnsureTexture(string texturePath)
    {
        if (string.Equals(_spriteTexture.Value, texturePath, StringComparison.OrdinalIgnoreCase))
            return;

        _spriteTexture.Value = texturePath;
        reloadSprite();
    }

    private void StartSuckLoop()
    {
        if (Sprite.CurrentAnimation is not null)
            return;

        Sprite.loop = true;
        Sprite.setCurrentAnimation(BuildSuckLoopAnimation());
        CustomMonsterDebug.Hurricane(this, "start suck loop");
    }

    private List<FarmerSprite.AnimationFrame> BuildSuckLoopAnimation()
    {
        return new List<FarmerSprite.AnimationFrame>
        {
            new FarmerSprite.AnimationFrame(SuckLoopStartFrame, SuckLoopIntervalMs),
            new FarmerSprite.AnimationFrame(SuckLoopStartFrame + 1, SuckLoopIntervalMs),
        };
    }

    private void FaceTargetByDominantAxis(Farmer target)
    {
        var myTile = TilePoint;
        var playerTile = target.TilePoint;
        var deltaX = playerTile.X - myTile.X;
        var deltaY = playerTile.Y - myTile.Y;

        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
            FacingDirection = deltaX >= 0 ? 1 : 3;
        else
            FacingDirection = deltaY >= 0 ? 2 : 0;
    }

    private void ShowFacingFrame()
    {
        Sprite.StopAnimation();
        Sprite.CurrentAnimation = null;
        Sprite.currentFrame = GetFacingFrame();
        Sprite.UpdateSourceRect();
    }

    private int GetFacingFrame()
    {
        return FacingDirection switch
        {
            0 => 0,
            1 => FacingFrameStride,
            2 => DefaultFacingFrame,
            3 => FacingFrameStride * 3,
            _ => DefaultFacingFrame,
        };
    }

    private void StartDeathAnimationIfNeeded()
    {
        if (_deathAnimationCompleted)
            return;

        EnsureTexture(_normalSpriteTexture);
        if (_deathAnimationStarted)
            return;

        _deathAnimationStarted = true;
        IsWalkingTowardPlayer = false;
        Halt();
        Sprite.StopAnimation();
        Sprite.loop = false;
        Sprite.setCurrentAnimation(BuildDeathAnimation());
    }

    private List<FarmerSprite.AnimationFrame> BuildDeathAnimation()
    {
        var frames = new List<FarmerSprite.AnimationFrame>(DeathAnimFrames);

        for (var i = 0; i < DeathAnimFrames - 1; i++)
            frames.Add(new FarmerSprite.AnimationFrame(DeathAnimStartFrame + i, SpinAnimIntervalMs));

        frames.Add(new FarmerSprite.AnimationFrame(
            DeathAnimStartFrame + DeathAnimFrames - 1,
            SpinAnimIntervalMs,
            0,
            false,
            false,
            OnDeathAnimationComplete));

        return frames;
    }

    private void OnDeathAnimationComplete(Farmer who)
    {
        _deathAnimationCompleted = true;
        EnsureTexture(_normalSpriteTexture);
        Sprite.CurrentAnimation = null;
        Sprite.currentFrame = DeathAnimStartFrame + DeathAnimFrames - 1;
        Sprite.UpdateSourceRect();
        CustomMonsterDebug.Hurricane(this, "death animation complete");
    }

    private void LogStateIfChanged()
    {
        var currentState = (HurricaneState)_state.Value;
        if (_previousLoggedState == currentState)
            return;

        CustomMonsterDebug.Hurricane(this, $"state -> {currentState} timerMs={_stateTimerMs.Value}");
        _previousLoggedState = currentState;
    }
}
