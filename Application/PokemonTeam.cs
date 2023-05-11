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
        private Pokemon[] team = new Pokemon[6];

        public void SetPokemon(int i, Pokemon p)
        {
            if (i >= 0 && i < team.Length)
            {
                team[i] = p;
            }            
        }

        private double CalculateStandardDeviation(Dictionary<PokemonType, int> typeDictionary)
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

            foreach (Pokemon p in team)
            {
                foreach (PokemonType t in typeArray)
                {
                    double typeEffectiveness = p.GetEffectiveness(t);

                    // make sure the maps contains something for this type
                    if (!weaknesses.ContainsKey(t))
                    {
                        weaknesses[t] = 0;
                    }
                    if (!resistances.ContainsKey(t))
                    {
                        resistances[t] = 0;
                    }

                    // use effectiveness of type against this pokemon to determine weakness or resistance
                    if (typeEffectiveness > 1.0)
                    {
                        weaknesses[t]++;
                    }
                    else if (typeEffectiveness < 1.0)
                    {
                        resistances[t]++;
                    }
                }
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
