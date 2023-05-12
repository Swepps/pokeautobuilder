using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static autoteambuilder.PokemonTypes;

namespace autoteambuilder
{
    internal class Pokedex : IList<Pokemon>
    {
        private List<Pokemon> entirePokedex = new List<Pokemon>();
        private List<Pokemon> returnPokedex = new List<Pokemon>();

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
                entirePokedex.Add(pokemon);
            }

            returnPokedex = new List<Pokemon>(entirePokedex);
        }

        public Pokemon this[int index] { get => returnPokedex[index]; set => returnPokedex[index] = value; }

        public int Count => returnPokedex.Count;

        public bool IsReadOnly => false;

        public void Add(Pokemon item)
        {
            if (!entirePokedex.Contains(item))
            {
                entirePokedex.Add(item);
            }
        }

        public void Clear()
        {
            entirePokedex.Clear();
            returnPokedex.Clear();
        }

        public bool Contains(Pokemon item)
        {
            return returnPokedex.Contains(item);
        }

        public void CopyTo(Pokemon[] array, int arrayIndex)
        {
            foreach (Pokemon pokemon in entirePokedex)
            {
                array[arrayIndex++] = pokemon; 
            }
        }

        public IEnumerator<Pokemon> GetEnumerator()
        {
            return returnPokedex.GetEnumerator();
        }

        public int IndexOf(Pokemon item)
        {
            return returnPokedex.IndexOf(item);
        }

        public void Insert(int index, Pokemon item)
        {
            if (!entirePokedex.Contains(item))
            {
                entirePokedex.Insert(index, item);
            }
        }

        public bool Remove(Pokemon item)
        {
            return entirePokedex.Remove(item);
        }

        public void RemoveAt(int index)
        {
            entirePokedex.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return returnPokedex.GetEnumerator();
        }

        public Pokemon RandomPokemon()
        {
            Random rnd = new();
            int randomIdx = rnd.Next(returnPokedex.Count);
            return returnPokedex[randomIdx];
        }
    }
}
