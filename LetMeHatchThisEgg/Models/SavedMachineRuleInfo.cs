using StardewValley.GameData.Machines;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.Models
{
    public class SavedMachineRuleInfo
    {
        public string MachineKey { get; set; }
        public string OutputRuleId { get; set; }
        public List<MachineOutputTriggerRule> Triggers { get; set; }
        public string OutputItemRuleID { get; set; }
        public string? OutputItemId { get; set; }

        public SavedMachineRuleInfo(
            string machineKey,
            string outputRuleId,
            List<MachineOutputTriggerRule> triggers,
            string outputItemRuleID,
            string? outputItemId
        )
        {
            MachineKey = machineKey;
            OutputRuleId = outputRuleId;
            Triggers = triggers;
            OutputItemRuleID = outputItemRuleID;
            OutputItemId = outputItemId;
        }
    }
}