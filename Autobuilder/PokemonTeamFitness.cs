using GeneticSharp;
using PokemonDataModel;

namespace Autobuilder
{
    public class PokemonTeamFitness : IFitness
    {
        private readonly AutobuilderWeightings _weightings;

        public PokemonTeamFitness(AutobuilderWeightings weightings)
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

            // duplicates in a team don't qualify as a valid team. AllowMegas/AllowGmax being false
            // bans the mechanic outright (any count > 0), which supersedes AllowMultipleMegas/Gmax
            // (only relevant once the mechanic is allowed at all)
            bool tooManyMegas = _weightings.AllowMegas
                ? !_weightings.AllowMultipleMegas && team.CountMegaPokemon() > 1
                : team.CountMegaPokemon() > 0;
            bool tooManyGmax = _weightings.AllowGmax
                ? !_weightings.AllowMultipleGmax && team.CountGmaxPokemon() > 1
                : team.CountGmaxPokemon() > 0;

            if (fitness < 0 || team.ContainsDuplicates() || tooManyMegas || tooManyGmax)
                fitness = 0;

            return fitness;
        }
    }
}
