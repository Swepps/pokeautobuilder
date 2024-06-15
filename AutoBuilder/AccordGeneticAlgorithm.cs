using Accord.Genetic;
using PokemonDataModel;

namespace AutoBuilder
{
    public class AccordGeneticAlgorithm
    {

        public static PokemonTeam SolvePokemonTeam(PokemonStorage availablePokemon, AutoBuilderWeightings weightings, PokemonTeam lockedMembers)
        {
            Population population = new Population(500, new PokemonTeamChromosome(availablePokemon, weightings, lockedMembers), new PokemonTeamChromosome.FitnessFunction(), new EliteSelection());

            for (int i = 0; i < 100; i++)
            {
                population.RunEpoch();
            }

            return ((PokemonTeamChromosome)population.BestChromosome).Team;
        }

        internal class PokemonTeamChromosome : ChromosomeBase
        {
            static Random Random = new Random();

            private readonly PokemonStorage _storage;
            private readonly PokemonTeam _lockedMembers;
            public readonly AutoBuilderWeightings _weightings;

            public PokemonTeam Team;

            public PokemonTeamChromosome(PokemonStorage storage, AutoBuilderWeightings weightings, PokemonTeam lockedMembers)
            {
                _storage = storage;
                _lockedMembers = lockedMembers;
                _weightings = weightings;

                Team = new PokemonTeam();

                Generate();
            }

            public PokemonTeamChromosome(PokemonTeam team, PokemonStorage storage, PokemonTeam lockedMembers)
            {
                _storage = storage;
                _lockedMembers = lockedMembers;
                Team = new PokemonTeam();
                for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
                {
                    Team.Pokemon[i] = team.Pokemon[i];
                }
            }

            public override IChromosome Clone()
            {
                return new PokemonTeamChromosome(Team, _storage, _lockedMembers);
            }

            public override IChromosome CreateNew()
            {
                PokemonTeamChromosome chrom = new PokemonTeamChromosome(_storage, _weightings, _lockedMembers);
                chrom.Generate();
                return chrom;
            }

            public override void Crossover(IChromosome pair)
            {
                PokemonTeamChromosome? otherChrom = pair as PokemonTeamChromosome;
                if (otherChrom == null)
                    return;

                int randIdx = Random.Next(PokemonTeam.MaxTeamSize);
                for (; randIdx < PokemonTeam.MaxTeamSize; randIdx++)
                {
                    if (!Team.Pokemon.Contains(otherChrom.Team.Pokemon[randIdx]))
                        Team.Pokemon[randIdx] = otherChrom.Team.Pokemon[randIdx];
                }
            }

            public override void Generate()
            {
                Team = _storage.GetRandomTeam(_lockedMembers);
            }

            public override void Mutate()
            {
                // can't mutate if the data set is less than team size
                if (_storage.Pokemon.Count <= PokemonTeam.MaxTeamSize || _lockedMembers.CountPokemon() >= PokemonTeam.MaxTeamSize)
                    return;

                // make sure we don't swap out any locked members
                int randIdx = Random.Next(PokemonTeam.MaxTeamSize);
                while (_lockedMembers.Pokemon[randIdx] != null)
                    randIdx = Random.Next(PokemonTeam.MaxTeamSize);

                // now mutate
                SmartPokemon randPokemon = _storage.GetRandomPokemon();
                while (Team.Pokemon.Contains(randPokemon))
                {
                    randPokemon = _storage.GetRandomPokemon();
                }
                Team.Pokemon[randIdx] = randPokemon;
            }

            public class FitnessFunction : IFitnessFunction
            {
                public double Evaluate(IChromosome chromosome)
                {
                    PokemonTeamChromosome? pokeChrom = chromosome as PokemonTeamChromosome;
                    if (pokeChrom == null)
                        return 0;

                    return AutoBuilder.CalculateScore(pokeChrom.Team, pokeChrom._weightings);
                }
            }
        }
    }
}
