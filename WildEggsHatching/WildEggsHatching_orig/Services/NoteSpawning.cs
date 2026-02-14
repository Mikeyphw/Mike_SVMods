using StardewModdingAPI;
using StardewValley;
using WildEggsHatching.DesertTomb;
using WildEggsHatching.Framework;
using WildEggsHatching.Managers.GameAssetManagement;
using WildEggsHatching.Models;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Services
{
    internal class NoteSpawning
    {
        private readonly ModData _modSaveData;

        internal NoteSpawning(ModData modSaveData)
        {
            _modSaveData = modSaveData;
            LoggerHelper.VerboseLog("NoteSpawning service initialized.");
        }

        internal void HandleHatchedAnimals(GameLocation location)
        {
            LoggerHelper.VerboseLog(
                $"Handling hatched animals in location: {location.NameOrUniqueName}"
            );
            foreach (
                var animal in _modSaveData.HatchedAnimals.Where(a =>
                    a.Home.NameOrUniqueName == location.NameOrUniqueName && !a.NoteSpawned
                )
            )
            {
                LoggerHelper.VerboseLog($"Checking animal: {animal.FarmAnimalName} for note spawning.");
                animal.NoteSpawned = GameAssetManager.HasItemOnThisLocation(
                    location,
                    animal.NoteID
                );
                if (animal.NoteSpawned)
                {
                    LoggerHelper.VerboseLog(
                        $"Note spawned for animal: {animal.FarmAnimalName}. Adding HUD message and quest."
                    );
                    var hudMessage = new HUDMessage(TranslationHelper.Get("hudmessage"))
                    {
                        timeLeft = 5000,
                        messageSubject = ItemRegistry.Create(animal.NoteID),
                    };
                    Game1.addHUDMessage(hudMessage);
                    Game1.player.addQuest(animal.QuestID);
                }
                else
                {
                    LoggerHelper.VerboseLog($"No note spawned for animal: {animal.FarmAnimalName}.");
                }
            }
        }

        private bool SpawnNote(string index, GameLocation location)
        {
            if (location == null)
            {
                LoggerHelper.VerboseLog("Location is null");
                return false;
            }
            var tile = TileUtility.GetRandomTileIncubator(location);

            StardewValley.Object note =
                new(index, 1)
                {
                    Location = location,
                    TileLocation = tile,
                    IsSpawnedObject = true, //allow "normal" forage behavior if applicable (including allowing players to pick it up)
                };
            LoggerHelper.VerboseLog(
                $"Spawning forage object. Name: {note.Name}. Location: {tile.X},{tile.Y} ({location.Name}).",
                LogLevel.Error
            );
            bool success = location.objects.TryAdd(tile, note); //attempt to add the object and return success/failure
            LoggerHelper.VerboseLog(
                success
                    ? $"Successfully spawned note: {note.Name} at {tile.X},{tile.Y}."
                    : $"Failed to spawn note: {note.Name} at {tile.X},{tile.Y}."
            );
            return success;
        }

        internal void Spawn(ChosenAnimal animal)
        {
            LoggerHelper.VerboseLog(
                $"Attempting to spawn note for animal: {animal.FarmAnimalName}"
            );
            if (!animal.NoteSpawned && !GameAssetManager.HasItem(animal.NoteID))
            {
                LoggerHelper.VerboseLog(
                    $"Animal {animal.FarmAnimalName} does not have a note spawned. Spawning note."
                );
                SpawnNote(animal.NoteID, animal.Home);
            }
            else
            {
                LoggerHelper.VerboseLog(
                    $"Animal {animal.FarmAnimalName} already has a note spawned or item exists."
                );
            }
        }
    }
}
