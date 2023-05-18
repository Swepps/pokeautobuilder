using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace autoteambuilder
{
    using Type = PokeApiNet.Type;

    internal class TeamBuilder
    {
        // clever piece of combinations code I stole from the internet!

        // Enumerate all possible m-size combinations of [0, 1, ..., n-1] array
        // in lexicographic order (first [0, 1, 2, ..., m-1]).
        private static IEnumerable<int[]> Combinations(int m, int n)
        {
            int[] result = new int[m];
            Stack<int> stack = new Stack<int>(m);
            stack.Push(0);
            while (stack.Count > 0)
            {
                int index = stack.Count - 1;
                int value = stack.Pop();
                while (value < n)
                {
                    result[index++] = value++;
                    stack.Push(value);
                    if (index != m) continue;
                    yield return (int[])result.Clone(); // thanks to @xanatos
                                                        //yield return result;
                    break;
                }
            }
        }

        public static IEnumerable<T[]> Combinations<T>(T[] array, int m)
        {
            if (array.Length < m)
                throw new ArgumentException("Array length can't be less than number of selected elements");
            if (m < 1)
                throw new ArgumentException("Number of selected elements can't be less than 1");
            T[] result = new T[m];
            foreach (int[] j in Combinations(m, array.Length))
            {
                for (int i = 0; i < m; i++)
                {
                    result[i] = array[j[i]];
                }
                yield return result;
            }
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

        public static double CalculateWeighting(PokemonTeam team)
        {
            double weighting = 0;

            List<Type> typeArray = MainWindow.AllTypes;
            Dictionary<Type, int> weaknesses = new Dictionary<Type, int>();
            Dictionary<Type, int> resistances = new Dictionary<Type, int>();

            double totalWeaknesses = 0;
            double totalResistances = 0;
            foreach (Type t in typeArray)
            {
                totalWeaknesses += weaknesses[t] = team.CountWeaknesses(t.Name);
                totalResistances += resistances[t] = team.CountResistances(t.Name);
            }

            double weaknessesSD = CalculateStandardDeviation(weaknesses);
            double resistancesSD = CalculateStandardDeviation(resistances);

            weighting += 3 - weaknessesSD;
            weighting += 3 - resistancesSD;
            weighting *= (team.CountPokemon() / 6.0);

            // for now we're just saying that a balanced spread of resistances and weakness is best

            return weighting;
        }

        public static PokemonTeam BuildTeam(Pokedex availablePokemon, PokemonTeam? lockedMembers = null)
        {
            if (lockedMembers == null)
            {
                lockedMembers = new PokemonTeam();
            }
            else if (lockedMembers.CountPokemon() >= 6)
            {
                // they're all locked!
                return lockedMembers;
            }

            PokemonTeam bestTeam = new PokemonTeam();
            double bestTeamWeighting = 0;

            // using the clever combinations code to quickly iterate through each combination of teams
            // we are selecting (6 - lockedMembers.Count) from all the pokemon in the box
            foreach (PokedexEntry[] team in Combinations<PokedexEntry>(availablePokemon.ToArray(), 6 - lockedMembers.CountPokemon()))
            {
                // check that we're not already using one of these selected pokemon in our team already
                foreach (PokedexEntry entry in team)
                {
                    foreach (SmartPokemon pokemon in lockedMembers)
                    {
                        if (entry.Pokemon == pokemon)
                            continue;
                    }
                }

                // create a new team using lockedMembers and chosen combination
                PokemonTeam newTeam = new PokemonTeam();
                int combinIdx = 0;
                for (int i = 0; i < 6; i++)
                {
                    if (lockedMembers.GetPokemon(i) != null)
                    {
                        newTeam.SetPokemon(i, lockedMembers.GetPokemon(i));
                    }
                    else if (combinIdx < team.Length)
                    {
                        newTeam.SetPokemon(i, team[combinIdx].Pokemon);
                        combinIdx++;
                    }
                }

                // check to see if new team is better
                double newTeamWeighting = CalculateWeighting(newTeam);
                if (newTeamWeighting > bestTeamWeighting)
                {
                    bestTeam = newTeam;
                    bestTeamWeighting = newTeamWeighting;
                }
            }

            return bestTeam;
        }
    }
}
