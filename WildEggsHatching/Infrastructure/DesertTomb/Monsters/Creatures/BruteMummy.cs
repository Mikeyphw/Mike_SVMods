using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Core.Configuration;
using WildEggsHatching.Infrastructure.DesertTomb.Monsters.Base;

namespace WildEggsHatching.Infrastructure.DesertTomb.Monsters.Creatures;

internal sealed class BruteMummy : Mummy
{
    internal BruteMummy(
        MonsterMultipliers multipliers,
        Vector2 position,
        int difficulty = 3,
        string textureName = "Brute Mummy",
        int speedToAdd = 0,
        string? customDropData = null
    )
        : base(position)
    {
        TombMonsterBuilder.SetupBasicMonster(
            this,
            multipliers,
            difficulty,
            textureName,
            speedToAdd + 1, // extra speed increment
            luckyBonusEnabled: true,
            customDropData: customDropData
        );

        TombMonsterBuilder.SetMonsterNames(this, "Brute Mummy");
    }

    protected override void updateMonsterSlaveAnimation(GameTime time)
    {
        if (!isMoving())
            return;

        switch (FacingDirection)
        {
            case 0:
                Sprite.AnimateUp(time);
                break;
            case 3:
                Sprite.AnimateLeft(time);
                break;
            case 1:
                Sprite.AnimateRight(time);
                break;
            case 2:
                Sprite.AnimateDown(time);
                break;
        }
    }

    public override void onDealContactDamage(Farmer who)
    {
        if (
            Game1.random.NextDouble() < 0.3
            && Player == Game1.player
            && !Player.temporarilyInvincible
            && Game1.random.Next(11) >= who.Immunity
            && !Player.hasBuff("28")
            && !Player.hasTrinketWithID("BasiliskPaw")
        )
        {
            Player.applyBuff("14");
            currentLocation.playSound("shadowpeep");
        }

        base.onDealContactDamage(who);
    }
}
