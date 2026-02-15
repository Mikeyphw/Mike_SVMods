# WildEggsHatching Public API (Phase 4)

This refactor exposes a **query-only** API via SMAPI `helper.ModRegistry.GetApi<IWildEggsApi>("Mikew.WildEggsHatching")`.

## Contract

- **API version:** `1` (additive-only changes).
- **Threading:** call from the game thread (typical SMAPI callbacks).
- **Host/client:** queries work on both; progression step reflects host state.

## Interface

```csharp
using WildEggsHatching.Api;
using WildEggsHatching.Core.Enums;

public interface IWildEggsApi
{
    int ApiVersion { get; }

    ProgressionStep GetProgressionStep();

    bool IsProgressionModeEnabled();

    bool IsDesertTombModuleEnabled();

    IReadOnlyDictionary<string, int> GetQuestCompletionDays();
}
```

## Example

```csharp
var api = Helper.ModRegistry.GetApi<IWildEggsApi>("Mikew.WildEggsHatching");
if (api is null || api.ApiVersion != 1)
    return;

Monitor.Log($"WildEggs step: {api.GetProgressionStep()}", LogLevel.Info);
```
