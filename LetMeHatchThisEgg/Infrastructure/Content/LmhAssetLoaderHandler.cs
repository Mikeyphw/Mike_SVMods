using LetMeHatchThisEgg.Infrastructure.Content.Loaders;
using Mikew.BirdsOfAFeather.Helpers.Content;
using StardewModdingAPI.Events;

namespace LetMeHatchThisEgg.Infrastructure.Content;

/// <summary>BOAF asset request handler adapter for LMH <see cref="IAssetLoader"/>.</summary>
public sealed class LmhAssetLoaderHandler : IAssetRequestHandler<ContentContext>
{
    private readonly IAssetLoader _loader;

    public LmhAssetLoaderHandler(IAssetLoader loader)
    {
        _loader = loader;
    }

    public int Order => (int)_loader.Priority;

    public bool StopAfterHandle => false;

    public bool IsMatch(AssetRequestedEventArgs e)
    {
        var name = e.NameWithoutLocale.BaseName.ToLowerInvariant();
        return _loader.CanLoad(name, e);
    }

    public bool ShouldApply(ContentContext context, AssetRequestedEventArgs e) => true;

    public void Handle(AssetRequestedEventArgs e, ContentContext context)
    {
        var name = e.NameWithoutLocale.BaseName.ToLowerInvariant();
        _loader.Load(name, e);
    }
}
