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
    [Serializable]
    [XmlRoot(ElementName = "Entry")]
    public class PokedexEntry
    {
        public string Name { get; set; }

        [XmlIgnore]
        public SmartPokemon? Pokemon { get; set; }

        public PokedexEntry() 
        {
            Name = "";
            Pokemon = null;
        }
        public PokedexEntry(string Name) 
        { 
            this.Name = Name.Trim();
            Pokemon = null;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    [Serializable]
    [XmlRoot(ElementName = "PokemonList")]
    public class Pokedex : ObservableCollection<PokedexEntry>
    {
        public Pokedex() { }
        public Pokedex(StringReader reader)
        {
            if (reader == null)
                return;

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line == null) continue;

                string[] values = line.Split(',');

                // Parse the values from the CSV line
                int pokedexNum = int.Parse(values[0]);
                string name = values[1];

                Add(new PokedexEntry(name));
            }
        }

        // uses the list of pokemon names to populate the dictionary with all the pokemon in this pokedex
        public async Task DownloadPokemon()
        {
            var tasks = this.Select(async pokedexEntry =>
            {
                if (pokedexEntry.Pokemon == null)
                {
                    SmartPokemon? pokemon = await PokeApiHandler.GetPokemonAsync(pokedexEntry.Name);
                    if (pokemon != null)
                    {
                        pokedexEntry.Pokemon = pokemon;
                    }
                }
            });

            await Task.WhenAll(tasks);
        }

        public bool RemovePokemon(string pokemonName)
        {
            PokedexEntry? pokedexEntry = this.FirstOrDefault(entry => entry.Name == pokemonName);
            if (pokedexEntry == null) return false;
            return Remove(pokedexEntry);
        }

        public PokedexEntry? FindPokemon(string pokemonName)
        {
            return this.Where(entry => entry.Name.Equals(pokemonName)).FirstOrDefault();
        }
    }
}
