using PokeApiNet;
using System.Text.Json.Serialization;
using Utility;

namespace PokemonDataModel
{
    // this class stores a single pokedex entry which is typically a single pokemon
    // however there are some pokemon with multiple varieties (e.g. rotom) which is
    // not stored in the pokedex so we can use this class to get each variety

    public class SmartPokemonEntry : IPokemonSearchable
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonIgnore]
        public string Name {
            get => SpeciesResource.Name;
            set => SpeciesResource.Name = value;
        }

        [JsonPropertyName("species_resource")]
        public NamedApiResource<PokemonSpecies> SpeciesResource { get; set; }

        [JsonIgnore]
        private PokemonSpecies? Species;

        [JsonConstructor]
        public SmartPokemonEntry(int Id, NamedApiResource<PokemonSpecies> SpeciesResource) 
        {
            this.Id = Id;
            this.SpeciesResource = SpeciesResource;
        }

        public async Task<PokemonSpecies> GetSpecies()
        {
            Species ??= await PokeApiService.Instance!.GetPokemonSpeciesAsync(SpeciesResource.Name);

            return Species ?? throw new Exception($"Could not load pokemon species: {SpeciesResource.Name}");
		}

        public override string ToString()
        {
            return StringUtils.FirstCharToUpper(SpeciesResource.Name);
        }

        IEnumerable<NamedApiResource<Pokemon>> IPokemonSearchable.GetAllVarieties()
        {
            if (Species is null) return [];

            List<NamedApiResource<Pokemon>> varieties = [];

            foreach (PokemonSpeciesVariety variety in Species!.Varieties)
            {
                varieties.Add(variety.Pokemon);
            }

            return varieties;
        }

        public async Task<IEnumerable<NamedApiResource<Pokemon>>> GetAllVarietiesAsync()
        {
            if (Species == null) await GetSpecies();

            List<NamedApiResource<Pokemon>> varieties = [];

            foreach (PokemonSpeciesVariety variety in Species!.Varieties)
            {
                varieties.Add(variety.Pokemon);
            }

            return varieties;
        }
    }

    // an observable collection of SmartPokemonEntry objects which is used as a binding
    // for the pokedex combobox
    public class SmartPokedex : List<SmartPokemonEntry>, ILazyPokemonList
    {
        public SmartPokedex(string name, NamedApiResource<Pokedex> pokedexResource) 
        { 
            Name = name; 
            PokedexResource = pokedexResource;
        }
        public SmartPokedex(string name, NamedApiResource<VersionGroup> versionGroupResource)
        {
            Name = name;
            VersionGroupResource = versionGroupResource;
        }
        public SmartPokedex(string name, Pokedex pokedex) 
        {
            Name = name;
            AddPokedex(pokedex);
        }

        [JsonPropertyName("name")]
        public string Name { get; set; }

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
            Random rand = new Random();
            return this[rand.Next(Count)];
        }

        public async Task<IEnumerable<IPokemonSearchable>> GetListAsync()
        {
            if (this.Count == 0)
            {
                if (PokedexResource is not null)
                {
                    Pokedex? fetchedDex = await PokeApiService.Instance!.GetPokedexAsync(PokedexResource);
                    if (fetchedDex is not null)
                    {
                        AddPokedex(fetchedDex);
                    }
                }
                else if (VersionGroupResource is not null)
                {
                    VersionGroup group = await PokeApiService.Instance!.GetVersionGroupAsync(VersionGroupResource);

                    List<Task<Pokedex?>> pokedexTasks = [];
                    foreach (var pokedex in group.Pokedexes)
                    {
                        pokedexTasks.Add(PokeApiService.Instance.GetPokedexAsync(pokedex));
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
    }
}
