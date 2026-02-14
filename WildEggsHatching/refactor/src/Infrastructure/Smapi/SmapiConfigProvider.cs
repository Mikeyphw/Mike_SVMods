using System;
using StardewModdingAPI;
using WildEggsHatching.Infrastructure.Config;

namespace WildEggsHatching.Infrastructure.Smapi;

public sealed class SmapiConfigProvider : IConfigProvider
{
    private readonly IModHelper _helper;
    private ModConfig _config;

    public SmapiConfigProvider(IModHelper helper)
    {
        _helper = helper;
        _config = helper.ReadConfig<ModConfig>();
    }

    public ModConfig Config => _config;

    public event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

    public void Reload()
    {
        var old = _config;
        _config = _helper.ReadConfig<ModConfig>();
        ConfigChanged?.Invoke(this, new ConfigChangedEventArgs(old, _config, "reload"));
    }

    public void Save()
    {
        _helper.WriteConfig(_config);
        ConfigChanged?.Invoke(this, new ConfigChangedEventArgs(_config, _config, "save"));
    }

    public void Update(Action<ModConfig> mutator, string reason)
    {
        var old = _config;
        mutator(_config);
        _helper.WriteConfig(_config);
        ConfigChanged?.Invoke(this, new ConfigChangedEventArgs(old, _config, reason));
    }
}
