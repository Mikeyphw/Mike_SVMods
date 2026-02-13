using System.Collections.Generic;
using System.Linq;
using Mikew.BirdsOfAFeather;
using StardewModdingAPI;

namespace BirdsOfAFeather.Helpers
{
    public class I18TranslationHelper
    {
        private readonly IModHelper _helper;
        private readonly HashSet<string> _translationsKeys = new();
        private const string en_i18n = "i18n/default.json";

        // Constructor to initialize the helper
        public I18TranslationHelper(IModHelper modHelper)
        {
            _helper = modHelper;
        }

        public Translation Get(string i18nKey)
        {
            _translationsKeys.Add(i18nKey.ToLower());
            return _helper.Translation.Get(i18nKey);
        }

        private Dictionary<string, string> ReadJson()
        {
            var i18nJson = _helper.Data.ReadJsonFile<Dictionary<string, string>>(en_i18n);
            if (i18nJson == null)
            {
                WriteJson();
                i18nJson = _helper.Data.ReadJsonFile<Dictionary<string, string>>(en_i18n);
            }
            return i18nJson!;
        }

        private void WriteJson()
        {
            _helper.Data.WriteJsonFile<Dictionary<string, string>>(en_i18n, new());
        }

        public void Checki18nKeys(IMonitor monitor)
        {
            var i18nJson = ReadJson();
            foreach (
                var key in i18nJson
                    .Where(k => !_translationsKeys.Contains(k.Key.ToLower()))
                    .Select(k => k.Key)
            )
            {
                i18nJson.Remove(key);
                monitor.Log($"Removed key \"{key}\" from {en_i18n}", LogLevel.Info);
            }
            foreach (
                var key in _translationsKeys.Where(k =>
                    !i18nJson.Keys.Select(k => k.ToLower()).Contains(k)
                )
            )
            {
                i18nJson.Add(key, string.Empty);
                monitor.Log($"Added key \"{key}\" to {en_i18n}", LogLevel.Info);
            }
            WriteJson();
        }
    }
}