using LetMeHatchThisEgg.Models;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.Integrations
{
    public class Integration
    {
        public Dictionary<string, BreedingDataFemale> BreedingData { get; set; }

        public Integration(IntegrationName integration)
        {
            BreedingData = GetBreedingData(integration);
        }

        private Dictionary<string, BreedingDataFemale> GetBreedingData(IntegrationName integration)
        {
            if (ModEntry.HelperInstance == null)
            {
                return new();
            }
            var jsonPath = integration switch
            {
                IntegrationName.RoosterControlAddon => "data/Integrations/Roosters Hatching and Control Addon/content.json",
                IntegrationName.VanillaGame => "data/Integrations/Vanilla Game/content.json",
                _ => ""
            };
            var breedingData = ModEntry.HelperInstance.Data.ReadJsonFile<
                Dictionary<string, BreedingDataFemale>
            >(jsonPath);
            breedingData ??= CreateNewBreedingData(integration, jsonPath);
            return breedingData;
        }

        private Dictionary<string, BreedingDataFemale> CreateNewBreedingData(
            IntegrationName integration,
            string jsonPath
        )
        {
            if (ModEntry.HelperInstance == null)
            {
                return new();
            }

            var breedingData = integration switch
            {
                IntegrationName.RoosterControlAddon
                    => RoosterControlAddonIntegration.CreateBreedingData(),
                IntegrationName.VanillaGame => VanillaGameIntegration.CreateBreedingData(),
                _ => new()
            };
            ModEntry.HelperInstance.Data.WriteJsonFile(jsonPath, breedingData);
            return breedingData;
        }
    }
}
