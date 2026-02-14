using Microsoft.Xna.Framework;

namespace WildEggsHatching.Models
{
    public class SpawnedEggInfo
    {
        public string Id { get; set; }
        public string MapName { get; set; }
        public string ModID { get; set; }
        public Vector2 Tile { get; set; }

        public SpawnedEggInfo(string id, string modID, string mapName, Vector2 tile)
        {
            MapName = mapName;
            Id = id;
            ModID = modID;
            Tile = tile;
        }
    }
}
