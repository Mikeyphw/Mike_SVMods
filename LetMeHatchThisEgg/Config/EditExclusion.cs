using System.Collections.Generic;
using System.Linq;
using static LetMeHatchThisEgg.EnumsAndStrings.Enums;

using static LetMeHatchThisEgg.ModEntry;

namespace LetMeHatchThisEgg.Config
{
    public sealed class EditExclusion
    {
        public Dictionary<EditType, List<string>> EditExclusions { get; private set; }
        public List<string> ClonedEggsExclusionListEggs { get; set; }

        public EditExclusion()
        {
            EditExclusions = CreateEditExclusions();
            ClonedEggsExclusionListEggs = new() { "437", "439", "857", "680", "413" };
        }

        private Dictionary<EditType, List<string>> CreateEditExclusions()
        {
            var exclusions = new Dictionary<EditType, List<string>>
            {
                {
                    EditType.EggList, new List<string>
                    {
                        "cockwhite, cockbrown, cockblue, cockvoid, cockgold",
                        "River Lung",
                        "Mountain Lung",
                        "Lava Lung",
                        "Storm Lung",
                        "Pond Lung",
                        "Creek Lung",
                        "Sky Lung",
                        "Forest Lung",
                        "Flower Lung",
                        "Spectre Lung",
                        "Abyss Lung",
                        "Celestial Lung",
                        "Time Lung",
                        "Crystal Lung",
                        "Phase Lung",
                        "Radioactive Lung",
                        "Pygmy Lung",
                        "Meadowlands Lung",
                        "Dune Lung",
                        "ExampleAnimalKey1",
                        "ExampleAnimalKey2",
                        "ExampleAnimalKey3",
                        "OR",
                        "ExampleAnimalKey1, ExampleAnimalKey2, ExampleAnimalKey3"
                    }
                },
                {
                    EditType.BirthText, new List<string>
                    {
                        "ExampleAnimalKey1",
                        "ExampleAnimalKey2",
                        "ExampleAnimalKey3",
                        "OR",
                        "ExampleAnimalKey1, ExampleAnimalKey2, ExampleAnimalKey3"
                    }
                },
                {
                    EditType.CloneEggs, new List<string>
                    {
                        "cockwhite, cockbrown, cockblue, cockvoid, cockgold",
                        "Rooster",
                        "River Lung",
                        "Mountain Lung",
                        "Lava Lung",
                        "Storm Lung",
                        "Pond Lung",
                        "Creek Lung",
                        "Sky Lung",
                        "Forest Lung",
                        "Flower Lung",
                        "Spectre Lung",
                        "Abyss Lung",
                        "Celestial Lung",
                        "Time Lung",
                        "Crystal Lung",
                        "Phase Lung",
                        "Radioactive Lung",
                        "Pygmy Lung",
                        "Meadowlands Lung",
                        "Dune Lung",
                        "ExampleAnimalKey1",
                        "ExampleAnimalKey2",
                        "ExampleAnimalKey3",
                        "OR",
                        "ExampleAnimalKey1, ExampleAnimalKey2, ExampleAnimalKey3"
                    }
                }
            };

            return exclusions;
        }
    }
}