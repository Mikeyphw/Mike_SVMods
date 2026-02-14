using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.Models
{
    public class OutputItemCloneInfo
    {
        public string InputItemID { get; set; }
        public string InputItemClonedID { get; set; }
        public string OutputItemOriginalID { get; set; }
        public string OutputItemClonedID { get; set; }

        public OutputItemCloneInfo(
            string inputItemID,
            string inputItemClonedID,
            string outputItemOriginalID,
            string outputItemClonedID
        )
        {
            InputItemID = inputItemID;
            InputItemClonedID = inputItemClonedID;
            OutputItemOriginalID = outputItemOriginalID;
            OutputItemClonedID = outputItemClonedID;
        }
    }
}