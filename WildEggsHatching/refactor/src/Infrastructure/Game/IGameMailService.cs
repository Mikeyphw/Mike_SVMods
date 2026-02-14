namespace WildEggsHatching.Infrastructure.Game;

public interface IGameMailService
{
    bool HasReceivedMail(string mailId);
    void AddMailForTomorrow(string mailId, bool noLetter = false);
}
