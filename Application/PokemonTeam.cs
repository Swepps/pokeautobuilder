using autoteambuilder;
using System;
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

        // Aah GCSE maths... this seems much easier than I thought it was when I was 15
        private static double CalculateStandardDeviation(Dictionary<Type, int> typeDictionary)
        {
            List<Type> typeArray = MainWindow.AllTypes;
            double mean = 0;
            foreach (Type type in typeArray) 
            {
                mean += typeDictionary[type];
            }
            mean /= typeArray.Count;

            double variance = 0;
            foreach (Type type in typeArray)
            {
                variance += Math.Pow((typeDictionary[type] - mean), 2);
            }
            variance /= typeArray.Count;

            return Math.Sqrt(variance);
        }

        public double CalculateWeighting()
        {
            double weighting = 0;

            List<Type> typeArray = MainWindow.AllTypes;
            Dictionary<Type, int> weaknesses = new Dictionary<Type, int>();
            Dictionary<Type, int> resistances = new Dictionary<Type, int>();

            foreach (Type t in typeArray)
            {
                weaknesses[t] = CountWeaknesses(t.Name);
                resistances[t] = CountResistances(t.Name);
            }

            double weaknessesSD = CalculateStandardDeviation(weaknesses);
            double resistancesSD = CalculateStandardDeviation(resistances);

            weighting += 3 - weaknessesSD;
            weighting += 3 - resistancesSD;
            weighting *= ((double)CountPokemon() / 6.0);

            // for now we're just saying that a balanced spread of resistances and weakness is best

            return weighting;
        }
    }
}
