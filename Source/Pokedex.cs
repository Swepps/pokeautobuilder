using Newtonsoft.Json;
using PokeApiNet;
using pokeAutoBuilder.Source.Services;
using System.Collections.ObjectModel;

namespace pokeAutoBuilder.Source
{
    // this class stores a single pokedex entry which is typically a single pokemon
    // however there are some pokemon with multiple varieties (e.g. rotom) which is
    // not stored in the pokedex so we can use this class to get each variety

    public class SmartPokemonEntry
    {
        public int Id { get; set; }
        public PokemonSpecies Species { get; set; }

        public SmartPokemonEntry(int Id, PokemonSpecies Species) 
        {
            this.Id = Id;
            this.Species = Species;
        }

        public List<NamedApiResource<Pokemon>> GetAllVarieties()
        {
            List<NamedApiResource<Pokemon>> varieties = new List<NamedApiResource<Pokemon>>();

            foreach (PokemonSpeciesVariety variety in Species.Varieties)
            {
                varieties.Add(variety.Pokemon);
            }

            return varieties;
        }

        public override string ToString()
        {
            return StringUtils.FirstCharToUpper(Species.Name);
        }
    }

    // an observable collection of SmartPokemonEntry objects which is used as a binding
    // for the pokedex combobox
    public class SmartPokedex : List<SmartPokemonEntry>
    {
        public SmartPokedex() { }
        public SmartPokedex(Pokedex pokedex) 
        {
            SetPokedex(pokedex);
        }

        public async void SetPokedex(Pokedex pokedex)
        {
            this.Clear();
            var tasks = pokedex.PokemonEntries.Select(async entry =>
            {
                PokemonSpecies? species = await PokeApiService.Instance!.GetPokemonSpeciesAsync(entry);
                if (species != null)
                    Add(new SmartPokemonEntry(entry.EntryNumber, species));
            });

            await Task.WhenAll(tasks);

            // make sure the list is in Pokedex entry order
            Sort((a, b) => a.Id.CompareTo(b.Id));
        }

        public bool RemovePokemon(string speciesName)
        {
            SmartPokemonEntry? entry = this.FirstOrDefault(entry => entry.Species.Name == speciesName);
            if (entry == null) return false;
            return Remove(entry);
        }

        public SmartPokemonEntry? FindPokemon(string speciesName)
        {
            return this.Where(entry => entry.Species.Name.Equals(speciesName)).FirstOrDefault();
        }

        public List<SmartPokemonEntry> SearchPokedex(string searchTerm)
        {
            List<SmartPokemonEntry> results = this.Where(entry => entry.Species.Name.Contains(searchTerm)).OrderBy(e => e.Species.Name).ToList();
            return results;
        }

        public SmartPokemonEntry? RandomPokemon()
        {
            Random rand = new Random();
            return this[rand.Next(Count)];
        }
    }
}
