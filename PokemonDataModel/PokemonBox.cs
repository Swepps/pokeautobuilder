using System.Text.Json.Serialization;

namespace PokemonDataModel
{
    public class PokemonBox : ILazyPokemonList
    {
        [JsonPropertyName("pokemon")]
        public List<SmartPokemon> Pokemon { get; set; }

        private string _name = string.Empty;
        [JsonPropertyName("name")]
        public string Name
        {
            get
            {
                if (String.IsNullOrEmpty(_name))
                    return "Unnamed Box";

                return _name;
            }
            set
            {
                _name = value;
            }
        }

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
            SmartPokemon randPokemon = Pokemon[Random.Shared.Next(0, Pokemon.Count)];
            return randPokemon;
        }

        public PokemonTeam GetRandomTeam(PokemonTeam? lockedMembers = null)
        {
            if (lockedMembers == null)
            {
                lockedMembers = new PokemonTeam();
            }
            else if (!lockedMembers.Pokemon.Any(p => p == null)
                || Pokemon.Count < PokemonTeam.MaxTeamSize)
            {
                // they're all locked!
                return lockedMembers;
            }

            // generate the random members and stick into an array for later
            int numOfRandMembers = PokemonTeam.MaxTeamSize - lockedMembers.CountPokemon();
            List<SmartPokemon> randomMembers = Pokemon.OrderBy(p => Random.Shared.Next()).Take(numOfRandMembers).ToList();

            // create a new team using lockedMembers and random members
            PokemonTeam newTeam = new();
            int randIdx = 0;
            for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
            {
                if (lockedMembers.Pokemon.Count > i
                    && lockedMembers.Pokemon[i] != null)
                {
                    newTeam.Pokemon.Add(lockedMembers.Pokemon[i]);
                }
                else if (randIdx < randomMembers.Count)
                {
                    newTeam.Pokemon.Add(randomMembers[randIdx]);
                    randIdx++;
                }
            }

            return newTeam;
        }

        public Task<IEnumerable<IPokemonSearchable>> GetListAsync()
        {
            return Task.Run(() => Pokemon.AsEnumerable<IPokemonSearchable>());
        }

        public bool IsEmpty()
        {
            return Pokemon.Count == 0;
        }
    }
}
