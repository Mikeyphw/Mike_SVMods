using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters;
using WildEggsHatching.DesertTomb.Configuration;
using WildEggsHatching.DesertTomb.TombMonsters.Base;

namespace WildEggsHatching.DesertTomb.TombMonsters.Creatures
{
    internal class BruteMummy : Mummy
    {
        internal BruteMummy(
            MonsterMultipliers monsterMultipliers,
            Vector2 position,
            int difficulty = 3,
            string textureName = "BruteMummy",
            int speedToAdd = 0
        )
            : base(position)
        {
            TombMonsterBuilder.SetupBasicMonster(this, difficulty, textureName, speedToAdd + 1); // +1 for the extra speed increment

            TombMonsterBuilder.SetMonsterNames(this, "Brute Mummy");
        }

        protected override void updateMonsterSlaveAnimation(GameTime time)
        {
            if (isMoving())
            {
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
        }

        public override void onDealContactDamage(Farmer who)
        {
            if (
                Game1.random.NextDouble() < 0.3
                && base.Player == Game1.player
                && !base.Player.temporarilyInvincible
                && Game1.random.Next(11) >= who.Immunity
                && !base.Player.hasBuff("28")
                && !base.Player.hasTrinketWithID("BasiliskPaw")
            )
            {
                base.Player.applyBuff("14");
                base.currentLocation.playSound("shadowpeep");
            }
            base.onDealContactDamage(who);
        }
    }
}
