using Accord.Genetic;
using PokeApiNet;
using PokemonDataModel;
using Utility;

namespace AutoBuilder
{
    public struct AutoBuilderWeightings
    {
        // sum of all weightings. some bools are worth 2
        public static readonly double MaxPossibleScore = 11.0;

        // priorities                               Weighting values
        public bool ResistantAll;                   // 1.0
        public bool STABCoverageAll;                // 1.0
		public bool CoverageOnOffensive;            // 1.0
		public bool ResistancesOnDefensive;         // 1.0

		// balance weightings
		public double MoveSetBalanceWeighting;      // max 1.0
        public double StabBalanceWeighting;         // max 1.0
		public double ResistanceBalanceWeighting;   // max 1.0
		public double WeaknessBalanceWeighting;     // max 1.0

		public double BaseStatTotalWeighting;       // scales other stat weightings
		// base stats ( total = 3.0 )
		public double BaseStatHpWeighting;          // max 0.5
		public double BaseStatAttWeighting;         // max 0.5
		public double BaseStatDefWeighting;         // max 0.5
		public double BaseStatSpAttWeighting;       // max 0.5
		public double BaseStatSpDefWeighting;       // max 0.5
		public double BaseStatSpeWeighting;         // max 0.5

        public Dictionary<string, bool> TypeWeightings = [];

		public AutoBuilderWeightings(
            Dictionary<string, bool> typeWeightings
            , bool resistantAll = true
            , bool stabCoverageAll = true
            , bool coverageOnOffensive = true
            , bool resistancesOnDefensive = true

            , double moveSetBalanceWeighting = 1.0
            , double stabBalanceWeighting = 1.0
            , double resistanceBalanceWeighting = 1.0
            , double weaknessBalanceWeighting = 1.0

            , double baseStatTotalWeighting = 1.0
            , double baseStatHpWeighting = 0.5
            , double baseStatAttWeighting = 0.5
            , double baseStatDefWeighting = 0.5
            , double baseStatSpAttWeighting = 0.5
            , double baseStatSpDefWeighting = 0.5
            , double baseStatSpeWeighting = 0.5)
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

            TypeWeightings = typeWeightings;
        }
        public AutoBuilderWeightings(
            bool resistantAll = true
            , bool stabCoverageAll = true
            , bool coverageOnOffensive = true
            , bool resistancesOnDefensive = true

            , double moveSetBalanceWeighting = 1.0
            , double stabBalanceWeighting = 1.0
            , double resistanceBalanceWeighting = 1.0
            , double weaknessBalanceWeighting = 1.0

            , double baseStatTotalWeighting = 1.0
            , double baseStatHpWeighting = 0.5
            , double baseStatAttWeighting = 0.5
            , double baseStatDefWeighting = 0.5
            , double baseStatSpAttWeighting = 0.5
            , double baseStatSpDefWeighting = 0.5
            , double baseStatSpeWeighting = 0.5)
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

