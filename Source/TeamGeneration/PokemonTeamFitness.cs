using GeneticSharp;

namespace pokeAutoBuilder.Source.TeamGeneration
{
    public class PokemonTeamFitness : IFitness
    {
        private readonly AutoBuilderWeightings _weightings;

        public PokemonTeamFitness(AutoBuilderWeightings weightings)
        {
            _weightings = weightings;
        }

        public double Evaluate(IChromosome chromosome)
        {
            PokemonTeam team = (chromosome as PokemonTeamChromosome)!.GetTeam();

            double fitness = AutoBuilder.CalculateScore(team, _weightings);

            // duplicates in a team don't qualify as a valid team
            if (fitness < 0 || team.ContainsDuplicates())
                fitness = 0;

            return fitness;
        }
    }
}
