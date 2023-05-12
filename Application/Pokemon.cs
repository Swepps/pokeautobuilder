using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace autoteambuilder
{
    using PokemonType = PokemonTypes.PokemonType; 
    internal class Pokemon
    {
        private int pokedexNum = -1;
        private string name = "";
        private string variant = "";
        private PokemonType type1 = PokemonType.Normal;
        private PokemonType type2 = PokemonType.None;

        public Pokemon(int pokedexNum, string name, PokemonType type1, PokemonType type2 = PokemonType.None, string variant = "")
        {
            PokedexNum = pokedexNum;
            Name = name;
            Type1 = type1;
            Type2 = type2;
            Variant = variant;
        }

        public string Name { get => name; set => name = value; }
        public int PokedexNum { get => pokedexNum; set => pokedexNum = value; }
        public string Variant { get => variant; set => variant = value; }
        internal PokemonType Type1 { get => type1; set => type1 = value; }
        internal PokemonType Type2 { get => type2; set => type2 = value; }

        public double GetEffectiveness(PokemonType type)
        {
            double effectiveness = PokemonTypes.GetEffectiveness(type, Type1);
            if (Type2 != PokemonType.None)
                effectiveness *= PokemonTypes.GetEffectiveness(type, Type2);

            return effectiveness;
        }

        public override string ToString()
        {
            string name = Name;
            if (Variant != "")
                name += " - " + Variant;

            return name;
        }
    }
}
