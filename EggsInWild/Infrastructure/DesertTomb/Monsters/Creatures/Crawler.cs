using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
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

    private const int CurlAnimStartFrame = 12;
    private const int CurlAnimFrames = 4;
    private const int CurlAnimIntervalMs = 100;


    private static int CurlDurationMs => DebugKnobs.CrawlerCurlDurationMs;
    private static int CurlCooldownMs => DebugKnobs.CrawlerCurlCooldownMs;

    private readonly NetInt _state = new((int)CrawlerState.Crawling);
    private readonly NetInt _curlRemainingMs = new(0);
    private readonly NetInt _curlCooldownMs = new(0);
    private readonly NetString _spriteTexture = new();

    private readonly NetEvent1Field<bool, NetBool> _curlEvent = new();

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
        _spriteTexture.Value = $"Characters\\Monsters\\{textureName.Replace(" ", "")}";

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
        Sprite = new AnimatedSprite(_spriteTexture.Value, 0, 16, 16);
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
        if ((CrawlerState)_state.Value == CrawlerState.Curled)
            return -1;

        return base.takeDamage(damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);
    }

    public override void behaviorAtGameTick(GameTime time)
    {
        base.behaviorAtGameTick(time);

        var elapsedMs = (int)Math.Max(0, time.ElapsedGameTime.TotalMilliseconds);

        if (_curlCooldownMs.Value > 0)
            _curlCooldownMs.Value = Math.Max(0, _curlCooldownMs.Value - elapsedMs);

        if ((CrawlerState)_state.Value == CrawlerState.Curled)
        {
            IsWalkingTowardPlayer = false;
            Halt();

            if (_curlRemainingMs.Value > 0)
                _curlRemainingMs.Value = Math.Max(0, _curlRemainingMs.Value - elapsedMs);

            if (Game1.IsMasterGame && _curlRemainingMs.Value <= 0)
                FireCurlEvent(reverse: true);

            return;
        }

        IsWalkingTowardPlayer = true;

        if (!Game1.IsMasterGame)
            return;

        if (_curlCooldownMs.Value > 0)
            return;

        if (!withinPlayerThreshold(CurlTriggerDistanceTiles))
            return;

        FireCurlEvent(reverse: false);
    }

    protected override void updateMonsterSlaveAnimation(GameTime time)
    {
        if ((CrawlerState)_state.Value == CrawlerState.Curled)
        {
            if (Sprite.CurrentAnimation is null)
                Sprite.Animate(time, CurlAnimStartFrame + CurlAnimFrames - 1, 1, 200f);
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

    private void FireCurlEvent(bool reverse)
        => _curlEvent.Fire(reverse);

    private void PerformCurl(bool reverse)
    {
        Sprite.setCurrentAnimation(BuildCurlAnimation(reverse));
        Sprite.loop = false;

        if (!reverse)
        {
            _state.Value = (int)CrawlerState.Curled;
            _curlRemainingMs.Value = CurlDurationMs;
            _curlCooldownMs.Value = CurlCooldownMs;

            Halt();
            IsWalkingTowardPlayer = false;
            currentLocation?.playSound("stoneCrack");
        }
        else
        {
            _state.Value = (int)CrawlerState.Crawling;
            _curlRemainingMs.Value = 0;

            IsWalkingTowardPlayer = true;
            currentLocation?.playSound("skeletonDie");
        }
    }

    private List<FarmerSprite.AnimationFrame> BuildCurlAnimation(bool reverse)
    {
        var frames = new List<FarmerSprite.AnimationFrame>();

        if (!reverse)
        {
            for (var i = 0; i < CurlAnimFrames; i++)
                frames.Add(new FarmerSprite.AnimationFrame(CurlAnimStartFrame + i, CurlAnimIntervalMs));

            frames.Add(new FarmerSprite.AnimationFrame(CurlAnimStartFrame + CurlAnimFrames - 1, CurlAnimIntervalMs, 0, false, false, OnCurlInComplete));
        }
        else
        {
            for (var i = CurlAnimFrames - 1; i >= 0; i--)
                frames.Add(new FarmerSprite.AnimationFrame(CurlAnimStartFrame + i, CurlAnimIntervalMs));

            frames.Add(new FarmerSprite.AnimationFrame(CurlAnimStartFrame, CurlAnimIntervalMs, 0, false, false, OnCurlOutComplete));
        }

        return frames;
    }

    private void OnCurlInComplete(Farmer who)
    {
        Sprite.currentFrame = CurlAnimStartFrame + CurlAnimFrames - 1;
        Sprite.CurrentAnimation = null;
    }

    private void OnCurlOutComplete(Farmer who)
    {
        Sprite.currentFrame = 0;
        Sprite.CurrentAnimation = null;
    }
}
