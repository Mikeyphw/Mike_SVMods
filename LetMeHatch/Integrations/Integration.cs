using LetMeHatch.Models;
using static LetMeHatch.EnumsAndStrings.Enums;

using static LetMeHatch.ModEntry;
using LetMeHatch.Infrastructure.Smapi;

namespace LetMeHatch.Integrations
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
#if MIKE_DEV
            var breedingData = ModEntry.HelperInstance.Data.ReadJsonFile<
                Dictionary<string, BreedingDataFemale>
            >(jsonPath);
            breedingData ??= CreateNewBreedingData(integration, jsonPath);
            return breedingData;
#else
            return EmbeddedJson.ReadOrThrow<Dictionary<string, BreedingDataFemale>>(
                ModEntry.HelperInstance,
                jsonPath
            );
#endif
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
#if MIKE_DEV
            ModEntry.HelperInstance.Data.WriteJsonFile(jsonPath, breedingData);
#endif
            return breedingData;
        }
    }
}
