using System;
using System.Collections.Generic;
using Mikew.NestCore.Helpers.SaveData;
using EggsInWild.Application.AnimalNotes;
using EggsInWild.Infrastructure.Game;

namespace EggsInWild.Application.Animals;

/// <summary>
/// Resolves whether a given farm-animal key has already been unlocked for weighting purposes.
/// </summary>
public sealed class AnimalUnlockedState
{
    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    private readonly ISaveDataStore _store;
    private readonly IGameMailService _mail;

    public AnimalUnlockedState(ISaveDataStore store, IGameMailService mail)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _mail = mail ?? throw new ArgumentNullException(nameof(mail));
    }

    public bool IsUnlocked(string farmAnimalKey)
    {
        if (string.IsNullOrWhiteSpace(farmAnimalKey))
            return false;

        var mailId = AnimalNoteIds.NoteMailId(farmAnimalKey);
        if (_mail.HasReceivedMail(mailId))
            return true;

        var reg = TryGetRegisteredTypes();
        return reg.Contains(farmAnimalKey);
    }

    private HashSet<string> TryGetRegisteredTypes()
    {
        try
        {
            var env = _store.ReadEnvelope<AnimalNotesSaveData>(SaveKeys.AnimalNotes);
            var data = env?.Data;
            if (data?.RegisteredAnimalTypes is null)
                return new HashSet<string>(KeyComparer);

            return new HashSet<string>(data.RegisteredAnimalTypes, KeyComparer);
        }
        catch
        {
            return new HashSet<string>(KeyComparer);
        }
    }
}
