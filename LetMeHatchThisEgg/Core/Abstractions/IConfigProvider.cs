namespace LetMeHatchThisEgg.Core.Abstractions;

/// <summary>Provides access to the mod configuration.</summary>
/// <typeparam name="TConfig">The configuration type.</typeparam>
public interface IConfigProvider<out TConfig>
    where TConfig : class
{
    /// <summary>Gets the current configuration instance.</summary>
    TConfig Config { get; }
}
