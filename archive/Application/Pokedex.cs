using PokeApiNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace autoteambuilder
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
            return LowercaseToNamecaseConverter.FirstCharToUpper(Species.Name);
        }
    }

    // an observable collection of SmartPokemonEntry objects which is used as a binding
    // for the pokedex combobox
    public class SmartPokedex : ObservableCollection<SmartPokemonEntry>
    {
        public SmartPokedex() { }
        public SmartPokedex(Pokedex pokedex) 
        {
            SetPokedex(pokedex);
        }

        public async void SetPokedex(Pokedex pokedex)
        {
            var tasks = pokedex.PokemonEntries.Select(async entry =>
            {
                PokemonSpecies? species = await PokeApiHandler.GetPokemonSpeciesAsync(entry);
                if (species != null)
                    Add(new SmartPokemonEntry(entry.EntryNumber, species));
            });

            await Task.WhenAll(tasks);
        }

        public bool RemovePokemon(string speciesName)
        {
            SmartPokemonEntry? entry = this.FirstOrDefault(entry => entry.Species.Name == speciesName);
            if (entry == null) return false;
            return Remove(entry);
        }

        public SmartPokemonEntry? FindPokemon(string pokemonName)
        {
            return this.Where(entry => entry.Species.Name.Equals(pokemonName)).FirstOrDefault();
        }

        public SmartPokemonEntry? RandomPokemon()
        {
            Random rand = new Random();
            return this[rand.Next(Count)];
        }
    }
}
