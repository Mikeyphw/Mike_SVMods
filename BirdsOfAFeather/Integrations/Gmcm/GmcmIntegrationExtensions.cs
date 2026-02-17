namespace Mikew.BirdsOfAFeather.Integrations.Gmcm;

public static class GmcmIntegrationExtensions
{
    /// <summary>
    /// Rebuild GMCM using a declarative layout that supports pages + expression-bound options.
    /// </summary>
    public static void RebuildLayout<TConfig>(
        this GmcmIntegration gmcm,
        Func<TConfig> getConfig,
        Action reset,
        Action save,
        Action<GmcmMenuLayout<TConfig>> define,
        string? fieldIdPrefix = null,
        bool titleScreenOnly = false,
        bool force = true,
        bool attachLocaleHandler = true)
    {
        var prefix = fieldIdPrefix ?? gmcm.FieldIdPrefix;

        gmcm.Rebuild(
            reset: reset,
            save: save,
            build: menu =>
            {
                var layout = new GmcmMenuLayout<TConfig>(menu, getConfig, prefix);
                define(layout);
                layout.Build();
            },
            titleScreenOnly: titleScreenOnly,
            force: force,
            attachLocaleHandler: attachLocaleHandler
        );
    }
}
