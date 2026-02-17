using Mikew.BirdsOfAFeather.Helpers;

namespace Mikew.BirdsOfAFeather.Integrations.Gmcm;

/// <summary>
/// Declarative GMCM layout: render root, then named pages.
/// This avoids accidental "current page" issues.
/// </summary>
public sealed class GmcmMenuLayout<TConfig>
{
    private readonly IGMCMOptionsHelper _menu;
    private readonly Func<TConfig> _getConfig;
    private readonly string _fieldIdPrefix;

    private readonly List<Action<GmcmPageBuilder<TConfig>>> _root = new();
    private readonly List<PageDef> _pages = new();

    internal GmcmMenuLayout(IGMCMOptionsHelper menu, Func<TConfig> getConfig, string fieldIdPrefix)
    {
        _menu = menu;
        _getConfig = getConfig;
        _fieldIdPrefix = fieldIdPrefix;
    }

    public GmcmMenuLayout<TConfig> Root(Action<GmcmPageBuilder<TConfig>> build)
    {
        _root.Add(build);
        return this;
    }

    /// <summary>
    /// Add a translated page (uses <c>config.page.&lt;pageId&gt;.name</c>).
    /// </summary>
    public GmcmMenuLayout<TConfig> Page(string pageId, Action<GmcmPageBuilder<TConfig>> build, bool printId = false)
    {
        _pages.Add(PageDef.I18n(pageId, build, printId));
        return this;
    }

    /// <summary>Add a page with a custom title (title can still be translated).</summary>
    public GmcmMenuLayout<TConfig> Page(string pageId, Func<string> title, Action<GmcmPageBuilder<TConfig>> build, bool printId = false)
    {
        _pages.Add(PageDef.Custom(pageId, title, build, printId));
        return this;
    }

    public void Build()
    {
        var builder = new GmcmPageBuilder<TConfig>(_menu, _getConfig, _fieldIdPrefix);

        builder.SetCurrentPage("root");
        foreach (var r in _root)
            r(builder);

        foreach (var page in _pages)
        {
            if (page.Title is null)
                _menu.AddPage(page.PageId, printID: page.PrintId);
            else
                _menu.AddPage(page.PageId, page.Title, printID: page.PrintId);

            builder.SetCurrentPage(page.PageId);
            page.Build(builder);
        }
    }

    private readonly record struct PageDef(
        string PageId,
        Func<string>? Title,
        Action<GmcmPageBuilder<TConfig>> Build,
        bool PrintId)
    {
        public static PageDef I18n(string id, Action<GmcmPageBuilder<TConfig>> build, bool printId)
            => new(id, null, build, printId);

        public static PageDef Custom(string id, Func<string> title, Action<GmcmPageBuilder<TConfig>> build, bool printId)
            => new(id, title, build, printId);
    }
}
