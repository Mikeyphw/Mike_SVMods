using Mikew.BirdsOfAFeather.Helpers.GameStateQueries;
using StardewValley;
using StardewValley.Extensions;
using WildEggsHatching.Framework;
using static WildEggsHatching.EnumsAndStrings.Strings;
using static WildEggsHatching.ModEntry;

namespace WildEggsHatching.Managers.GameAssetManagement
{
    /// <summary>The mod entry point.</summary>
    internal class MailEditor
    {
        private readonly ModData _modSaveData;
        internal readonly Dictionary<string, string> MailToAdd;
        internal readonly List<string> AllMail = new()
        {
            IDs.Mail.MarnieThanks,
            IDs.Mail.WizardDone,
            IDs.Mail.SpecialOrderDoneI,
        };
        private const string ItemTypeQuest = "quest";
        private const string ItemTypeCraftingRecipe = "craftingRecipe";

        internal MailEditor(ModData modData)
        {
            _modSaveData = modData;
            MailToAdd = new();
            LoggerHelper.VerboseLog("MailEditor initialized.");
        }

        private void CreateMail()
        {
            LoggerHelper.VerboseLog("Creating mail data...");
            MailToAdd[IDs.Mail.MarnieThanks] = CreateMailData(
                "mail.MarnieThanks",
                ItemTypeQuest,
                IDs.Quests.WizardStudy,
                true,
                "Wild Egg Thanks"
            );
            MailToAdd[IDs.Mail.WizardThanks] = CreateMailData(
                "mail.WizardThanks",
                ItemTypeQuest,
                IDs.Quests.TheDesertScroll,
                false,
                "The Desert Scroll"
            );
            MailToAdd[IDs.Mail.WizardFindings] = CreateMailData(
                "mail.WizardFindings",
                ItemTypeCraftingRecipe,
                IDs.Objects.ElixirCrafting,
                false,
                "Wizard Findings"
            );
            MailToAdd[IDs.Mail.MarnieGotIncubator] = CreateMailData(
                "mail.MarnieIncubator",
                ItemTypeCraftingRecipe,
                IDs.BigCraftables.WEIncubatorCrafting,
                false,
                "Marnie Incubator"
            );
            LoggerHelper.VerboseLog("Mail data created.");
        }

        private string CreateMailData(
            string textKey,
            string itemType,
            string itemID,
            bool autoAccept,
            string mailName
        )
        {
            var mailData =
                $"{TranslationHelper.Get(textKey)} %item {itemType} {itemID} {(autoAccept ? "true " : " ")}%% [#]{mailName}";
            LoggerHelper.VerboseLog($"Created mail data: {mailData}");
            return mailData;
        }

        private string CreateNoteMailData(string textKey, string animalDisplayName)
        {
            var noteMailData =
                $"{TranslationHelper.Get(textKey)} {TranslationHelper.Get("mail.animalAvaliable.newanimal")} {animalDisplayName}**[#]{animalDisplayName} Avaliable";
            LoggerHelper.VerboseLog($"Created note mail data: {noteMailData}");
            return noteMailData;
        }

        private void CreateNoteMail()
        {
            LoggerHelper.VerboseLog("Creating note mail for hatched animals...");
            if (_modSaveData.HatchedAnimals.Any())
            {
                foreach (
                    var animal in _modSaveData.HatchedAnimals.Where(x =>
                        !MailToAdd.ContainsKey(x.MailID)
                    )
                )
                {
                    var displayName = GameAssetManager.GetDisplayName(animal.FarmAnimalName);
                    MailToAdd[animal.MailID] = CreateNoteMailData(
                        $"mail.animalAvaliable{Game1.random.Next(3)}",
                        displayName
                    );
                    if (!AllMail.Contains(animal.MailID))
                    {
                        AllMail.Add(animal.MailID);
                    }
                    LoggerHelper.VerboseLog($"Note mail created for animal: {displayName}");
                }
            }
            LoggerHelper.VerboseLog("Note mail creation completed.");
        }

        internal void AddMail(IDictionary<string, string> data)
        {
            LoggerHelper.VerboseLog("Adding mail...");
            GameAssetManager.PopulateIfEmpty(MailToAdd, () => CreateMail());
            CreateNoteMail();
            data.TryAddMany(MailToAdd);
            LoggerHelper.VerboseLog("Mail added successfully.");
        }

        internal static bool HasMail(string mailID, MailType mailType = MailType.Received)
        {
            LoggerHelper.VerboseLog($"Checking for mail: {mailID} of type: {mailType}");
            bool hasMail = GameStateQuery.CheckConditions(
                PlayerCondition.HasMail(mailID, mailType)
            );
            LoggerHelper.VerboseLog($"Has mail: {mailID} of type: {mailType} - {hasMail}");
            return hasMail;
        }

        internal static bool AddMailToPlayer(
            string mailID,
            MailType mailType,
            bool isFlag = false,
            bool condition = true
        )
        {
            LoggerHelper.VerboseLog($"Adding mail: {mailID} of type: {mailType} to player...");
            bool added = false;
            if (condition && !HasMail(mailID, mailType) && !HasMail(mailID, MailType.Received))
            {
                switch (mailType)
                {
                    case MailType.Tomorrow:
                        Game1.addMailForTomorrow(mailID, isFlag);
                        break;
                    case MailType.Received:
                        Game1.addMail(mailID, isFlag);
                        break;
                    case MailType.Any:
                        Game1.addMailForTomorrow(mailID, isFlag);
                        break;
                    case MailType.Mailbox:
                        Game1.addMail(mailID, isFlag);
                        break;
                }
                added = true;
            }
            LoggerHelper.VerboseLog(
                $"Added mail: {mailID} of type: {mailType} to player - {added}"
            );
            return added;
        }

        internal static void RemoveMailFromPlayer(string mailID)
        {
            LoggerHelper.VerboseLog($"Removing mail: {mailID} from player...");

            Game1.player.mailForTomorrow.Remove(mailID);
            Game1.player.mailbox.Remove(mailID);
            Game1.player.mailReceived.Remove(mailID);
            LoggerHelper.VerboseLog($"Removed mail: {mailID} from player.");
        }
    }
}
