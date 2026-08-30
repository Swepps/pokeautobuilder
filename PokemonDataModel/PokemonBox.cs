using System.Text.Json.Serialization;

namespace PokemonDataModel
{
    public class PokemonBox : ILazyPokemonList
    {
        [JsonPropertyName("pokemon")]
        public List<SmartPokemon> Pokemon { get; set; }

        [JsonPropertyName("rules")]
        public BoxRules Rules { get; set; } = BoxRules.Unrestricted();

        // Remembers which search location (a Pokedex name, per ILazyPokemonList.Name) this box was
        // last browsing on the storage page's "Add" panel, so switching boxes and coming back
        // restores where you were - e.g. adding from Emerald into a Gen 3 box.
        [JsonPropertyName("lastSearchLocationName")]
        public string? LastSearchLocationName { get; set; }

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

        // Warms every Pokemon in this box's multiplier cache for this box's current ruleset, so
        // downstream consumers (the auto-builder's genetic algorithm, coverage/defense panes) can
        // read GetMultipliers/GetResistance/etc. without checking first. Call whenever a Pokemon is
        // added to the box or the box's Rules change - the genetic algorithm never manufactures a
        // Pokemon that wasn't already attached to the box, so warming here is sufficient.
        public void EnsureInitialized(TypeChart baseChart)
        {
            TypeChart chart = baseChart.GetOrBuildDerived(Rules.Id, Rules.DisabledTypes);
            foreach (SmartPokemon pokemon in Pokemon)
            {
                if (!pokemon.HasInitializedRuleset(Rules.Id))
                {
                    pokemon.InitializeTypes(chart, Rules);
                }
            }
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
