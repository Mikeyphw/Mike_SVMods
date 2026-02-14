using static WildEggsHatching.Managers.GameAssetManagement.QuestEditor;
namespace WildEggsHatching.Helpers
{
    internal class QuestHelper
    {
        internal string ID { get; set; }
        internal QuestType Type { get; set; }
        internal string Title { get; set; }
        internal string QuestDetails { get; set; }
        internal string Hint { get; set; }
        internal string Solution { get; set; }
        internal string NextQuest { get; set; }
        internal int Gold { get; set; }
        internal string RewardDescription { get; set; }
        internal bool Cancellable { get; set; }
        internal string ReactionText { get; set; }

        internal QuestHelper(
            string id,
            QuestType type,
            string title,
            string questDetails,
            string hint,
            string solution,
            bool cancellable,
            string nextQuest = "-1",
            int gold = 0,
            string rewardDescription = "",
            string reactionText = ""
        )
        {
            ID = id;
            Type = type;
            Title = title;
            QuestDetails = questDetails;
            Hint = hint;
            Solution = solution;
            NextQuest = nextQuest;
            Gold = gold;
            RewardDescription = rewardDescription;
            Cancellable = cancellable;
            ReactionText = reactionText;
        }

        internal QuestHelper()
        {
            ID = "";
            Type = QuestType.Basic;
            Title = "";
            QuestDetails = "";
            Hint = "";
            Solution = "";
            NextQuest = "";
            Gold = 0;
            RewardDescription = "";
            Cancellable = true;
            ReactionText = "";
        }

        internal string GetQuestString()
        {
            var fields = new List<string>
            {
                Enum.GetName(Type)!,
                Title,
                QuestDetails,
                Hint,
                Solution,
                NextQuest,
                Gold.ToString(),
                RewardDescription,
                Cancellable.ToString(),
                ReactionText
            };
            return string.Join('/', fields);
        }
    }
}
