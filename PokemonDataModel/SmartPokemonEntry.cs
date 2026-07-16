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

        public async Task<PokemonSpecies> GetSpecies(PokeApiService apiService)
        {
            Species ??= await apiService.GetPokemonSpeciesAsync(SpeciesResource.Name);

            return Species ?? throw new Exception($"Could not load pokemon species: {SpeciesResource.Name}");
		}

        public override string ToString()
        {
            return StringUtils.PrettifyString(SpeciesResource.Name);
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

        public async Task<IEnumerable<NamedApiResource<Pokemon>>> GetAllVarietiesAsync(
            PokeApiService apiService
        )
        {
            if (Species == null) await GetSpecies(apiService);

            List<NamedApiResource<Pokemon>> varieties = [];

            foreach (PokemonSpeciesVariety variety in Species!.Varieties)
            {
                varieties.Add(variety.Pokemon);
            }

            return varieties;
        }
    }
}
