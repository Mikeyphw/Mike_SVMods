using StardewModdingAPI.Events;

namespace RoosterControl.Application;

/// <summary>
/// Routes SMAPI events into RoosterControl lifecycle handlers.
/// </summary>
internal interface IRoosterCoordinator
{
    void OnGameLaunched(object? sender, GameLaunchedEventArgs e);
    void OnSaveLoaded(object? sender, SaveLoadedEventArgs e);
    void OnSaving(object? sender, SavingEventArgs e);
    void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e);
    void OnDayStarted(object? sender, DayStartedEventArgs e);
    void OnLocaleChanged(object? sender, LocaleChangedEventArgs e);
    void OnWarped(object? sender, WarpedEventArgs e);
}
