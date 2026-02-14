namespace RoosterAddon.Framework
{
    public interface ILetMeHatchApi
    {
        List<string> AnimalsWithClonedEggs { get; }
        bool CloneEggs {get;}
    }
}
