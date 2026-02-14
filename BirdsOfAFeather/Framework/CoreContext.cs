using StardewModdingAPI;
using Mikew.BirdsOfAFeather.Helpers;

namespace Mikew.BirdsOfAFeather.Framework;

/// <summary>
/// Shared context for BirdsOfAFeather helpers.
/// </summary>
public sealed class CoreContext
{
    public CoreContext(IModHelper helper, IMonitor monitor, IManifest manifest, I18TranslationHelper translation)
    {
        Helper = helper;
        Monitor = monitor;
        Manifest = manifest;
        Translation = translation;
    }

    public IModHelper Helper { get; }
    public IMonitor Monitor { get; }
    public IManifest Manifest { get; }
    public I18TranslationHelper Translation { get; }
}
