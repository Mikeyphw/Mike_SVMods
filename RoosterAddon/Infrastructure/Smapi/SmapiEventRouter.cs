using RoosterAddon.Application;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace RoosterAddon.Infrastructure.Smapi;

/// <summary>
/// Subscribes to SMAPI events once and forwards them to a coordinator.
/// </summary>
internal sealed class SmapiEventRouter
{
    private readonly IModHelper _helper;
    private readonly IRoosterCoordinator _coordinator;

    private bool _hooked;

    public SmapiEventRouter(IModHelper helper, IRoosterCoordinator coordinator)
    {
        _helper = helper;
        _coordinator = coordinator;
    }

    public void Hook()
    {
        if (_hooked)
            return;

        _hooked = true;

        _helper.Events.GameLoop.GameLaunched += _coordinator.OnGameLaunched;
        _helper.Events.GameLoop.SaveLoaded += _coordinator.OnSaveLoaded;
        _helper.Events.GameLoop.Saving += _coordinator.OnSaving;
        _helper.Events.GameLoop.ReturnedToTitle += _coordinator.OnReturnedToTitle;
        _helper.Events.GameLoop.DayStarted += _coordinator.OnDayStarted;
        _helper.Events.Content.LocaleChanged += _coordinator.OnLocaleChanged;

        _helper.Events.Player.Warped += _coordinator.OnWarped;
        _helper.Events.Content.AssetRequested += _coordinator.OnAssetRequested;
    }

    public void Unhook()
    {
        if (!_hooked)
            return;

        _hooked = false;

        _helper.Events.GameLoop.GameLaunched -= _coordinator.OnGameLaunched;
        _helper.Events.GameLoop.SaveLoaded -= _coordinator.OnSaveLoaded;
        _helper.Events.GameLoop.Saving -= _coordinator.OnSaving;
        _helper.Events.GameLoop.ReturnedToTitle -= _coordinator.OnReturnedToTitle;
        _helper.Events.GameLoop.DayStarted -= _coordinator.OnDayStarted;
        _helper.Events.Content.LocaleChanged -= _coordinator.OnLocaleChanged;

        _helper.Events.Player.Warped -= _coordinator.OnWarped;
        _helper.Events.Content.AssetRequested -= _coordinator.OnAssetRequested;
    }
}
