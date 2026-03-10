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
/// Slow pursuer that can "curl up" when close to a player, becoming immune for a short duration.
/// </summary>
internal sealed class Crawler : Monster
{
    private enum CrawlerState
    {
        Crawling = 0,
        Curled = 1,
    }

    private static int CurlTriggerDistanceTiles => DebugKnobs.CrawlerCurlTriggerDistanceTiles;
    private static int CurlAnimIntervalMs => DebugKnobs.CrawlerCurlAnimIntervalMs;
    private static int FrameWidth => DebugKnobs.CrawlerFrameWidth;
    private static int FrameHeight => DebugKnobs.CrawlerFrameHeight;
    private static int CurledSpeedBonus => DebugKnobs.CrawlerCurledSpeedBonus;
    private static int DeathFrame => DebugKnobs.CrawlerDeathFrame;
    private static int DeathFrameLoops => DebugKnobs.CrawlerDeathFrameLoops;
    private static int CurlHoldMs => DebugKnobs.CrawlerCurlHoldMs;
    private static int CurledHaltGraceMs => DebugKnobs.CrawlerCurledHaltGraceMs;
    private static float MovementDetectThresholdSquared => (float)DebugKnobs.CrawlerMovementDetectThresholdSquared;
    private static int CurlDurationMs => DebugKnobs.CrawlerCurlDurationMs;
    private static int CurlCooldownMs => DebugKnobs.CrawlerCurlCooldownMs;

    private readonly NetInt _state = new((int)CrawlerState.Crawling);
    private readonly NetInt _curlRemainingMs = new(0);
    private readonly NetInt _curlCooldownMs = new(0);
    private readonly NetString _spriteTexture = new();
    private readonly NetEvent1Field<bool, NetBool> _curlEvent = new();
    private readonly string _crawlingSpriteTexture;
    private readonly string _curledSpriteTexture;

    private CrawlerState? _previousState;
    private CrawlerState? _previousLoggedState;
    private bool _pendingCrawlingTextureAfterTransition;
    private bool _curledSpeedApplied;
    private bool _deathAnimationStarted;
    private bool _deathAnimationCompleted;
    private Vector2 _lastTickPosition;
    private int _curledHaltMs;

    internal Crawler(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 3,
        string textureName = "Crawler",
        int speedToAdd = 0,
        string? customDropData = null
    )
        : base("Crawler", position)
    {
        _lastTickPosition = position;

        var normalizedTextureName = textureName.Replace(" ", "");
        _crawlingSpriteTexture = $"Characters\\Monsters\\{normalizedTextureName}";
        _curledSpriteTexture = $"Characters\\Monsters\\{normalizedTextureName}_curled";
        _spriteTexture.Value = _crawlingSpriteTexture;

        MaxHealth = 140;
        Health = MaxHealth;
        DamageToFarmer = 18;
        resilience.Value = 3;
        Speed = 1;

        TombMonsterBuilder.SetupBasicMonster(
            this,
            multipliers,
            difficulty,
            textureName,
            speedToAdd,
            luckyBonusEnabled: true,
            customDropData: customDropData
        );

        TombMonsterBuilder.SetMonsterNames(this, "Crawler");
        reloadSprite();
    }

    protected override void initNetFields()
    {
        base.initNetFields();
        NetFields
            .AddField(_state, nameof(_state))
            .AddField(_curlRemainingMs, nameof(_curlRemainingMs))
            .AddField(_curlCooldownMs, nameof(_curlCooldownMs))
            .AddField(_spriteTexture, nameof(_spriteTexture))
            .AddField(_curlEvent, nameof(_curlEvent));

        _curlEvent.onEvent += PerformCurl;
    }

    public override void reloadSprite(bool onlyAppearance = false)
    {
        Sprite = new AnimatedSprite(_spriteTexture.Value, 0, FrameWidth, FrameHeight);
        Sprite.SpriteHeight = FrameHeight;
        Sprite.UpdateSourceRect();
    }

