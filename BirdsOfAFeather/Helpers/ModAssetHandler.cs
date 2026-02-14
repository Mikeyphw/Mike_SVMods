using Microsoft.Xna.Framework.Graphics;
using Mikew.BirdsOfAFeather.Models;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using xTile;

namespace Mikew.BirdsOfAFeather.Helpers
{
    public partial class ModAssetHandler
    {
        private readonly IMonitor _monitor;
        private readonly string _modID;

        private Dictionary<
            string,
            Dictionary<AssetPathType, string>
        > ModAssets { get; set; } = new Dictionary<string, Dictionary<AssetPathType, string>>();
        
        internal readonly List<AssetData> AssetsData = new();
        public List<string> GamePaths { get; private set; } = new();

        public ModAssetHandler(string modID, IMonitor monitor)
        {
            _modID = modID;
            _monitor = monitor;
        }

        public enum AssetPathType
        {
            GamePath,
            ModFolderPath
        }

        public enum FileExtension
        {
            Png,
            Tmx
        }

        public string GetAssetPath<TEnum>(TEnum asset, AssetPathType assetDetail)
            where TEnum : Enum
        {
            string assetName = asset.ToString();
            return GetPath(assetName, assetDetail);
        }

        public string GetAssetPath(string asset, AssetPathType assetDetail)
        {
            return GetPath(asset, assetDetail);
        }

        private string GetPath(string assetName, AssetPathType pathType)
        {
            if (
                ModAssets.TryGetValue(assetName.ToLower(), out var innerDictionary)
                && innerDictionary.TryGetValue(pathType, out var value)
            )
            {
                return value;
            }
            else
            {
                // Log a message indicating why the path was not found
                _monitor.Log(
                    $"Path not found for asset '{assetName}' with path type '{pathType}'.",
                    LogLevel.Warn
                );
            }

            return pathType switch
            {
                AssetPathType.GamePath => $"Mods/{_modID}/erroritem",
                AssetPathType.ModFolderPath => $"assets/objects/erroritem",
                _ => string.Empty,
            };
        }

        public void AddAssets(List<AssetData> assetsToAdd)
        {
            AssetsData.AddRange(assetsToAdd);
        }

        public void SetUpAssets()
        {
            foreach (var data in AssetsData)
            {
                var enumType = data.EnumAssetType;
                var folder = data.EnumAssetFolder;
                var fileExtension = data.FileExt;
                string gamePath;

                if (data.GamePath != null)
                {
                    gamePath = data.GamePath;
                }
                else
                {
                    gamePath = $"Mods/{_modID}";
                }

                var assets = Enum.GetValues(enumType)
                    .Cast<Enum>()
                    .Select(enumValue => Enum.GetName(enumType, enumValue))
                    .ToList();
                var nonNullAssets = new List<string>(assets.Where(asset => asset != null)!);
                AddModAssets(folder, nonNullAssets, fileExtension, gamePath);
            }
        }

        private void AddModAssets(
            Enum folderType,
            List<string> assetsList,
            FileExtension fileExtension,
            string? gamePath = null
        )
        {
            gamePath ??= $"Mods/{_modID}";
            var folderName = folderType.ToString();
            foreach (var asset in assetsList)
            {
                var gamePathString = $"{gamePath}/{asset}";
                var modFolderPathString =
                    $"assets/{folderName}/{asset}.{Enum.GetName(typeof(FileExtension), fileExtension)}";
                ModAssets.Add(
                    asset.ToLower(),
                    new Dictionary<AssetPathType, string>
                    {
                        { AssetPathType.GamePath, gamePathString.ToLower() },
                        { AssetPathType.ModFolderPath, modFolderPathString.ToLower() }
                    }
                );
                GamePaths.Add(ModAssets[asset.ToLower()][AssetPathType.GamePath]);
            }
        }

        public void LoadModAssets(AssetRequestedEventArgs e)
        {
            var assetPath = GetAssetPath(
                Path.GetFileName(e.NameWithoutLocale.BaseName),
                AssetPathType.ModFolderPath
            );
            string assetExtension = Path.GetExtension(assetPath).Substring(1);

            switch (assetExtension.ToLower())
            {
                case "png":
                    e.LoadFromModFile<Texture2D>(assetPath, AssetLoadPriority.Medium);
                    break;
                case "tmx":
                    e.LoadFromModFile<Map>(assetPath, AssetLoadPriority.Medium);
                    break;
                default:
 break;
            }
        }
    }
}