using Microsoft.Xna.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;
using static Mikew.BirdsOfAFeather.EnumsAndStrings.Strings;

using static LetMeHatchThisEgg.ModEntry; namespace LetMeHatchThisEgg.Models
{
    public class ItemTextureInfo
    {
        public string TextureName { get; set; }
        public string ID { get; set; }
        public int SpriteIndex { get; set; }
        public CloneItemType ItemType { get; set; }
        public bool IsColored { get; set; }

        public ItemTextureInfo(
            string id,
            int spriteIndex,
            CloneItemType cloneItemType,
            string? textureName = null,
            bool isColored = true
        )
        {
            ID = id;
            SpriteIndex = spriteIndex;
            TextureName = textureName ?? AssetNames.SpringObjects;
            ItemType = cloneItemType;
            IsColored = isColored;
            //  Options = _options[id];
        }

        public ItemTextureInfo(string id, string? textureName = AssetNames.SpringObjects)
        {
            ID = id;
            SpriteIndex = Int32.Parse(id);
            TextureName = textureName ?? AssetNames.SpringObjects;
            //  Options = _options[id];
        }

        public Rectangle GetCropRectangle(int imageWidth)
        {
            int cropX = 0;
            int cropY = 0;

            int spriteWidth = Math.Min(imageWidth, 16);
            int spriteHeight = Math.Min(imageWidth, 16); // assuming image is square

            try
            {
                cropX = SpriteIndex % (imageWidth / spriteWidth) * spriteWidth;
                cropY = SpriteIndex / (imageWidth / spriteWidth) * spriteHeight;
            }
            catch (System.Exception ex)
            {
                LoggerHelper.VerboseLog($"Error: {ex.Message}");
            }

            // Check if cropX is outside the image bounds
            if (cropX + spriteWidth > imageWidth)
            {
                cropX = (cropX + spriteWidth) % imageWidth;
                cropY += spriteHeight; // Move to the next row
            }

            return new Rectangle(cropX, cropY, spriteWidth, spriteHeight);
        }
    }
}
