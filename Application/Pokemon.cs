using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace autoteambuilder
{
    using PokemonType = PokemonTypes.PokemonType; 
    internal class Pokemon
    {
        private int pokedexNum;
        private string name;
        private string variant;
        private PokemonType type1 = PokemonType.Normal;
        private PokemonType type2 = PokemonType.None;

        public Pokemon(int pokedexNum, string name, PokemonType type1, PokemonType type2 = PokemonType.None, string variant = "")
        {
            this.pokedexNum = pokedexNum;
            this.name = name;
            this.type1 = type1;
            this.type2 = type2;
            this.variant = variant;
        }

        public double GetEffectiveness(PokemonType type)
        {
            double effectiveness = PokemonTypes.GetEffectiveness(type, type1);
            if (type2 != PokemonType.None)
                effectiveness *= PokemonTypes.GetEffectiveness(type, type2);

            return effectiveness;
        }
    }
}
