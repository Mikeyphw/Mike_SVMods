using StardewValley;

namespace WildEggsHatching.Infrastructure.Game;

public sealed class GameMailService : IGameMailService
{
    public bool HasReceivedMail(string mailId)
        => Game1.player.mailReceived.Contains(mailId);

    public void AddMailForTomorrow(string mailId, bool noLetter = false)
    {
        if (!Game1.player.mailForTomorrow.Contains(mailId))
            Game1.player.mailForTomorrow.Add(mailId);

        if (noLetter && !Game1.player.mailReceived.Contains(mailId))
            Game1.player.mailReceived.Add(mailId);
    }
}
