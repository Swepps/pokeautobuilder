using autoteambuilder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace autoteambuilder
{
    using Type = PokeApiNet.Type;

    class PokemonTeam
    {
        private SmartPokemon?[] Pokemon = new SmartPokemon[6];

        public void SetPokemon(int i, SmartPokemon? p)
        {
            if (i >= 0 && i < Pokemon.Length)
            {
                Pokemon[i] = p;
            }
        }

        public SmartPokemon? GetPokemon(int i)
        {
            if (i >= 0 && i < Pokemon.Length)
                return Pokemon[i];
            else
                return null;
        }

        public int CountPokemon()
        {
            int count = 0;
            foreach (SmartPokemon? p in Pokemon)
            {
                if (p != null) count++;
            }
            return count;
        }

        // these functions should probs have been one function but oh well
        public int CountWeaknesses(string typeName)
        {
            int weaknesses = 0;
            foreach (SmartPokemon? p in Pokemon)
            {
                if (p == null) continue;

                Multipliers multipliers = p.GetMultipliers();

                if (multipliers.defense.TryGetValue(typeName, out double value)
                    &&
                    value > 1.0)
                {
                    weaknesses++;
                }
            }

            return weaknesses;
        }

        public int CountResistances(string typeName)
        {
            int resistances = 0;
            foreach (SmartPokemon? p in Pokemon)
            {
                if (p == null) continue;

                Multipliers multipliers = p.GetMultipliers();

                if (multipliers.defense.TryGetValue(typeName, out double value)
                    &&
                    value < 1.0)
                {
                    resistances++;
                }
            }

            return resistances;
        }

        public IEnumerator GetEnumerator() 
        {
            return Pokemon.GetEnumerator();
        }
    }
}
