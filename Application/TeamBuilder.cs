using PokeApiNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace autoteambuilder
{
    using Type = PokeApiNet.Type;

    internal class TeamBuilder
    {

        // The main juice of the team building. this is what decides how good a team is
        public static double CalculateScore(PokemonTeam team, double typeDefenseWeighting, double typeCoverageWeighting, Dictionary<string, double> baseStatsWeighting)
        {
            // gather some information about the types in the team
            List<Type> typeArray = MainWindow.AllTypes;
            Dictionary<Type, int> weaknesses = new Dictionary<Type, int>();
            Dictionary<Type, int> resistances = new Dictionary<Type, int>();
            Dictionary<Type, int> coverage = new Dictionary<Type, int>();

            double totalWeaknesses = 0;
            double totalResistances = 0;
            double totalCoverage = 0;
            foreach (Type t in typeArray)
            {
                totalWeaknesses += weaknesses[t] = team.CountWeaknesses(t.Name);
                totalResistances += resistances[t] = team.CountResistances(t.Name);
                totalCoverage += coverage[t] = team.CountCoverage(t.Name);
            }

            // defense score
            double defenseScore = 0;
            if (typeDefenseWeighting > 0)
            {
                double weaknessesSD = CalculateStandardDeviation(weaknesses);
                double resistancesSD = CalculateStandardDeviation(resistances);

                defenseScore = 10 - (weaknessesSD + (2 * resistancesSD) + (totalWeaknesses / totalResistances));
                defenseScore *= typeDefenseWeighting;
            }

            // coverage score
            double coverageScore = 0;
            if (typeCoverageWeighting > 0)
            {
                coverageScore = CalculateCoverageScore(coverage, totalCoverage);
                coverageScore *= typeCoverageWeighting;
            }

            double statsScore = CalculateStatsScore(team, baseStatsWeighting);

            double score = defenseScore + coverageScore + statsScore;
            score *= team.CountPokemon() / 6.0;

            return score;
        }

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

        private static double CalculateCoverageScore(Dictionary<Type, int> typeDictionary, double totalCoverage)
        {
            List<Type> typeArray = MainWindow.AllTypes;
            double score = 5.0 + Math.Sqrt(totalCoverage);
            double scoreForType = score / typeArray.Count;

            // give up to 10 points for having at least 1 coverage of each type
            foreach (Type type in typeArray)
            {
                if (!typeDictionary.ContainsKey(type))
                    score -= scoreForType;
            }

            // deduct points for having a spread of coverage with high variance
            score -= CalculateStandardDeviation(typeDictionary);

            return score;
        }

        private static double CalculateStatsScore(PokemonTeam team, Dictionary<string, double> statWeightings)
        {
            double score = 0;

            Dictionary<string, int> statTotals = new Dictionary<string, int>();

            foreach (SmartPokemon? pokemon in team)
            {
                if (pokemon == null)
                    continue;

                foreach (PokemonStat stat in  pokemon.Stats)
                {
                    if (statTotals.ContainsKey(stat.Stat.Name))
                        statTotals[stat.Stat.Name] += stat.BaseStat;
                    else
                        statTotals[stat.Stat.Name] = stat.BaseStat;
                }
            }

            foreach (KeyValuePair<string, int> kvp in statTotals)
            {
                score += (kvp.Value / 300.0) * statWeightings[kvp.Key];
            }

            return score;
        }

        public static PokemonTeam BuildTeam(ObservableCollection<SmartPokemon> availablePokemon, PokemonTeam? lockedMembers, double defenseWeighting, double coverageWeighting, Dictionary<string, double> baseStatsWeighting)
        {
            if (lockedMembers == null)
            {
                lockedMembers = new PokemonTeam();
            }
            else if (lockedMembers.CountPokemon() >= 6 || availablePokemon.Count < 6)
            {
                // they're all locked!
                return lockedMembers;
            }

            PokemonTeam bestTeam = new PokemonTeam();
            double bestTeamScore = 0;

            // using the clever combinations code to quickly iterate through each combination of teams
            // we are selecting (6 - lockedMembers.Count) from all the pokemon in the box
            foreach (SmartPokemon[] remainingTeamCombination in Combinations<SmartPokemon>(availablePokemon.ToArray(), 6 - lockedMembers.CountPokemon()))
            {
                // check that we're not already using one of these selected pokemon in our team already
                // this is inefficient but the lists will always be small so should be pretty quick
                foreach (SmartPokemon entry in remainingTeamCombination)
                {
                    foreach (SmartPokemon? pokemon in lockedMembers)
                    {
                        if (entry == pokemon)
                            continue;
                    }
                }

                // create a new team using lockedMembers and chosen combination
                PokemonTeam newTeam = new PokemonTeam();
                int combinIdx = 0;
                for (int i = 0; i < 6; i++)
                {
                    if (lockedMembers[i] != null)
                    {
                        newTeam[i] = lockedMembers[i];
                    }
                    else if (combinIdx < remainingTeamCombination.Length)
                    {
                        newTeam[i] = remainingTeamCombination[combinIdx];
                        combinIdx++;
                    }
                }

                // check to see if new team is better
                double newTeamScore = CalculateScore(newTeam, defenseWeighting, coverageWeighting, baseStatsWeighting);
                if (newTeamScore > bestTeamScore)
                {
                    bestTeam = newTeam;
                    bestTeamScore = newTeamScore;
                }
            }

            return bestTeam;
        }
    }
}
