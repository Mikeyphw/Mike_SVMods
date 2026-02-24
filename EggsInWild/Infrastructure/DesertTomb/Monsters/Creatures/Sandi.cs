using System;
using System.Collections.Generic;
using Netcode;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Base;

namespace EggsInWild.Infrastructure.DesertTomb.Monsters.Creatures;

/// <summary>
/// Slow pursuer with a chance to dive under sand when attacked, becoming immune for a short duration.
/// </summary>
internal sealed class Sandi : Monster
{
    private enum SandiState
    {
        Surfaced = 0,
        Buried = 1,
    }

    private const double DiveChanceOnHit = 0.18;
    private const int DiveDurationMs = 2200;
    private const int DiveCooldownMs = 4200;

    private const int DiveAnimStartFrame = 12;
    private const int DiveAnimFrames = 4;
    private const int DiveAnimIntervalMs = 100;


    private readonly NetInt _state = new((int)SandiState.Surfaced);
    private readonly NetInt _diveRemainingMs = new(0);
    private readonly NetInt _diveCooldownMs = new(0);
    private readonly NetString _spriteTexture = new();
    private readonly NetInt _baseDamageToFarmer = new(0);

    private readonly NetEvent1Field<bool, NetBool> _diveEvent = new();

    internal Sandi(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 3,
        string textureName = "Sandi",
        int speedToAdd = 0,
        string? customDropData = null
    )
        : base("Sandi", position)
    {
        _spriteTexture.Value = $"Characters\\Monsters\\{textureName.Replace(" ", "")}";

        MaxHealth = 150;
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

        _baseDamageToFarmer.Value = DamageToFarmer;

        TombMonsterBuilder.SetMonsterNames(this, "Sandi");
    }

    protected override void initNetFields()
    {
        base.initNetFields();
        NetFields
            .AddField(_state, nameof(_state))
            .AddField(_diveRemainingMs, nameof(_diveRemainingMs))
            .AddField(_diveCooldownMs, nameof(_diveCooldownMs))
            .AddField(_spriteTexture, nameof(_spriteTexture))
            .AddField(_baseDamageToFarmer, nameof(_baseDamageToFarmer))
            .AddField(_diveEvent, nameof(_diveEvent));

        _diveEvent.onEvent += PerformDive;
    }

    public override void reloadSprite(bool onlyAppearance = false)
    {
        Sprite = new AnimatedSprite(_spriteTexture.Value, 0, 16, 16);
    }

    public override void update(GameTime time, GameLocation location)
    {
        _diveEvent.Poll();
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
        if ((SandiState)_state.Value == SandiState.Buried)
            return -1;

        var dealt = base.takeDamage(damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);

        if (!Game1.IsMasterGame)
            return dealt;

        if (Health <= 0)
            return dealt;

        if (_diveCooldownMs.Value > 0)
            return dealt;

        if (Game1.random.NextDouble() <= DiveChanceOnHit)
            FireDiveEvent(reverse: false);

        return dealt;
    }

    public override void behaviorAtGameTick(GameTime time)
    {
        base.behaviorAtGameTick(time);

        var elapsedMs = (int)Math.Max(0, time.ElapsedGameTime.TotalMilliseconds);

        if (_diveCooldownMs.Value > 0)
            _diveCooldownMs.Value = Math.Max(0, _diveCooldownMs.Value - elapsedMs);

        if ((SandiState)_state.Value == SandiState.Buried)
        {
            IsWalkingTowardPlayer = false;
            Halt();

            if (_diveRemainingMs.Value > 0)
                _diveRemainingMs.Value = Math.Max(0, _diveRemainingMs.Value - elapsedMs);

            if (Game1.IsMasterGame && _diveRemainingMs.Value <= 0)
                FireDiveEvent(reverse: true);

            return;
        }

        IsWalkingTowardPlayer = true;
    }

    protected override void updateMonsterSlaveAnimation(GameTime time)
    {
        if ((SandiState)_state.Value == SandiState.Buried)
        {
            if (Sprite.CurrentAnimation is null)
                Sprite.Animate(time, DiveAnimStartFrame + DiveAnimFrames - 1, 1, 200f);
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

    private void FireDiveEvent(bool reverse)
        => _diveEvent.Fire(reverse);

    private void PerformDive(bool reverse)
    {
        Sprite.setCurrentAnimation(BuildDiveAnimation(reverse));
        Sprite.loop = false;

        if (!reverse)
        {
            _state.Value = (int)SandiState.Buried;
            _diveRemainingMs.Value = DiveDurationMs;
            _diveCooldownMs.Value = DiveCooldownMs;

            IsInvisible = true;
            damageToFarmer.Value = 0;
            collidesWithOtherCharacters.Value = false;

            Halt();
            currentLocation?.playSound("dirtyHit");
        }
        else
        {
            _state.Value = (int)SandiState.Surfaced;
            _diveRemainingMs.Value = 0;

            IsInvisible = false;
            damageToFarmer.Value = _baseDamageToFarmer.Value;
            collidesWithOtherCharacters.Value = true;

            currentLocation?.playSound("skeletonDie");
        }
    }

    private List<FarmerSprite.AnimationFrame> BuildDiveAnimation(bool reverse)
    {
        var frames = new List<FarmerSprite.AnimationFrame>();

        if (!reverse)
        {
            for (var i = 0; i < DiveAnimFrames; i++)
                frames.Add(new FarmerSprite.AnimationFrame(DiveAnimStartFrame + i, DiveAnimIntervalMs));

            frames.Add(new FarmerSprite.AnimationFrame(DiveAnimStartFrame + DiveAnimFrames - 1, DiveAnimIntervalMs, 0, false, false, OnDiveInComplete));
        }
        else
        {
            for (var i = DiveAnimFrames - 1; i >= 0; i--)
                frames.Add(new FarmerSprite.AnimationFrame(DiveAnimStartFrame + i, DiveAnimIntervalMs));

            frames.Add(new FarmerSprite.AnimationFrame(DiveAnimStartFrame, DiveAnimIntervalMs, 0, false, false, OnDiveOutComplete));
        }

        return frames;
    }

    private void OnDiveInComplete(Farmer who)
    {
        Sprite.currentFrame = DiveAnimStartFrame + DiveAnimFrames - 1;
        Sprite.CurrentAnimation = null;
    }

    private void OnDiveOutComplete(Farmer who)
    {
        Sprite.currentFrame = 0;
        Sprite.CurrentAnimation = null;
    }
}
