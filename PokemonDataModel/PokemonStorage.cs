using System.Linq;
using System.Text.Json.Serialization;

namespace PokemonDataModel
{
    public class PokemonStorage : ILazyPokemonList
    {
        [JsonPropertyName("pokemon")]
        public List<SmartPokemon> Pokemon { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        public PokemonStorage()
        {
            Pokemon = [];
        }
        public PokemonStorage(string name) 
        {
            Name = name;
            Pokemon = [];
        }

        // used when getting cached pokemon storage list
        public PokemonStorage(string name, List<SmartPokemon> pokemonList) 
        {
            Name = name;
            Pokemon = pokemonList;
        }

        public PokemonStorage(List<SmartPokemon> pokemonList)
        {
            Name = "Storage";
            Pokemon = pokemonList;
        }

        public PokemonTeam GetRandomTeam(PokemonTeam? lockedMembers = null)
        {
            if (lockedMembers == null)
            {
                lockedMembers = new PokemonTeam();
            }
            else if (lockedMembers.CountPokemon() >= PokemonTeam.MaxTeamSize || Pokemon.Count < PokemonTeam.MaxTeamSize)
            {
                // they're all locked!
                return lockedMembers;
            }

            // generate the random members and stick into an array for later
            Random rand = new();
            int numOfRandMembers = PokemonTeam.MaxTeamSize - lockedMembers.CountPokemon();
            List<SmartPokemon> randomMembers = Pokemon.OrderBy(p => rand.Next()).Take(numOfRandMembers).ToList();

            // create a new team using lockedMembers and random members
            PokemonTeam newTeam = new();
            int randIdx = 0;
            for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
            {
                if (lockedMembers.Pokemon[i] != null)
                {
                    newTeam.Pokemon[i] = lockedMembers.Pokemon[i];
                }
                else if (randIdx < randomMembers.Count)
                {
                    newTeam.Pokemon[i] = randomMembers[randIdx];
                    randIdx++;
                }
            }

            return newTeam;
        }

        public SmartPokemon GetRandomPokemon()
        {
            Random rand = new();
            SmartPokemon randPokemon = Pokemon[rand.Next(0, Pokemon.Count)];
            return randPokemon;
        }

        public Task<IEnumerable<IPokemonSearchable>> GetListAsync()
        {
            return Task.Run(() => Pokemon.AsEnumerable<IPokemonSearchable>());
        }
    }
}
