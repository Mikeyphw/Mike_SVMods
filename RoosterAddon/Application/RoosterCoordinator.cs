using StardewModdingAPI;

namespace RoosterAddon.Application;

/// <summary>
/// Coordinator that owns lifecycle flow and delegates into the existing ModEntry implementation.
/// This is a Phase 2 scaffold: later phases should move logic out of ModEntry into services.
/// </summary>
internal sealed class RoosterCoordinator : IRoosterCoordinator
{
    private readonly ModEntry _host;

    public RoosterCoordinator(ModEntry host)
    {
        _host = host;
    }

    public void OnGameLaunched(object? sender, StardewModdingAPI.Events.GameLaunchedEventArgs e) =>
        _host.OnGameLaunched(sender, e);

    public void OnSaveLoaded(object? sender, StardewModdingAPI.Events.SaveLoadedEventArgs e) =>
        _host.OnSaveLoaded(sender, e);

    public void OnSaving(object? sender, StardewModdingAPI.Events.SavingEventArgs e) =>
        _host.OnSaving(sender, e);

    public void OnReturnedToTitle(object? sender, StardewModdingAPI.Events.ReturnedToTitleEventArgs e) =>
        _host.OnReturnedToTitle(sender, e);

    public void OnDayStarted(object? sender, StardewModdingAPI.Events.DayStartedEventArgs e) =>
        _host.OnDayStart(sender, e);

    public void OnLocaleChanged(object? sender, StardewModdingAPI.Events.LocaleChangedEventArgs e) =>
        _host.OnLocaleChanged(sender, e);

    public void OnWarped(object? sender, StardewModdingAPI.Events.WarpedEventArgs e) =>
        _host.OnWarped(sender, e);

    public void OnAssetRequested(object? sender, StardewModdingAPI.Events.AssetRequestedEventArgs e) =>
        _host.OnAssetRequested(sender, e);
}
