using System;

namespace WildEggsHatching.Infrastructure.Config;

public interface IConfigProvider
{
    ModConfig Config { get; }

    event EventHandler<ConfigChangedEventArgs>? ConfigChanged;

    void Reload();

    void Save();

    void ResetToDefaults();

    /// <summary>Mutate config, then persist and broadcast change.</summary>
    void Update(Action<ModConfig> mutator, string reason);
}

public sealed class ConfigChangedEventArgs : EventArgs
{
    public ConfigChangedEventArgs(ModConfig oldConfig, ModConfig newConfig, string reason)
    {
        OldConfig = oldConfig;
        NewConfig = newConfig;
        Reason = reason;
    }

    public ModConfig OldConfig { get; }
    public ModConfig NewConfig { get; }
    public string Reason { get; }
}
