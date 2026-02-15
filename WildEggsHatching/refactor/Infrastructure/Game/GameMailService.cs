using System;
using System.Linq;
using StardewValley;

namespace WildEggsHatching.Infrastructure.Game;

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

    private static bool MatchesMailEntry(string entry, string mailId)
    {
        if (string.Equals(entry, mailId, StringComparison.Ordinal))
            return true;

        // SDV stores no-letter mail as "<id>%&NL&%".
        return entry.StartsWith(mailId + "%&", StringComparison.Ordinal);
    }
}
