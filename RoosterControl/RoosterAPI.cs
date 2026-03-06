public class RoosterHatchingApi
{
    private static RoosterHatchingApi? _instance;

    public static RoosterHatchingApi Instance
    {
        get { return _instance ??= new RoosterHatchingApi(); }
    }

    public int ApiVersion => 1;

    public List<string> ChosenFemales { get; private set; } = new();

    public void UpdateChosenFemales(List<string> newFemales)
    {
        ChosenFemales.Clear();
        ChosenFemales.AddRange(newFemales);
    }

    private RoosterHatchingApi() { }
}
