namespace WildEggsHatching.Models
{
    public class ExtraConditions
    {
        public int StartYear { get; set; }
        public List<string> Seasons { get; set; }
        public List<int> Days { get; set; }
        public string GameStateQuery { get; set; }
        public List<string> WeatherYesterday { get; set; }
        public List<string> WeatherToday { get; set; }
        public List<string> WeatherTomorrow { get; set; }

        public ExtraConditions()
        {
            StartYear = 1;
            GameStateQuery = string.Empty;
            Seasons = new List<string>();
            Days = new List<int>();
            WeatherYesterday = new List<string>();
            WeatherToday = new List<string>();
            WeatherTomorrow = new List<string>();
        }
        public ExtraConditions DeepClone()
        {
            return new ExtraConditions
            {
                StartYear = this.StartYear,
                GameStateQuery = this.GameStateQuery,
                Seasons = new List<string>(this.Seasons), // Create a new list and copy elements
                Days = new List<int>(this.Days), // Create a new list and copy elements
                WeatherYesterday = new List<string>(this.WeatherYesterday), // Create a new list and copy elements
                WeatherToday = new List<string>(this.WeatherToday), // Create a new list and copy elements
                WeatherTomorrow = new List<string>(this.WeatherTomorrow) // Create a new list and copy elements
            };
        }
    }
}
