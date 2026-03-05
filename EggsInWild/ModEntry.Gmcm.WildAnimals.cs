using Mikew.NestCore.Helpers;
using Mikew.NestCore.Integrations.Gmcm;
using EggsInWild.Infrastructure.Config;
using EggsInWild.Infrastructure.Integrations.Gmcm;
using EggsInWild.Infrastructure.Integrations.Gmcm.Pages.WildAnimals;

namespace EggsInWild;

public sealed partial class ModEntry
{
    private partial void DefineGmcmWildAnimalsPages(GmcmMenuLayout<ModConfig> layout, I18TranslationHelper i18n)
    {
        var ctx = new EiwGmcmContext(
            Helper,
            Monitor,
            ModManifest,
            () => _config!.Get(),
            () => _config!.Save());

        WildAnimalsPages.Define(ctx, layout, i18n);
        _knownLocationTypeKeysForNormalization = ctx.KnownLocationTypeKeys;
    }
}