            foreach (string type in Globals.AllTypes) 
            {
                TypeWeightings[type] = true;
            }
        }

        public double SumWeightings()
        {
            double sum = 0;

            if (ResistantAll)
                sum += 1.0;

            if (STABCoverageAll)
                sum += 1.0;

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
            double totalTypes = weightings.TypeWeightings.Where((t) => t.Value).Count();

            // gather some information about the types in the team
            Dictionary<string, int> weaknesses = [];
            Dictionary<string, int> resistances = [];
            Dictionary<string, int> STABcoverage = [];
            Dictionary<string, int> movecoverage = [];

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
            double STABCoverageAllScore = 0;
            if (weightings.STABCoverageAll)
            {
                STABCoverageAllScore = 1.0;
                foreach (string type in Globals.AllTypes)
                {
                    if (STABcoverage.TryGetValue(type, out int count) && count < 1)
                    {
                        // normal type isn't as important
                        if (type == "normal")
                        {
                            STABCoverageAllScore -= 0.05;
                        }
                        else
                        {
                            STABCoverageAllScore -= 0.1;
                        }
                    }
                }
                STABCoverageAllScore *= scaleFactor;
            }

            // Resistant All score
            double resistantAllScore = 0;
            if (weightings.ResistantAll)
            {
                resistantAllScore = 1.0;
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
                            resistantAllScore -= 0.1;
                        }
                    }
                }
                resistantAllScore *= scaleFactor;
            }

            // offensive pokemon have good coverage score
            double coverageOnOffensiveScore = 0;
            if (weightings.CoverageOnOffensive)
            {
                coverageOnOffensiveScore = CalculateCoverageScore(team, weightings) * scaleFactor;
            }

            // defensive pokemon have good resistances score
            double resistancesOnDefensiveScore = 0;
            if (weightings.ResistancesOnDefensive)
            {
                resistancesOnDefensiveScore = CalculateResistancesScore(team, weightings) * scaleFactor;
            }

            // resistance balance score
            double resistanceScore = 0;
            if (weightings.ResistanceBalanceWeighting > 0)
            {
                // create a score 0.0 - 1.0 based on how many resistances the team has
                // vs the types we're interested in
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.TypeWeightings[type])
                    {
                        resistanceScore += (resistances[type] / 6.0) / totalTypes;
                    }
                }

                double resistancesSD = CalculateStandardDeviation(resistances);
                resistanceScore -= (resistancesSD / (double)((1 + Globals.AllTypes.Count)  - totalTypes));
                resistanceScore = Math.Clamp(resistanceScore, 0, 1.0);
                resistanceScore *= weightings.ResistanceBalanceWeighting * scaleFactor;
            }

            // weaknesses balance score
            double weaknessScore = 0;
            if (weightings.WeaknessBalanceWeighting > 0)
            {
                weaknessScore = 1.0;
                // create a score 0.0 - 1.0 based on how many weaknesses the team has
                // vs the types we're interested in
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.TypeWeightings[type])
                    {
                        weaknessScore -= (weaknesses[type] / 6.0) / totalTypes;
                    }
                }

                double weaknessesSD = CalculateStandardDeviation(weaknesses);
                weaknessScore -= (weaknessesSD / (double)((1 + Globals.AllTypes.Count) - totalTypes));
                weaknessScore = Math.Clamp(weaknessScore, 0, 1.0);
                weaknessScore *= weightings.WeaknessBalanceWeighting * scaleFactor;
            }

            // move coverage balance score
            double moveBalanceScore = 0;
            if (weightings.MoveSetBalanceWeighting > 0)
            {
                // create a score 0.0 - 1.0 based on how good the move coverage is
                // vs the types we're interested in
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.TypeWeightings[type])
                    {
                        moveBalanceScore += (movecoverage[type] / 6.0) / totalTypes;
                    }
                }

                double moveBalanceSD = CalculateStandardDeviation(movecoverage);
                moveBalanceScore -= (moveBalanceScore / (double)((1 + Globals.AllTypes.Count) - totalTypes));
                moveBalanceScore = Math.Clamp(moveBalanceScore, 0, 1.0);
                moveBalanceScore *= weightings.MoveSetBalanceWeighting * scaleFactor;
            }

            // STAB coverage balance score
            double stabBalanceScore = 0;
            if (weightings.StabBalanceWeighting > 0)
            {
                // create a score 0.0 - 1.0 based on how good the STAB coverage is
                // vs the types we're interested in
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.TypeWeightings[type])
                    {
                        stabBalanceScore += (STABcoverage[type] / 6.0) / totalTypes;
                    }
                }

                double stabBalanceSD = CalculateStandardDeviation(STABcoverage);
                stabBalanceScore -= (stabBalanceScore / (double)((1 + Globals.AllTypes.Count) - totalTypes));
                stabBalanceScore = Math.Clamp(stabBalanceScore, 0, 1.0);
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
            double score = STABCoverageAllScore + resistantAllScore +
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
                if (typeDictionary.TryGetValue(type, out int typeCount)) mean += typeCount;
            }
            mean /= Globals.AllTypes.Count;

            double variance = 0;
            foreach (string type in Globals.AllTypes)
            {
                if (typeDictionary.TryGetValue(type, out int typeCount)) variance += Math.Pow(typeCount - mean, 2);
            }
            variance /= Globals.AllTypes.Count;

            return Math.Sqrt(variance);
        }

        private static double CalculateStatsScore(PokemonTeam team, AutoBuilderWeightings weightings)
        {
            Dictionary<string, int> statTotals = new Dictionary<string, int>();

            foreach (SmartPokemon? pokemon in team.Pokemon)
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

        private static double CalculateCoverageScore(PokemonTeam team, AutoBuilderWeightings weightings)
        {
            double coverageScore = 0;

            foreach (SmartPokemon? p in team.Pokemon)
            {
                if (p is null)
                    continue;

                double countCoverage = 0;
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.TypeWeightings[type])
                    {
                        if (p.IsTypeCoveredBySTAB(type)) countCoverage++;
                        if (p.IsTypeCoveredByMove(type)) countCoverage++;
                    }
                }
                double totalOffStats = p.GetBaseStat("attack") + p.GetBaseStat("special-attack") + p.GetBaseStat("speed");
                double totalDefStats = p.GetBaseStat("hp") + p.GetBaseStat("defense") + p.GetBaseStat("special-defense");
                double offensiveFactor = totalOffStats / totalDefStats;

                coverageScore += ((offensiveFactor * countCoverage) + (10.0 / offensiveFactor)) / 150.0;
            }

            if (coverageScore > 1.0)
                coverageScore = 1.0;

            return coverageScore;
        }

        private static double CalculateResistancesScore(PokemonTeam team, AutoBuilderWeightings weightings)
        {
            double resistancesScore = 0;

            foreach (SmartPokemon? p in team.Pokemon)
            {
                if (p is null)
                    continue;

                double countResistances = 0;
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.TypeWeightings[type])
                    {
                        countResistances += 2.0 - p.GetResistance(type);
                    }
                }
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
            else if (lockedMembers.CountPokemon() >= PokemonTeam.MaxTeamSize || availablePokemon.Pokemon.Count < PokemonTeam.MaxTeamSize)
            {
                // they're all locked!
                return lockedMembers;
            }

            PokemonTeam bestTeam = new PokemonTeam();
            double bestTeamScore = 0;

            // using the clever combinations code to quickly iterate through each combination of teams
            // we are selecting (6 - lockedMembers.Count) from all the pokemon in the box
            foreach (SmartPokemon[] remainingTeamCombination in Combinations<SmartPokemon>(availablePokemon.Pokemon.ToArray(), PokemonTeam.MaxTeamSize - lockedMembers.CountPokemon()))
            {
                // check that we're not already using one of these selected pokemon in our team already
                // this is inefficient but the lists will always be small so should be pretty quick
                foreach (SmartPokemon entry in remainingTeamCombination)
                {
                    foreach (SmartPokemon? pokemon in lockedMembers.Pokemon)
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
                    if (lockedMembers.Pokemon[i] != null)
                    {
                        newTeam.Pokemon[i] = lockedMembers.Pokemon[i];
                    }
                    else if (combinIdx < remainingTeamCombination.Length)
                    {
                        newTeam.Pokemon[i] = remainingTeamCombination[combinIdx];
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
            else if (lockedMembers.CountPokemon() >= PokemonTeam.MaxTeamSize || availablePokemon.Pokemon.Count < PokemonTeam.MaxTeamSize)
            {
                // they're all locked!
                return lockedMembers;
            }

            return AccordGeneticAlgorithm.SolvePokemonTeam(availablePokemon, weightings, lockedMembers);
        }
    }   
}
