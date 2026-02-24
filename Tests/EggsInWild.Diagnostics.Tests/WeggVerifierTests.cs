using System;
using System.Collections.Generic;
using StardewModdingAPI;
using EggsInWild.Infrastructure.Diagnostics;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Logging;
using EggsInWild.Infrastructure.SaveData;
using Xunit;

namespace EggsInWild.Diagnostics.Tests;

public sealed class WeggVerifierTests
{
    [Fact]
    public void Verify_ShouldFail_WhenNestCoreMissing()
    {
        var helper = new FakeHelper(loadedMods: new HashSet<string>());
        var store = new FakeSaveDataStore();
        var config = new FakeConfigProvider(new ModConfig());
        var log = new FakeLog();

        var report = WeggVerifier.Run(helper, store, config, log, full: false);

        Assert.False(report.Success);
        Assert.Contains(report.Checks, c => c.Id == "nestcore.present" && c.Status == "fail");
    }

    [Fact]
    public void Verify_ShouldWarn_WhenGmcmMissing()
    {
        var helper = new FakeHelper(loadedMods: new HashSet<string> { "Mikew.NestCore" });
        var store = new FakeSaveDataStore();
        var config = new FakeConfigProvider(new ModConfig());
        var log = new FakeLog();

        var report = WeggVerifier.Run(helper, store, config, log, full: false);

        Assert.Contains(report.Checks, c => c.Id == "gmcm.present" && c.Status == "warn");
    }

    private sealed class FakeHelper : IModHelper
    {
        public FakeHelper(HashSet<string> loadedMods)
        {
            ModRegistry = new FakeModRegistry(loadedMods);
        }

        public IModRegistry ModRegistry { get; }

        public string DirectoryPath => ".";

        #region Unused members
        public IContentHelper Content => throw new NotImplementedException();
        public IDataHelper Data => throw new NotImplementedException();
        public IEventsHelper Events => throw new NotImplementedException();
        public IInputHelper Input => throw new NotImplementedException();
        public IMultiplayerHelper Multiplayer => throw new NotImplementedException();
        public IReflectionHelper Reflection => throw new NotImplementedException();
        public ISemanticVersioningApi SemanticVersioning => throw new NotImplementedException();
        public ITranslationHelper Translation => throw new NotImplementedException();
        public T ReadConfig<T>() where T : new() => throw new NotImplementedException();
        public void WriteConfig<T>(T config) where T : class => throw new NotImplementedException();
        public void WriteConfig<T>(T config, string path) where T : class => throw new NotImplementedException();
        public T ReadConfig<T>(string path) where T : new() => throw new NotImplementedException();
        public IModEvents ModEvents => throw new NotImplementedException();
        public IModContentHelper ModContent => throw new NotImplementedException();
        #endregion
    }

    private sealed class FakeModRegistry : IModRegistry
    {
        private readonly HashSet<string> _loadedMods;

        public FakeModRegistry(HashSet<string> loadedMods) => _loadedMods = loadedMods;

        public bool IsLoaded(string uniqueID) => _loadedMods.Contains(uniqueID);

        #region Unused members
        public IModInfo? Get(string uniqueID) => null;
        public T? GetApi<T>(string uniqueID) where T : class => null;
        public IEnumerable<IModInfo> GetAll() => Array.Empty<IModInfo>();
        public IEnumerable<IModInfo> GetAll(bool includeDisabled) => Array.Empty<IModInfo>();
        public bool AreAllLoaded(params string[] uniqueIDs) => throw new NotImplementedException();
        public bool AreLoaded(params string[] uniqueIDs) => throw new NotImplementedException();
        #endregion
    }

    private sealed class FakeSaveDataStore : ISaveDataStore
    {
        public T? ReadEnvelope<T>(string key) where T : class => null;
        public void WriteEnvelope<T>(string key, T data) where T : class => throw new NotImplementedException();
        public void Delete(string key) { }
    }

    private sealed class FakeConfigProvider : IConfigProvider
    {
        public FakeConfigProvider(ModConfig cfg) => Config = cfg;
        public ModConfig Config { get; private set; }
        public event EventHandler<ConfigChangedEventArgs>? ConfigChanged;
        public void Reload() { }
        public void Save() { }
        public void ResetToDefaults() => Config = new ModConfig();
        public void Update(Action<ModConfig> mutator, string reason) => mutator(Config);
    }

    private sealed class FakeLog : ILog
    {
        public void Log(string message, LogLevel level) { }
    }
}
