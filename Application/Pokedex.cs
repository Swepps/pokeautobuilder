using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace autoteambuilder
{
    class Pokedex : List<string>
    {
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
                //OLDPokemonType type1 = (OLDPokemonType)Enum.Parse(typeof(OLDPokemonType), values[3]);
                //OLDPokemonType type2 = OLDPokemonType.None;
                //if (values[4] != "")
                //{
                //    type2 = (OLDPokemonType)Enum.Parse(typeof(OLDPokemonType), values[4]);
                //}

                Add(name);
            }
        }
    }
}
