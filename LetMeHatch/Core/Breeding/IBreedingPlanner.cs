namespace LetMeHatch.Core.Breeding;

/// <summary>
/// Builds a pure-data <see cref="BreedingPlan"/> from <see cref="BreedingPlanInput"/>.
/// </summary>
public interface IBreedingPlanner
{
    BreedingPlan BuildPlan(BreedingPlanInput input);
}
