using GeneticSharp;
using PokemonDataModel;

namespace AutoBuilder
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
            if (chromosome is not PokemonTeamChromosome pokemonTeamChromosome)
                return 0.0;

            PokemonTeam team = pokemonTeamChromosome.GetTeam();

            pokemonTeamChromosome.WeightingScores = AutoBuilder.CalculateScore(team, _weightings);

            double fitness = pokemonTeamChromosome.WeightingScores.SumWeightings();
            // duplicates in a team don't qualify as a valid team
            if (fitness < 0 || team.ContainsDuplicates())
                fitness = 0;

            return fitness;
        }
    }
}
