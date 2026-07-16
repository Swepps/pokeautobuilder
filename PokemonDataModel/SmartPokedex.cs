using PokeApiNet;
using System.Text.Json.Serialization;

namespace PokemonDataModel
{
    // an observable collection of SmartPokemonEntry objects which is used as a binding
    // for the pokedex combobox
    public class SmartPokedex : List<SmartPokemonEntry>, ILazyPokemonList
    {
        // SmartPokedex is never persisted (unlike PokemonBox, its sibling ILazyPokemonList
        // implementer) - it's rebuilt fresh every session from MainLayout, which always has DI
        // access - so it can take PokeApiService via constructor injection rather than needing it
        // threaded through GetListAsync as a parameter (which would force a signature change on
        // the shared ILazyPokemonList interface, including PokemonBox which doesn't need it).
        public SmartPokedex(PokeApiService apiService, string name, NamedApiResource<Pokedex> pokedexResource)
        {
            _apiService = apiService;
            Name = name;
            PokedexResource = pokedexResource;
        }
        public SmartPokedex(PokeApiService apiService, string name, NamedApiResource<VersionGroup> versionGroupResource)
        {
            _apiService = apiService;
            Name = name;
            VersionGroupResource = versionGroupResource;
        }
        public SmartPokedex(PokeApiService apiService, string name, Pokedex pokedex)
        {
            _apiService = apiService;
            Name = name;
            AddPokedex(pokedex);
        }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        private readonly PokeApiService _apiService;
        private readonly NamedApiResource<Pokedex>? PokedexResource;
        private readonly NamedApiResource<VersionGroup>? VersionGroupResource;

        public void AddPokedex(Pokedex pokedex)
        {
            foreach (var entry in pokedex.PokemonEntries)
            {
                Add(new SmartPokemonEntry(entry.EntryNumber, entry.PokemonSpecies));
			}

            // make sure the list is in Pokedex entry order
            Sort((a, b) => a.Id.CompareTo(b.Id));
        }

        public bool RemovePokemon(string speciesName)
        {
            SmartPokemonEntry? entry = this.FirstOrDefault(entry => entry.Name == speciesName);
            if (entry == null) return false;
            return Remove(entry);
        }

        public SmartPokemonEntry? FindPokemon(string speciesName)
        {
            return this.Where(entry => entry.Name.Equals(speciesName)).FirstOrDefault();
        }

        public List<SmartPokemonEntry> SearchPokedex(string searchTerm)
        {
            List<SmartPokemonEntry> results = this.Where(entry => entry.Name.Contains(searchTerm)).OrderBy(e => e.Name).ToList();
            return results;
        }

        public SmartPokemonEntry? RandomPokemon()
        {
            return this[Random.Shared.Next(Count)];
        }

        public async Task<IEnumerable<IPokemonSearchable>> GetListAsync()
        {
            if (this.Count == 0)
            {
                if (PokedexResource is not null)
                {
                    Pokedex? fetchedDex = await _apiService.GetPokedexAsync(PokedexResource);
                    if (fetchedDex is not null)
                    {
                        AddPokedex(fetchedDex);
                    }
                }
                else if (VersionGroupResource is not null)
                {
                    VersionGroup group = await _apiService.GetVersionGroupAsync(VersionGroupResource);

                    List<Task<Pokedex?>> pokedexTasks = [];
                    foreach (var pokedex in group.Pokedexes)
                    {
                        pokedexTasks.Add(_apiService.GetPokedexAsync(pokedex));
                    }
                    await Task.WhenAll(pokedexTasks);

                    foreach (var pokedexTask in pokedexTasks)
                    {
                        if (pokedexTask.Result is not null)
                            AddPokedex(pokedexTask.Result);
                    }
                }
            }

            return this;
        }

        // assume a pokedex is never empty
        public bool IsEmpty()
        {
            return false;
        }
    }
}