    public override void update(GameTime time, GameLocation location)
    {
        _curlEvent.Poll();
        base.update(time, location);
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
        return base.takeDamage(damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);
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
        var movedThisTick = Vector2.DistanceSquared(Position, _lastTickPosition) > MovementDetectThresholdSquared;
        _lastTickPosition = Position;

        LogStateIfChanged();
        CustomMonsterDebug.CrawlerHeartbeat(this, "tick", $"heartbeat state={(CrawlerState)_state.Value} curlRemainingMs={_curlRemainingMs.Value} cooldownMs={_curlCooldownMs.Value} moving={isMoving()} movedThisTick={movedThisTick} haltedMs={_curledHaltMs} spriteAnim={(Sprite.CurrentAnimation is not null)} speed={Speed}");

        if (_curlCooldownMs.Value > 0)
            _curlCooldownMs.Value = Math.Max(0, _curlCooldownMs.Value - elapsedMs);

        if ((CrawlerState)_state.Value == CrawlerState.Curled)
        {
            IsWalkingTowardPlayer = true;

            if (_curlRemainingMs.Value > 0)
                _curlRemainingMs.Value = Math.Max(0, _curlRemainingMs.Value - elapsedMs);

            if (movedThisTick)
                _curledHaltMs = 0;
            else
                _curledHaltMs += elapsedMs;

            if (Game1.IsMasterGame && Sprite.CurrentAnimation is null && _curledHaltMs >= CurledHaltGraceMs)
            {
                CustomMonsterDebug.Crawler(this, $"curled but halted -> forcing uncurl haltedMs={_curledHaltMs}");
                FireCurlEvent(reverse: true);
                return;
            }

            if (Game1.IsMasterGame && _curlRemainingMs.Value <= 0)
            {
                CustomMonsterDebug.Crawler(this, "curl duration expired -> uncurl");
                FireCurlEvent(reverse: true);
            }

            return;
        }

        IsWalkingTowardPlayer = true;

        if (!Game1.IsMasterGame)
            return;

        if (_curlCooldownMs.Value > 0)
        {
            CustomMonsterDebug.CrawlerHeartbeat(this, "curl_blocked_cooldown", $"curl blocked: cooldownMs={_curlCooldownMs.Value}", 30);
            return;
        }

        if (!withinPlayerThreshold(CurlTriggerDistanceTiles))
        {
            CustomMonsterDebug.CrawlerHeartbeat(this, "curl_blocked_range", $"curl blocked: target out of range triggerTiles={CurlTriggerDistanceTiles}", 20);
            return;
        }

        CustomMonsterDebug.Crawler(this, $"curl trigger met triggerTiles={CurlTriggerDistanceTiles}");
        FireCurlEvent(reverse: false);
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

        var shouldAnimateMovement = isMoving() || ((CrawlerState)_state.Value == CrawlerState.Curled && _curledHaltMs < CurledHaltGraceMs);
        if (!shouldAnimateMovement)
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

    private void FireCurlEvent(bool reverse)
        => _curlEvent.Fire(reverse);

    private void PerformCurl(bool reverse)
    {
        EnsureTexture(_curledSpriteTexture);
        Sprite.setCurrentAnimation(BuildCurlAnimation(reverse));
        Sprite.loop = false;

        if (!reverse)
        {
            _state.Value = (int)CrawlerState.Curled;
            _curlRemainingMs.Value = CurlDurationMs;
            _curlCooldownMs.Value = CurlCooldownMs;
            _curledHaltMs = 0;
            ApplyCurledSpeedBonus();
            IsWalkingTowardPlayer = true;
            currentLocation?.playSound("stoneCrack");
            CustomMonsterDebug.Crawler(this, $"curl in start durationMs={CurlDurationMs} cooldownMs={CurlCooldownMs}");
        }
        else
        {
            _curlRemainingMs.Value = 0;
            _curledHaltMs = 0;
            RemoveCurledSpeedBonus();
            _pendingCrawlingTextureAfterTransition = true;
            _state.Value = (int)CrawlerState.Crawling;
            IsWalkingTowardPlayer = true;
            currentLocation?.playSound("skeletonDie");
            CustomMonsterDebug.Crawler(this, "curl out start");
        }
    }

    private List<FarmerSprite.AnimationFrame> BuildCurlAnimation(bool reverse)
    {
        var sequence = GetCurlTransitionSequence();
        var frames = new List<FarmerSprite.AnimationFrame>(sequence.Length);

        if (reverse)
        {
            for (var i = sequence.Length - 1; i >= 0; i--)
                frames.Add(new FarmerSprite.AnimationFrame(sequence[i], i == sequence.Length - 1 ? CurlHoldMs : CurlAnimIntervalMs));

            frames[frames.Count - 1] = new FarmerSprite.AnimationFrame(
                sequence[0],
                CurlAnimIntervalMs,
                0,
                false,
                false,
                OnCurlOutComplete);
            return frames;
        }

        for (var i = 0; i < sequence.Length - 1; i++)
            frames.Add(new FarmerSprite.AnimationFrame(sequence[i], i == 0 ? CurlHoldMs : CurlAnimIntervalMs));

        frames.Add(new FarmerSprite.AnimationFrame(
            sequence[sequence.Length - 1],
            CurlAnimIntervalMs,
            0,
            false,
            false,
            OnCurlInComplete));
        return frames;
    }

    private void OnCurlInComplete(Farmer who)
    {
        Sprite.CurrentAnimation = null;
        Sprite.currentFrame = GetCurledFacingFrame();
        Sprite.UpdateSourceRect();
        CustomMonsterDebug.Crawler(this, "curl in animation complete");
    }

    private void OnCurlOutComplete(Farmer who)
    {
        Sprite.CurrentAnimation = null;
        if (_pendingCrawlingTextureAfterTransition)
        {
            _pendingCrawlingTextureAfterTransition = false;
            EnsureTexture(_crawlingSpriteTexture);
        }
        ShowFacingFrame();
        CustomMonsterDebug.Crawler(this, "curl out animation complete");
    }

    private int[] GetCurlTransitionSequence()
    {
        return FacingDirection switch
        {
            3 => new[] { 16, 15, 14, 13, 12 },
            1 => new[] { 17, 4, 5, 6, 7 },
            2 => new[] { 18, 0, 1, 2, 3 },
            0 => new[] { 19, 8, 9, 10, 11 },
            _ => new[] { 18, 0, 1, 2, 3 },
        };
    }

    private int GetCurledFacingFrame()
    {
        return FacingDirection switch
        {
            3 => 12,
            1 => 7,
            2 => 3,
            0 => 11,
            _ => 3,
        };
    }

    private void HandleStateVisualTransitions()
    {
        var currentState = (CrawlerState)_state.Value;
        if (_previousState is null)
        {
            _previousState = currentState;
            EnsureTexture(currentState == CrawlerState.Curled ? _curledSpriteTexture : _crawlingSpriteTexture);
            ApplyStateSpeed(currentState);
            return;
        }

        if (_previousState.Value == currentState)
            return;

        ApplyStateSpeed(currentState);
        _previousState = currentState;
    }

    private void ApplyStateSpeed(CrawlerState state)
    {
        if (state == CrawlerState.Curled)
            ApplyCurledSpeedBonus();
        else
            RemoveCurledSpeedBonus();
    }

    private void ApplyCurledSpeedBonus()
    {
        if (_curledSpeedApplied)
            return;

        Speed += CurledSpeedBonus;
        _curledSpeedApplied = true;
    }

    private void RemoveCurledSpeedBonus()
    {
        if (!_curledSpeedApplied)
            return;

        Speed = Math.Max(0, Speed - CurledSpeedBonus);
        _curledSpeedApplied = false;
    }

    private void EnsureTexture(string texturePath)
    {
        if (string.Equals(_spriteTexture.Value, texturePath, StringComparison.OrdinalIgnoreCase))
            return;

        _spriteTexture.Value = texturePath;
        reloadSprite();
    }

    private void ShowFacingFrame()
    {
        Sprite.currentFrame = (CrawlerState)_state.Value == CrawlerState.Curled
            ? GetCurledFacingFrame()
            : FacingDirection switch
            {
                0 => 8,
                1 => 4,
                2 => 0,
                3 => 12,
                _ => 0,
            };
        Sprite.UpdateSourceRect();
    }

    private void StartDeathAnimationIfNeeded()
    {
        if (_deathAnimationCompleted)
            return;

        EnsureTexture(_curledSpriteTexture);
        if (_deathAnimationStarted)
            return;

        _deathAnimationStarted = true;
        RemoveCurledSpeedBonus();
        IsWalkingTowardPlayer = false;
        Halt();
        Sprite.StopAnimation();
        Sprite.loop = false;
        Sprite.setCurrentAnimation(BuildDeathAnimation());
    }

    private List<FarmerSprite.AnimationFrame> BuildDeathAnimation()
    {
        var frames = new List<FarmerSprite.AnimationFrame>(DeathFrameLoops);
        for (var i = 0; i < DeathFrameLoops - 1; i++)
            frames.Add(new FarmerSprite.AnimationFrame(DeathFrame, 140));

        frames.Add(new FarmerSprite.AnimationFrame(DeathFrame, 140, 0, false, false, OnDeathAnimationComplete));
        return frames;
    }

    private void OnDeathAnimationComplete(Farmer who)
    {
        _deathAnimationCompleted = true;
        Sprite.CurrentAnimation = null;
        Sprite.currentFrame = DeathFrame;
        Sprite.UpdateSourceRect();
        CustomMonsterDebug.Crawler(this, "death animation complete");
    }

    private void LogStateIfChanged()
    {
        var currentState = (CrawlerState)_state.Value;
        if (_previousLoggedState == currentState)
            return;

        CustomMonsterDebug.Crawler(this, $"state -> {currentState} curlRemainingMs={_curlRemainingMs.Value} cooldownMs={_curlCooldownMs.Value} speed={Speed}");
        _previousLoggedState = currentState;
    }
}
