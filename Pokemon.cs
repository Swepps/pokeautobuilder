using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonAutoTeamBuilder
{
    using PokemonType = PokemonTypes.PokemonType; 
    internal class Pokemon
    {
        private int pokedexNum;
        private string name;
        private string variant;
        private PokemonType type1 = PokemonType.Normal;
        private PokemonType type2 = PokemonType.None;

        Pokemon(int pokedexNum, string name, PokemonType type1, PokemonType type2 = PokemonType.None, string variant = "")
        {
            this.pokedexNum = pokedexNum;
            this.name = name;
            this.type1 = type1;
            this.type2 = type2;
            this.variant = variant;
        }
    }
}
