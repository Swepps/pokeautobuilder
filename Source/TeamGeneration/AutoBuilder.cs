using Accord.Genetic;
using Microsoft.AspNetCore.Components;
using PokeApiNet;

namespace pokeAutoBuilder.Source.TeamGeneration
{
    using Type = PokeApiNet.Type;

    public struct AutoBuilderWeightings
    {
        // sum of all weightings. some bools are worth 2
        public static readonly double MaxPossibleScore = 13.0;

        // priorities
        public bool ResistantAll;
        public bool STABCoverageAll;
        public bool CoverageOnOffensive;
        public bool ResistancesOnDefensive;

        // balance weightings
        public double MoveSetBalanceWeighting;
        public double StabBalanceWeighting;
        public double ResistanceBalanceWeighting;
        public double WeaknessBalanceWeighting;

        // base stats ( total = 3.0 )
        public double BaseStatTotalWeighting;
        public double BaseStatHpWeighting;
        public double BaseStatAttWeighting;
        public double BaseStatDefWeighting;
        public double BaseStatSpAttWeighting;
        public double BaseStatSpDefWeighting;
        public double BaseStatSpeWeighting;

        public AutoBuilderWeightings(
            bool resistantAll, bool stabCoverageAll, bool coverageOnOffensive, bool resistancesOnDefensive,
            double moveSetBalanceWeighting, double stabBalanceWeighting,
            double resistanceBalanceWeighting, double weaknessBalanceWeighting,
            double baseStatTotalWeighting, 
            double baseStatHpWeighting, double baseStatAttWeighting, double baseStatDefWeighting, 
            double baseStatSpAttWeighting, double baseStatSpDefWeighting, double baseStatSpeWeighting)
        {
            ResistantAll = resistantAll;
            STABCoverageAll = stabCoverageAll;
            CoverageOnOffensive = coverageOnOffensive;
            ResistancesOnDefensive = resistancesOnDefensive;

            MoveSetBalanceWeighting = moveSetBalanceWeighting;
            StabBalanceWeighting = stabBalanceWeighting;
            ResistanceBalanceWeighting = resistanceBalanceWeighting;
            WeaknessBalanceWeighting = weaknessBalanceWeighting;

            BaseStatTotalWeighting = baseStatTotalWeighting;
            BaseStatHpWeighting = baseStatHpWeighting;
            BaseStatAttWeighting = baseStatAttWeighting;
            BaseStatDefWeighting = baseStatDefWeighting;
            BaseStatSpAttWeighting = baseStatSpAttWeighting;
            BaseStatSpDefWeighting = baseStatSpDefWeighting;
            BaseStatSpeWeighting = baseStatSpeWeighting;
        }

        public double SumWeightings()
        {
            double sum = 0;

            if (ResistantAll)
                sum += 2.0;

            if (STABCoverageAll)
                sum += 2.0;

            if (CoverageOnOffensive)
                sum += 1.0;

            if (ResistancesOnDefensive)
                sum += 1.0;

            sum += MoveSetBalanceWeighting;
            sum += StabBalanceWeighting;
            sum += ResistanceBalanceWeighting;
            sum += WeaknessBalanceWeighting;

            sum += 0.5 * BaseStatHpWeighting;
            sum += 0.5 * BaseStatAttWeighting;
            sum += 0.5 * BaseStatDefWeighting;
            sum += 0.5 * BaseStatSpAttWeighting;
            sum += 0.5 * BaseStatSpDefWeighting;
            sum += 0.5 * BaseStatSpeWeighting;

            return sum;
        }
    }

