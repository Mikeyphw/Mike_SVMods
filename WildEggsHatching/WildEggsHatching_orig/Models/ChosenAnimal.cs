using StardewValley;
namespace WildEggsHatching.Models;

public class ChosenAnimal
    {
        public string FarmAnimalName { get; set; }
        public long ID { get; set; }
        public GameLocation Home { get; set; }
        public string HouseType { get; set; }
        public bool WasBorn { get; set; }
        public bool NoteSpawned { get; set; }
        public string NoteID { get; set; }
        public string QuestID { get; set; }
        public string MailID { get; set; }
        public int? DayNoteWasGiven { get; set; }

        public ChosenAnimal()
        {
            FarmAnimalName = "";
            ID = 0;
            HouseType = "";
            WasBorn = false;
            NoteSpawned = false;
            NoteID = "";
            QuestID = "";
            MailID = "";
            DayNoteWasGiven = null;
            Home = null!;
        }

        public ChosenAnimal(string farmAnimalName, long id, GameLocation home, string houseType)
        {
            FarmAnimalName = farmAnimalName;
            ID = id;
            HouseType = houseType;
            Home = home;
            WasBorn = false;
            NoteSpawned = false;
            NoteID = "";
            QuestID = "";
            MailID = "";
            DayNoteWasGiven = null;
        }

        public void SetIDs()
        {
            string animalKey = FarmAnimalName.Replace(" ", "");
            NoteID = $"{ModEntry.ModID}_junimoNote_{animalKey}";
            QuestID = $"wildEggs_{animalKey}Note";
            MailID = $"wildEggs_NoteMail_{animalKey}";
        }
    }

