namespace EggsInWild.Infrastructure.Game;

public interface IGameMailService
{
    bool HasReceivedMail(string mailId);
    bool HasMailForTomorrow(string mailId);

    /// <summary>Add a mail to be delivered tomorrow, if it's not already received or queued.</summary>
    void AddMailForTomorrow(string mailId, bool noLetter = false);

    /// <summary>Add a mail directly to received mail list (flags/no-letter), if not already present.</summary>
    void AddReceivedMail(string mailId, bool noLetter = false);
}