    internal class AutoBuilder
    {
        // The main juice of the team building. this is what decides how good a team is
        public static double CalculateScore(PokemonTeam team, AutoBuilderWeightings weightings)
        {
            if (team.CountPokemon() == 0)
                return 0;

            // scale the base stat weightings by the total weighting
            weightings.BaseStatHpWeighting *= weightings.BaseStatTotalWeighting;
            weightings.BaseStatAttWeighting *= weightings.BaseStatTotalWeighting;
            weightings.BaseStatDefWeighting *= weightings.BaseStatTotalWeighting;
            weightings.BaseStatSpAttWeighting *= weightings.BaseStatTotalWeighting;
            weightings.BaseStatSpDefWeighting *= weightings.BaseStatTotalWeighting;
            weightings.BaseStatSpeWeighting *= weightings.BaseStatTotalWeighting;

            double maxScore = weightings.SumWeightings();
            double scaleFactor = AutoBuilderWeightings.MaxPossibleScore / maxScore;

            // gather some information about the types in the team
            Dictionary<string, int> weaknesses = new Dictionary<string, int>();
            Dictionary<string, int> resistances = new Dictionary<string, int>();
            Dictionary<string, int> STABcoverage = new Dictionary<string, int>();
            Dictionary<string, int> movecoverage = new Dictionary<string, int>();

            double totalWeaknesses = 0;
            double totalResistances = 0;
            double totalSTABCoverage = 0;
            foreach (string t in Globals.AllTypes)
            {
                totalWeaknesses += weaknesses[t] = team.CountWeaknesses(t);
                totalResistances += resistances[t] = team.CountResistances(t);
                totalSTABCoverage += STABcoverage[t] = team.CountSTABCoverage(t);
                movecoverage[t] = team.CountMoveCoverage(t);
            }

            // --- calculate the scores ---

            // STAB coverage score
            double STABCoverageScore = 0;
            if (weightings.STABCoverageAll)
            {
                STABCoverageScore = 2.0;
                foreach (string type in Globals.AllTypes)
                {
                    if (STABcoverage.TryGetValue(type, out int count) && count < 1)
                    {
                        // normal type isn't as important
                        if (type == "normal")
                        {
                            STABCoverageScore -= 0.05;
                        }
                        else
                        {
                            STABCoverageScore -= 0.2;
                        }
                    }
                }
                STABCoverageScore *= scaleFactor;
            }

            // Resistant All score
            double resistantAllScore = 0;
            if (weightings.ResistantAll)
            {
                resistantAllScore = 2.0;
                foreach (string type in Globals.AllTypes)
                {
                    if (resistances.TryGetValue(type, out int count) && count < 1)
                    {
                        // normal type isn't as important
                        if (type == "normal")
                        {
                            resistantAllScore -= 0.05;
                        }
                        else
                        {
                            resistantAllScore -= 0.2;
                        }
                    }
                }
                resistantAllScore *= scaleFactor;
            }

            // offensive pokemon have good coverage score
            double coverageOnOffensiveScore = 0;
            if (weightings.CoverageOnOffensive)
            {
                coverageOnOffensiveScore = CalculateCoverageScore(team) * scaleFactor;
            }

            // defensive pokemon have good resistances score
            double resistancesOnDefensiveScore = 0;
            if (weightings.ResistancesOnDefensive)
            {
                resistancesOnDefensiveScore = CalculateResistancesScore(team) * scaleFactor;
            }

            // resistance balance score
            double resistanceScore = 0;
            if (weightings.ResistanceBalanceWeighting > 0)
            {
                double resistancesSD = CalculateStandardDeviation(resistances);

                resistanceScore = 0.2 * ((4 + (totalResistances/50.0)) - resistancesSD);
                resistanceScore *= weightings.ResistanceBalanceWeighting * scaleFactor;
            }

            // weaknesses balance score
            double weaknessScore = 0;
            if (weightings.WeaknessBalanceWeighting > 0)
            {
                double weaknessesSD = CalculateStandardDeviation(weaknesses);

                weaknessScore = 0.2 * ((5 - (totalWeaknesses / 50.0)) - weaknessesSD);
                weaknessScore *= weightings.WeaknessBalanceWeighting * scaleFactor;
            }

            // move coverage balance score
            double moveBalanceScore = 0;
            if (weightings.MoveSetBalanceWeighting > 0)
            {
                double moveBalanceSD = CalculateStandardDeviation(movecoverage);

                moveBalanceScore = 0.2 * (5 - moveBalanceSD);
                moveBalanceScore *= weightings.MoveSetBalanceWeighting * scaleFactor;
            }

            // STAB coverage balance score
            double stabBalanceScore = 0;
            if (weightings.StabBalanceWeighting > 0)
            {
                double stabBalanceSD = CalculateStandardDeviation(STABcoverage);

                stabBalanceScore = 0.2 * (5 - stabBalanceSD);
                stabBalanceScore *= weightings.StabBalanceWeighting * scaleFactor;
            }

            // base stat scores
            double statScore = 0;
            if (weightings.BaseStatTotalWeighting > 0)
            {
                statScore = CalculateStatsScore(team, weightings);

                statScore *= scaleFactor;
            }

            // sum all the scores
            double score = STABCoverageScore + resistantAllScore +
                coverageOnOffensiveScore + resistancesOnDefensiveScore +
                resistanceScore + weaknessScore + moveBalanceScore + stabBalanceScore +
                statScore;

            score *= team.CountPokemon() / (double)PokemonTeam.MaxTeamSize;

            // scale it back to a num between 0 and 1
            score /= AutoBuilderWeightings.MaxPossibleScore;
            return score;
        }

        // Aah GCSE maths... this seems much easier than I thought it was when I was 15
        private static double CalculateStandardDeviation(Dictionary<string, int> typeDictionary)
        {
            double mean = 0;
            foreach (string type in Globals.AllTypes)
            {
                mean += typeDictionary[type];
            }
            mean /= Globals.AllTypes.Count;

            double variance = 0;
            foreach (string type in Globals.AllTypes)
            {
                variance += Math.Pow(typeDictionary[type] - mean, 2);
            }
            variance /= Globals.AllTypes.Count;

            return Math.Sqrt(variance);
        }

