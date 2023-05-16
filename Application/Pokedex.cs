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

                Add(name);
            }
        }
    }
}
