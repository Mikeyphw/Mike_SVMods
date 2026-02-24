using static Mikew.NestCore.Helpers.ModAssetHandler;

namespace Mikew.NestCore.Models
{
    public class AssetData
    {
        public Type EnumAssetType { get; set; }
        public Enum EnumAssetFolder { get; set; }
        public FileExtension FileExt { get; set; }
        public string? GamePath { get; set; }

        // Constructor
        public AssetData(
            Type enumAssetType,
            Enum enumAssetFolder,
            FileExtension fileExt,
            string? gamePath
        )
        {
            EnumAssetType = enumAssetType;
            EnumAssetFolder = enumAssetFolder;
            FileExt = fileExt;
            GamePath = gamePath;
        }
    }
}
