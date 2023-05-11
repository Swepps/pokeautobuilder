using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static autoteambuilder.PokemonTypes;

namespace autoteambuilder
{
    internal class Pokedex
    {
        private List<Pokemon> pokemonList = new List<Pokemon>();

        public Pokedex(StringReader reader)
        {
            if (reader == null)
                return;

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] values = line.Split(',');

                // Parse the values from the CSV line
                int pokedexNum = int.Parse(values[0]);
                string name = values[1];
                string variant = values[2];
                PokemonType type1 = (PokemonType)Enum.Parse(typeof(PokemonType), values[3]);
                PokemonType type2 = PokemonType.None;
                if (values[4] != "")
                {
                    type2 = (PokemonType)Enum.Parse(typeof(PokemonType), values[4]);
                }

                // Create a new Pokemon object
                Pokemon pokemon = new(pokedexNum, name, type1, type2, variant);

                // Add the Pokemon to the list
                PokemonList.Add(pokemon);
            }
        }

        internal List<Pokemon> PokemonList { get => pokemonList; set => pokemonList = value; }

        public Pokemon RandomPokemon()
        {
            Random rnd = new();
            int randomIdx = rnd.Next(PokemonList.Count);
            return PokemonList[randomIdx];
        }
    }
}
