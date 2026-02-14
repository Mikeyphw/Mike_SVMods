using System;
using System.Linq;
using WildEggsHatching.Core.Models;

namespace WildEggsHatching.Infrastructure.Save;

/// <summary>
/// Handles loading and saving player progression state to/from save data.
/// Provides versioned DTOs and migration from legacy formats.
/// </summary>
public sealed class PlayerStateRepository
{
    /// <summary>The current save data key used by the mod.</summary>
    private const string SaveKey = ModSaveDataV1.SaveKey;

    /// <summary>The helper instance for reading/writing save data.</summary>
    private readonly StardewModdingAPI.IModHelper _helper;

    /// <summary>
    /// Creates a new player state repository.
    /// </summary>
    /// <param name="helper">The SMAPI mod helper.</param>
    public PlayerStateRepository(StardewModdingAPI.IModHelper? helper = null)
    {
        _helper = helper ?? ModEntry.HelperInstance;
    }

    /// <summary>
    /// Loads the player state from save data.
    /// Handles migration from V1 to V2 format.
    /// </summary>
    /// <returns>The loaded player state.</returns>
    public PlayerWildState Load()
    {
        var state = new PlayerWildState();

        // Try to load V2 format first
        var v2Data = _helper.Data.ReadSaveData<ModSaveDataV2>(SaveKey);
        if (v2Data != null)
        {
            LoadFromV2(state, v2Data);
            return state;
        }

        // Fall back to V1 format and migrate
        var v1Data = _helper.Data.ReadSaveData<ModSaveDataV1>(SaveKey);
        if (v1Data != null)
        {
            var migratedV2 = MigrateFromV1(v1Data);
            LoadFromV2(state, migratedV2);

            // Save in V2 format after migration
            Save(state);

            return state;
        }

        // No save data found - return empty state
        return state;
    }

    /// <summary>
    /// Saves the player state to save data.
    /// Writes in V2 format.
    /// </summary>
    /// <param name="state">The state to save.</param>
    public void Save(PlayerWildState state)
    {
        var v2Data = ConvertToV2(state);

        _helper.Data.WriteSaveData(SaveKey, v2Data);

        ModEntry.LoggerHelper.Log(
            $"Saved player state: {state.UnlockedAnimals.Count} unlocked, {state.DiscoveredAnimals.Count} discovered",
            StardewModdingAPI.LogLevel.Debug
        );
    }

    /// <summary>
    /// Loads state from V2 format.
    /// </summary>
    private void LoadFromV2(PlayerWildState state, ModSaveDataV2 data)
    {
        // Load unlocked animals
        foreach (var animal in data.UnlockedAnimals)
        {
            state.Unlock(animal);
        }

        // Load discovered animals
        foreach (var animal in data.DiscoveredAnimals)
        {
            state.Discover(animal);
        }

        // Load collected notes
        foreach (var note in data.CollectedNotes)
        {
            state.CollectNote(note);
        }

        // Load progression state
        state.StartDay = data.StartDay;
    }

    /// <summary>
    /// Migrates from V1 format to V2.
    /// </summary>
    private ModSaveDataV2 MigrateFromV1(ModSaveDataV1 v1)
    {
        var v2 = ModSaveDataV2.Create();

        // Migrate hatched animals to unlocked animals
        foreach (var hatchedAnimal in v1.HatchedAnimals)
        {
            if (!string.IsNullOrEmpty(hatchedAnimal.FarmAnimalName))
            {
                v2.UnlockedAnimals.Add(hatchedAnimal.FarmAnimalName);
            }
        }

        // Migrate collected notes
        foreach (var note in v1.CollectedNotes)
        {
            v2.CollectedNotes.Add(note);
        }

        // Migrate weather data
        v2.WeatherToday = v1.WeatherToday;
        v2.WeatherTomorrow = v1.WeatherTomorrow;
        v2.WeatherYesterday = v1.WeatherYesterday;

        // Migrate quest completion days (convert string keys to int keys)
        foreach (var kvp in v1.QuestCompletedDay)
        {
            if (int.TryParse(kvp.Key, out var questId))
            {
                v2.QuestCompletionDays[questId] = kvp.Value;
            }
        }

        ModEntry.LoggerHelper.Log(
            $"Migrated save data from V1 to V2: {v2.UnlockedAnimals.Count} unlocked animals",
            StardewModdingAPI.LogLevel.Info
        );

        return v2;
    }

    /// <summary>
    /// Converts player state to V2 format.
    /// </summary>
    private ModSaveDataV2 ConvertToV2(PlayerWildState state)
    {
        var v2 = ModSaveDataV2.Create();

        foreach (var animal in state.UnlockedAnimals)
        {
            v2.UnlockedAnimals.Add(animal);
        }

        foreach (var animal in state.DiscoveredAnimals)
        {
            v2.DiscoveredAnimals.Add(animal);
        }

        foreach (var note in state.CollectedNotes)
        {
            v2.CollectedNotes.Add(note);
        }

        v2.StartDay = state.StartDay;

        return v2;
    }

    /// <summary>
    /// Checks if save data exists.
    /// </summary>
    public bool HasSaveData()
    {
        var v2Data = _helper.Data.ReadSaveData<ModSaveDataV2>(SaveKey);
        if (v2Data != null)
            return true;

        var v1Data = _helper.Data.ReadSaveData<ModSaveDataV1>(SaveKey);
        return v1Data != null;
    }

    /// <summary>
    /// Deletes save data (for testing purposes).
    /// </summary>
    public void DeleteSaveData()
    {
        _helper.Data.WriteSaveData<ModSaveDataV2>(SaveKey, null!);
        _helper.Data.WriteSaveData<ModSaveDataV1>(SaveKey, null!);
    }
}

