using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using WildEggsHatching.EnumsAndStrings;
namespace WildEggsHatching.Models
{
    public class WildAnimalData : InputWildAnimalData
    {
        public string ModID { get; set; }
        public HashSet<AnimalKeyData> FarmAnimalKeys { get; set; }
        public string Name { get; set; }

        public WildAnimalData(
            string animalName,
            int price,
            bool isMagic,
            bool largeEgg,
            Dictionary<string, FarmAnimalKeys> extraModsKeys,
            Dictionary<Enums.LocationType, bool> locationTypes
        )
        {
            ModID = string.Join(" || ", extraModsKeys.Keys.Select(k => k.Trim()));
            ExtraModAnimalKeys = extraModsKeys;
            IsMagic = isMagic;
            Name = animalName.Trim();
            Price = price;
            LargeEgg = largeEgg;
            LocationTypes = locationTypes;
            // Simplified farm animal key processing
            FarmAnimalKeys = new();

            // Simplified cloned animal key processing
            /*if (ModEntry.LetMeHatchApi != null)
            {
                var clonedAnimals = ModEntry
                    .LetMeHatchApi.ClonedAnimalsInfo.Where(info =>
                        animalFarmAnimalKeys.Any(key => key.Id == info.FemaleKey)
                    )
                    .Select(info => new AnimalKeyData(info.MaleKey));
                FarmAnimalKeys.AddRange(clonedAnimals);
            }*/

            if (!ModEntry.Config.ChosenWeights.ContainsKey(animalName))
            {
                ModEntry.Config.ChosenWeights[animalName] = (float)
                    Math.Round(Game1.random.NextDouble(), 3);
            }
        }

        internal (string TexturePath, Rectangle Rec) GetAnimalTexturePathAndRec(int y)
        {
            var errorTexture = ("animals/error", new Rectangle(0, 0, 16, 16));

            // Simplified animal key selection
            var animalKey = FarmAnimalKeys
                .OrderBy(key => Game1.random.Next())
                .FirstOrDefault(key => Game1.farmAnimalData.ContainsKey(key.Id));

            if (animalKey == null)
            {
                return errorTexture;
            }

            var animal = Game1.farmAnimalData[animalKey.Id];
            var path = animal.Texture;
            var spriteWidth = animal.SpriteWidth;
            var spriteHeight = animal.SpriteHeight;

            // Simplified rectangle calculation
            Rectangle rect = spriteWidth switch
            {
                32 => new Rectangle(0, y, spriteWidth, spriteHeight),
                _ => new Rectangle(0, 0, spriteWidth, spriteHeight)
            };

            try
            {
                ModEntry.HelperInstance?.GameContent.Load<Texture2D>(path);
            }
            catch (System.Exception)
            {
                return errorTexture;
            }

            return (path, rect);
        }
    }
}
