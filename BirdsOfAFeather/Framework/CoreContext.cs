using Mikew.BirdsOfAFeather.Helpers;
using Mikew.BirdsOfAFeather.Helpers.Logging;
using StardewModdingAPI;

namespace Mikew.BirdsOfAFeather.Framework;

/// <summary>
/// Shared context for BirdsOfAFeather helpers.
/// </summary>
public sealed class CoreContext
{
    public CoreContext(IModHelper helper, IMonitor monitor, IManifest manifest, I18TranslationHelper translation)
        : this(helper, monitor, manifest, translation, new SmapiLog(monitor))
    {
    }

    public CoreContext(IModHelper helper, IMonitor monitor, IManifest manifest, I18TranslationHelper translation, ILog log)
    {
        Helper = helper;
        Monitor = monitor;
        Manifest = manifest;
        Translation = translation;
        Log = log;
    }

    public IModHelper Helper { get; }
    public IMonitor Monitor { get; }
    public IManifest Manifest { get; }
    public I18TranslationHelper Translation { get; }
    public ILog Log { get; }
}
