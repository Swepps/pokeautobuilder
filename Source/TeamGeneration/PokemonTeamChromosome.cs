using GeneticSharp;

namespace pokeAutoBuilder.Source.TeamGeneration
{
    public class PokemonTeamChromosome : ChromosomeBase
    {
        static Random Random = new Random();

        private readonly PokemonStorage _storage;
        private readonly PokemonTeam _lockedMembers;

        public double Score { get; internal set; }

        public PokemonTeamChromosome(PokemonStorage storage, PokemonTeam lockedMembers) : base (PokemonTeam.MaxTeamSize)
        {
            _storage = storage;
            _lockedMembers = lockedMembers;

            PokemonTeam randomTeam = _storage.GetRandomTeam();

            for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
            {
                ReplaceGene(i, new Gene(randomTeam[i]));
            }
        }

        public override Gene GenerateGene(int geneIndex)
        {
            return new Gene(_storage.GetRandomPokemon());
        }

        public override IChromosome CreateNew()
        {
            return new PokemonTeamChromosome(_storage, _lockedMembers);
        }

        public override IChromosome Clone()
        {
            var clone = new PokemonTeamChromosome(_storage, _lockedMembers);
            var genes = GetGenes();
            for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
            {
                clone.ReplaceGene(i, new Gene((SmartPokemon?)genes[i].Value));
            }
            clone!.Score = Score;

            return clone;
        }

        public PokemonTeam GetTeam()
        {
            var genes = GetGenes();

            // put into a new team then sort it
            PokemonTeam team = new PokemonTeam();
            for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
            {
                team[i] = genes[i].Value as SmartPokemon;
            }
            team.SortById();

            return team;
        }
    }
}
