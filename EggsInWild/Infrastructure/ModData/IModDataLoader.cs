namespace EggsInWild.Infrastructure.ModData;

public interface IModDataLoader
{
    ModDataSnapshot LoadBase();
    ModDataSnapshot Load();
}
