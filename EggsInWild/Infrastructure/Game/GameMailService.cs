using System;
using System.Linq;
using StardewValley;

namespace EggsInWild.Infrastructure.Game;

public sealed class GameMailService : IGameMailService
{
    public bool HasReceivedMail(string mailId)
        => Game1.player.mailReceived.Contains(mailId);

    public bool HasMailForTomorrow(string mailId)
        => Game1.player.mailForTomorrow.Any(entry => MatchesMailEntry(entry, mailId));

    public void AddMailForTomorrow(string mailId, bool noLetter = false)
    {
        if (HasReceivedMail(mailId) || HasMailForTomorrow(mailId))
            return;

        Game1.addMailForTomorrow(mailId, noLetter);
    }

    public void AddReceivedMail(string mailId, bool noLetter = false)
    {
        if (HasReceivedMail(mailId))
            return;

        Game1.addMail(mailId, noLetter);
    }

    public bool RemoveReceivedMail(string mailId)
    {
        if (string.IsNullOrWhiteSpace(mailId))
            return false;

        var removed = Game1.player.mailReceived.Remove(mailId);

        // Defensive: sometimes a mail can remain queued in mailbox; remove any exact matches.
        for (var i = Game1.player.mailbox.Count - 1; i >= 0; i--)
        {
            if (string.Equals(Game1.player.mailbox[i], mailId, StringComparison.Ordinal))
            {
                Game1.player.mailbox.RemoveAt(i);
                removed = true;
            }
        }

        return removed;
    }

    public bool RemoveMailForTomorrow(string mailId)
    {
        if (string.IsNullOrWhiteSpace(mailId))
            return false;

        var removedAny = false;

        // mailForTomorrow is a NetStringHashSet; remove matching entry variants.
        var matches = Game1.player.mailForTomorrow.Where(entry => MatchesMailEntry(entry, mailId)).ToList();
        foreach (var entry in matches)
        {
            if (Game1.player.mailForTomorrow.Remove(entry))
                removedAny = true;
        }

        return removedAny;
    }

    private static bool MatchesMailEntry(string entry, string mailId)
    {
        if (string.Equals(entry, mailId, StringComparison.Ordinal))
            return true;

        // SDV stores no-letter mail as "<id>%&NL&%".
        return entry.StartsWith(mailId + "%&", StringComparison.Ordinal);
    }
}
