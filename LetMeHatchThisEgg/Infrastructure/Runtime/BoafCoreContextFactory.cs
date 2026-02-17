using System.Reflection;
using Mikew.BirdsOfAFeather.Framework;
using Mikew.BirdsOfAFeather.Helpers;
using StardewModdingAPI;

namespace LetMeHatchThisEgg.Infrastructure.Runtime;

/// <summary>
/// Creates BirdsOfAFeather's <see cref="CoreContext"/> without hard-coding a constructor signature.
/// </summary>
internal static class BoafCoreContextFactory
{
    /// <summary>
    /// Attempts to construct a <see cref="CoreContext"/> using common constructor signatures.
    /// </summary>
    public static CoreContext Create(
        IModHelper helper,
        IMonitor monitor,
        IManifest manifest,
        I18TranslationHelper i18n
    )
    {
        var type = typeof(CoreContext);

        // Try: (IModHelper, IMonitor, IManifest, I18TranslationHelper)
        var ctx = TryCreate(type, new object?[] { helper, monitor, manifest, i18n });
        if (ctx is not null)
            return ctx;

        // Try: (IModHelper, IMonitor, IManifest)
        ctx = TryCreate(type, new object?[] { helper, monitor, manifest });
        if (ctx is not null)
            return ctx;

        // Try: (IModHelper, IManifest, IMonitor, I18TranslationHelper)
        ctx = TryCreate(type, new object?[] { helper, manifest, monitor, i18n });
        if (ctx is not null)
            return ctx;

        // Try: (IModHelper, IManifest, IMonitor)
        ctx = TryCreate(type, new object?[] { helper, manifest, monitor });
        if (ctx is not null)
            return ctx;

        throw new MissingMethodException(
            $"Unable to construct {type.FullName}. BirdsOfAFeather CoreContext constructor signature did not match expected patterns."
        );
    }

    private static CoreContext? TryCreate(Type type, object?[] args)
    {
        try
        {
            return (CoreContext?)Activator.CreateInstance(type, args);
        }
        catch (TargetInvocationException)
        {
            return null;
        }
        catch (MissingMethodException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
