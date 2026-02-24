using System;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using EggsInWild.DesertTomb.Core.Configuration;
using EggsInWild.Infrastructure.DesertTomb.Monsters.Base;

namespace EggsInWild.Infrastructure.DesertTomb.Monsters.Creatures;

/// <summary>
/// Wind spirit that spins in a small circle until aligned with a player, then charges and attempts to pull them in.
/// </summary>
internal sealed class Hurricane : Monster
{
    private enum HurricaneState
    {
        Swirl = 0,
        Charge = 1,
        Suck = 2,
    }

    private const int AlignTiles = 10;
    private const int SuckDistanceTiles = 2;
    private const int SwirlSegmentMs = 600;
    private const int ChargeMs = 900;
    private const int SuckMs = 1600;

    private readonly NetInt _state = new((int)HurricaneState.Swirl);
    private readonly NetInt _stateTimerMs = new(SwirlSegmentMs);
    private readonly NetInt _swirlDir = new(1); // 0 up,1 right,2 down,3 left
    private readonly NetString _spriteTexture = new();

    private Point _anchorTile;

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
        _spriteTexture.Value = $"Characters\\Monsters\\{textureName.Replace(" ", "")}";
        _anchorTile = new Point((int)(position.X / 64f), (int)(position.Y / 64f));

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
    }

    protected override void initNetFields()
    {
        base.initNetFields();
        NetFields
            .AddField(_state, nameof(_state))
            .AddField(_stateTimerMs, nameof(_stateTimerMs))
            .AddField(_swirlDir, nameof(_swirlDir))
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

        if (_stateTimerMs.Value > 0)
            _stateTimerMs.Value = Math.Max(0, _stateTimerMs.Value - elapsedMs);

        switch ((HurricaneState)_state.Value)
        {
            case HurricaneState.Suck:
                IsWalkingTowardPlayer = false;
                Halt();

                if (Game1.IsMasterGame)
                    ApplySuction();

                if (_stateTimerMs.Value <= 0)
                {
                    _state.Value = (int)HurricaneState.Swirl;
                    _stateTimerMs.Value = SwirlSegmentMs;
                }

                return;

            case HurricaneState.Charge:
                if (_stateTimerMs.Value <= 0)
                {
                    _state.Value = (int)HurricaneState.Swirl;
                    _stateTimerMs.Value = SwirlSegmentMs;
                    return;
                }

                ChargeTowardPlayer();
                return;

            case HurricaneState.Swirl:
            default:
                break;
        }

        if (Game1.IsMasterGame)
        {
            if (TryEnterSuck())
                return;

            if (TryEnterCharge())
                return;
        }

        Swirl();
    }

    protected override void updateMonsterSlaveAnimation(GameTime time)
    {
        Sprite.Animate(time, 0, 4, (HurricaneState)_state.Value == HurricaneState.Suck ? 70f : 110f);
    }

    private void Swirl()
    {
        IsWalkingTowardPlayer = false;

        if (Game1.IsMasterGame && _stateTimerMs.Value <= 0)
        {
            _stateTimerMs.Value = SwirlSegmentMs;
            _swirlDir.Value = (_swirlDir.Value + 1) % 4;
        }

        // keep the swirl roughly near the anchor to avoid drifting.
        var tile = TilePoint;
        if (Math.Abs(tile.X - _anchorTile.X) > 2 || Math.Abs(tile.Y - _anchorTile.Y) > 2)
            _swirlDir.Value = (_swirlDir.Value + 2) % 4;

        SetMoveDirection(_swirlDir.Value);
    }

    private bool TryEnterCharge()
    {
        var target = Player;
        if (target is null)
            return false;

        var myTile = TilePoint;
        var playerTile = target.TilePoint;

        var aligned = myTile.X == playerTile.X || myTile.Y == playerTile.Y;
        if (!aligned)
            return false;

        if (Math.Abs(myTile.X - playerTile.X) > AlignTiles || Math.Abs(myTile.Y - playerTile.Y) > AlignTiles)
            return false;

        _state.Value = (int)HurricaneState.Charge;
        _stateTimerMs.Value = ChargeMs;
        FaceTargetInLine(target);
        currentLocation.playSound("wind");
        return true;
    }

    private void ChargeTowardPlayer()
    {
        IsWalkingTowardPlayer = false;
        SetMoveDirection(FacingDirection);

        if (withinPlayerThreshold(SuckDistanceTiles) && Game1.IsMasterGame)
        {
            _state.Value = (int)HurricaneState.Suck;
            _stateTimerMs.Value = SuckMs;
            Halt();
            currentLocation.playSound("debuffSpell");
        }
    }

    private bool TryEnterSuck()
    {
        if (!withinPlayerThreshold(SuckDistanceTiles))
            return false;

        _state.Value = (int)HurricaneState.Suck;
        _stateTimerMs.Value = SuckMs;
        Halt();
        currentLocation.playSound("debuffSpell");
        return true;
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

    private void FaceTargetInLine(Farmer target)
    {
        var myTile = TilePoint;
        var playerTile = target.TilePoint;

        if (playerTile.X > myTile.X) FacingDirection = 1;
        else if (playerTile.X < myTile.X) FacingDirection = 3;
        else if (playerTile.Y > myTile.Y) FacingDirection = 2;
        else FacingDirection = 0;
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
