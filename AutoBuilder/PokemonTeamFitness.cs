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

            pokemonTeamChromosome.WeightingScores = TeamScorer.CalculateScore(team, _weightings);

            double fitness = pokemonTeamChromosome.WeightingScores.SumWeightings();

            // duplicates in a team don't qualify as a valid team
            // can't have more than one mega-evolved pokemon in a team
            if (
                fitness < 0
                || team.ContainsDuplicates()
                || (!_weightings.AllowMultipleMegas && team.CountMegaPokemon() > 1)
                || (!_weightings.AllowMultipleGmax && team.CountGmaxPokemon() > 1)
            )
                fitness = 0;

            return fitness;
        }
    }
}
