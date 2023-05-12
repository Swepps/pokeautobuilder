using autoteambuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace autoteambuilder
{
    using PokemonType = PokemonTypes.PokemonType;
    class PokemonTeam
    {
        public Pokemon[] Pokemon = new Pokemon[6];

        public void SetPokemon(int i, Pokemon p)
        {
            if (i >= 0 && i < Pokemon.Length)
            {
                Pokemon[i] = p;
            }            
        }

        public int CountWeaknesses(PokemonType type)
        {
            int weaknesses = 0;
            foreach (Pokemon p in Pokemon)
            {
                if (p == null) continue;

                if (p.GetEffectiveness(type) > 1.0)
                    weaknesses++;
            }

            return weaknesses;
        }

        public int CountResistances(PokemonType type)
        {
            int resistances = 0;
            foreach (Pokemon p in Pokemon)
            {
                if (p == null) continue;

                if (p.GetEffectiveness(type) < 1.0)
                    resistances++;
            }

            return resistances;
        }

        // Aah GCSE maths... this seems much easier than I thought it was when I was 15
        private static double CalculateStandardDeviation(Dictionary<PokemonType, int> typeDictionary)
        {
            PokemonType[] typeArray = PokemonTypes.GetTypeArray();
            double mean = 0;
            foreach (PokemonType type in typeArray) 
            {
                mean += typeDictionary[type];
            }
            mean /= typeArray.Length;

            double variance = 0;
            foreach (PokemonType type in typeArray)
            {
                variance += Math.Pow((typeDictionary[type] - mean), 2);
            }
            variance /= typeArray.Length;

            return Math.Sqrt(variance);
        }

        public double CalculateWeighting()
        {
            double weighting = 0;

            PokemonType[] typeArray = PokemonTypes.GetTypeArray();
            Dictionary<PokemonType, int> weaknesses = new Dictionary<PokemonType, int>();
            Dictionary<PokemonType, int> resistances = new Dictionary<PokemonType, int>();

            foreach (PokemonType t in typeArray)
            {
                weaknesses[t] = CountWeaknesses(t);
                resistances[t] = CountResistances(t);
            }

            double weaknessesSD = CalculateStandardDeviation(weaknesses);
            double resistancesSD = CalculateStandardDeviation(resistances);

            weighting += 3 - weaknessesSD;
            weighting += 3 - resistancesSD;

            // for now we're just saying that a balanced spread of resistances and weakness is best

            return weighting;
        }
    }
}