        private static double CalculateStatsScore(PokemonTeam team, AutoBuilderWeightings weightings)
        {
            Dictionary<string, int> statTotals = new Dictionary<string, int>();

            foreach (SmartPokemon? pokemon in team)
            {
                if (pokemon == null)
                    continue;

                foreach (PokemonStat stat in pokemon.Stats)
                {
                    if (statTotals.ContainsKey(stat.Stat.Name))
                        statTotals[stat.Stat.Name] += stat.BaseStat;
                    else
                        statTotals[stat.Stat.Name] = stat.BaseStat;
                }
            }

            double statScore = 0;
            foreach (KeyValuePair<string, int> kvp in statTotals)
            {
                switch (kvp.Key)
                {
                    case "hp":
                        statScore += kvp.Value / 600.0 * weightings.BaseStatHpWeighting;
                        break;
                    case "attack":
                        statScore += kvp.Value / 600.0 * weightings.BaseStatAttWeighting;
                        break;
                    case "special-attack":
                        statScore += kvp.Value / 600.0 * weightings.BaseStatSpAttWeighting;
                        break;
                    case "defense":
                        statScore += kvp.Value / 600.0 * weightings.BaseStatDefWeighting;
                        break;
                    case "special-defense":
                        statScore += kvp.Value / 600.0 * weightings.BaseStatSpDefWeighting;
                        break;
                    case "speed":
                        statScore += kvp.Value / 600.0 * weightings.BaseStatSpeWeighting;
                        break;
                }
            }

            return statScore;
        }

        private static double CalculateCoverageScore(PokemonTeam team)
        {
            double coverageScore = 0;

            foreach (SmartPokemon? p in team)
            {
                if (p is null)
                    continue;

                int countCoverage = p.CountTotalCoverage();
                double totalOffStats = p.GetBaseStat("attack") + p.GetBaseStat("special-attack") + p.GetBaseStat("speed");
                double totalDefStats = p.GetBaseStat("hp") + p.GetBaseStat("defense") + p.GetBaseStat("special-defense");
                double offensiveFactor = totalOffStats / totalDefStats;

                coverageScore += ((offensiveFactor * countCoverage) + (10.0 / offensiveFactor)) / 150.0;
            }

            if (coverageScore > 1.0)
                coverageScore = 1.0;

            return coverageScore;
        }

        private static double CalculateResistancesScore(PokemonTeam team)
        {
            double resistancesScore = 0;

            foreach (SmartPokemon? p in team)
            {
                if (p is null)
                    continue;

                int countResistances = p.CountTotalResistances();
                double totalOffStats = p.GetBaseStat("attack") + p.GetBaseStat("special-attack") + p.GetBaseStat("speed");
                double totalDefStats = p.GetBaseStat("hp") + p.GetBaseStat("defense") + p.GetBaseStat("special-defense");
                double defensiveFactor = totalDefStats / totalOffStats;

                resistancesScore += ((defensiveFactor * countResistances) + (10.0 / defensiveFactor)) / 150.0;
            }

            if (resistancesScore > 1.0)
                resistancesScore = 1.0;

            return resistancesScore;
        }


        public static PokemonTeam BuildTeam(PokemonStorage availablePokemon, PokemonTeam? lockedMembers, AutoBuilderWeightings weightings)
        {
            if (lockedMembers == null)
            {
                lockedMembers = new PokemonTeam();
            }
            else if (lockedMembers.CountPokemon() >= PokemonTeam.MaxTeamSize || availablePokemon.Count < PokemonTeam.MaxTeamSize)
            {
                // they're all locked!
                return lockedMembers;
            }

            PokemonTeam bestTeam = new PokemonTeam();
            double bestTeamScore = 0;

            // using the clever combinations code to quickly iterate through each combination of teams
            // we are selecting (6 - lockedMembers.Count) from all the pokemon in the box
            foreach (SmartPokemon[] remainingTeamCombination in Combinations<SmartPokemon>(availablePokemon.ToArray(), PokemonTeam.MaxTeamSize - lockedMembers.CountPokemon()))
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
                for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
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
                double newTeamScore = CalculateScore(newTeam, weightings);
                if (newTeamScore > bestTeamScore)
                {
                    bestTeam = newTeam;
                    bestTeamScore = newTeamScore;
                }
            }

            return bestTeam;
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



        public static PokemonTeam BuildTeamGenetic(PokemonStorage availablePokemon, AutoBuilderWeightings weightings, PokemonTeam? lockedMembers)
        {
            if (lockedMembers == null)
            {
                lockedMembers = new PokemonTeam();
            }
            else if (lockedMembers.CountPokemon() >= PokemonTeam.MaxTeamSize || availablePokemon.Count < PokemonTeam.MaxTeamSize)
            {
                // they're all locked!
                return lockedMembers;
            }

            return AccordGeneticAlgorithm.SolvePokemonTeam(availablePokemon, weightings, lockedMembers);
        }
    }   
}
