using System;
using Mikew.NestCore.Integrations.Gmcm;
using StardewModdingAPI;

namespace Mikew.NestCore.Framework;

/// <summary>Public API exposed by NestCore for other mods.</summary>
public interface INestCoreApi
{
    /// <summary>API version for compatibility checks.</summary>
    int ApiVersion { get; }

    /// <summary>
    /// Try to create a GMCM integration instance using NestCore's GMCM builder.
    /// Returns <c>null</c> when GMCM isn't installed.
    /// </summary>
    GmcmIntegration? TryCreateGmcmIntegration(IModHelper helper, IManifest manifest);

    /// <summary>
    /// Register (or rebuild) a GMCM menu for the caller using NestCore's GMCM builder DSL.
    /// Returns <c>null</c> when GMCM isn't installed.
    /// </summary>
    /// <typeparam name="TConfig">The caller's config type.</typeparam>
    /// <param name="helper">SMAPI helper from the caller mod.</param>
    /// <param name="manifest">Caller mod manifest.</param>
        /// <param name="getConfig">Returns the current config instance.</param>
    /// <param name="reset">Resets config to defaults.</param>
    /// <param name="save">Persists config changes.</param>
    /// <param name="define">Defines pages + options using NestCore's layout DSL.</param>
    /// <param name="fieldIdPrefix">Optional stable field-id prefix; defaults to caller UniqueID.</param>
    /// <param name="titleScreenOnly">Whether the menu is title-screen only.</param>
    /// <param name="force">Whether to unregister/register before rebuilding.</param>
    /// <param name="attachLocaleHandler">Whether to rebuild on locale change.</param>
    GmcmIntegration? RegisterGmcmMenu<TConfig>(
        IModHelper helper,
        IManifest manifest,
        Func<TConfig> getConfig,
        Action reset,
        Action save,
        Action<GmcmMenuLayout<TConfig>> define,
        string? fieldIdPrefix = null,
        bool titleScreenOnly = false,
        bool force = true,
        bool attachLocaleHandler = true);

}
