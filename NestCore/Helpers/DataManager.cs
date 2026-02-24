using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StardewModdingAPI;

namespace Mikew.NestCore.Helpers
{
    public static class DataManager
    {
        public static T LoadData<T>(IModHelper helper, string? fileName = null)
            where T : class, new()
        {
            string path = $"data/{fileName ?? typeof(T).Name}.json";
            string fullPath = Path.Combine(helper.DirectoryPath, path);

            if (!File.Exists(fullPath))
            {
                SaveData(new T(), helper, fileName);
            }

            return helper.Data.ReadJsonFile<T>(path) ?? new T();
        }

        public static void SaveData<T>(T data, IModHelper helper, string? fileName = null)
            where T : class, new()
        {
            string path = $"data/{fileName ?? typeof(T).Name}.json";
            helper.Data.WriteJsonFile(path, data);
        }
    }
}
