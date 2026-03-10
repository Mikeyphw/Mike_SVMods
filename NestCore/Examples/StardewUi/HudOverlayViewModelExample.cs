// FILE: NestCore/Examples/StardewUi/HudOverlayViewModelExample.cs
#if NEST_DEV
using System;
using System.Threading.Tasks;
using Mikew.NestCore.Helpers.UI;

namespace Mikew.NestCore.Examples.StardewUi;

/// <summary>
/// Copy/paste example for using ObservableObject + AsyncRelayCommand in a StardewUI HUD overlay.
/// </summary>
public sealed class HudOverlayViewModelExample : ObservableObject
{
    private string _message = "NEST_DEV HUD example";

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public AsyncRelayCommand Refresh { get; }

    public HudOverlayViewModelExample()
    {
        Refresh = new AsyncRelayCommand(
            executeAsync: RefreshAsync,
            onError: ex => Message = $"Error: {ex.Message}"
        );
    }

    private async Task RefreshAsync()
    {
        await Task.Delay(200).ConfigureAwait(false);
        Message = $"Updated at {DateTime.Now:T}";
    }
}
#endif
