using System.Linq;
using System.Text.Json.Serialization;

namespace PokemonDataModel
{
    public class PokemonBox : ILazyPokemonList
    {
        [JsonPropertyName("pokemon")]
        public List<SmartPokemon> Pokemon { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        public PokemonBox()
        {
            Pokemon = [];
        }

        public PokemonBox(string name)
        {
            Name = name;
            Pokemon = [];
        }

        public PokemonBox(string name, List<SmartPokemon> pokemonList)
        {
            Name = name;
            Pokemon = pokemonList;
        }

        public PokemonBox(List<SmartPokemon> pokemonList)
        {
            Name = "Box";
            Pokemon = pokemonList;
        }

        public SmartPokemon GetRandomPokemon()
        {
            Random rand = new();
            SmartPokemon randPokemon = Pokemon[rand.Next(0, Pokemon.Count)];
            return randPokemon;
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

        public Task<IEnumerable<IPokemonSearchable>> GetListAsync()
        {
            return Task.Run(() => Pokemon.AsEnumerable<IPokemonSearchable>());
        }
    }

    public class PokemonStorage
    {
        [JsonPropertyName("boxes")]
        public List<PokemonBox> Boxes { get; set; }

        public PokemonStorage()
        {
            Boxes = [];
        }
    }
}
