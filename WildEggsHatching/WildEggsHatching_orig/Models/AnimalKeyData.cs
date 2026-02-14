namespace WildEggsHatching.Models
{
    public class AnimalKeyData
    {
        public string Id { get; set; }
        public float Weight { get; set; }

        public AnimalKeyData(string id, float weight = 1)
        {
            Id = id.Trim();
            Weight = weight;
        }
    }
}
