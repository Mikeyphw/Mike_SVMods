using System;
using StardewModdingAPI;

namespace LetMeHatchThisEgg.Infrastructure.Config;

public sealed class SmapiConfigProvider<TConfig>
    where TConfig : class, new()
{
    private readonly IModHelper _helper;

    public SmapiConfigProvider(IModHelper helper)
    {
        _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        Config = _helper.ReadConfig<TConfig>();
    }

    public TConfig Config { get; private set; }

    public void Reload()
    {
        Config = _helper.ReadConfig<TConfig>();
    }

    public void Save()
    {
        _helper.WriteConfig(Config);
    }
}
