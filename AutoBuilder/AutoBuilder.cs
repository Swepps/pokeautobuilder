using Accord.Genetic;
using PokeApiNet;
using PokemonDataModel;
using Utility;

namespace AutoBuilder
{
    public class AutoBuilderWeightings
    {
        // priorities                               Weighting values
        public double ResistantAll;                 // max 1.0
        public double STABCoverageAll;              // max 1.0
		public double CoverageOnOffensive;          // max 1.0
		public double ResistancesOnDefensive;       // max 1.0

		// balance weightings
		public double MoveSetBalance;      // max 1.0
        public double StabBalance;         // max 1.0
		public double ResistanceBalance;   // max 1.0
		public double WeaknessBalance;     // max 1.0

		public double BaseStatTotal;       // max 1.0 scales other stat weightings
		public double BaseStatHp;          // max 1.0
		public double BaseStatAtt;         // max 1.0
		public double BaseStatDef;         // max 1.0
		public double BaseStatSpAtt;       // max 1.0
		public double BaseStatSpDef;       // max 1.0
		public double BaseStatSpe;         // max 1.0

        public Dictionary<string, bool> Types = [];

        private static Dictionary<string, bool> MakeDefaultTypeWeightings()
        {
            Dictionary<string, bool> typeWeightings = [];
            foreach (string type in Globals.AllTypes)
            {
                typeWeightings[type] = true;
            }
            return typeWeightings;
        }

		public AutoBuilderWeightings(
            Dictionary<string, bool> typeWeightings
            , double resistantAll = 1.0
            , double stabCoverageAll = 1.0
            , double coverageOnOffensive = 1.0
            , double resistancesOnDefensive = 1.0

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

            MoveSetBalance = moveSetBalanceWeighting;
            StabBalance = stabBalanceWeighting;
            ResistanceBalance = resistanceBalanceWeighting;
            WeaknessBalance = weaknessBalanceWeighting;

            BaseStatTotal = baseStatTotalWeighting;
            BaseStatHp = baseStatHpWeighting;
            BaseStatAtt = baseStatAttWeighting;
            BaseStatDef = baseStatDefWeighting;
            BaseStatSpAtt = baseStatSpAttWeighting;
            BaseStatSpDef = baseStatSpDefWeighting;
            BaseStatSpe = baseStatSpeWeighting;

            Types = typeWeightings;
        }
        public AutoBuilderWeightings() : this(MakeDefaultTypeWeightings())
        {
        }

        public double SumWeightings()
        {
            double sum = 0;

            sum += ResistantAll;
            sum += STABCoverageAll;
            sum += CoverageOnOffensive;
            sum += ResistancesOnDefensive;

            sum += MoveSetBalance;
            sum += StabBalance;
            sum += ResistanceBalance;
            sum += WeaknessBalance;

            sum += BaseStatHp;
            sum += BaseStatAtt;
            sum += BaseStatDef;
            sum += BaseStatSpAtt;
            sum += BaseStatSpDef;
            sum += BaseStatSpe;

            return sum;
        }
    }

    internal class AutoBuilder
    {
        // The main juice of the team building. this is what decides how good a team is
        public static AutoBuilderWeightings CalculateScore(PokemonTeam team, AutoBuilderWeightings weightings)
        {
            // use another list of weightings to get the individual score from each parameter
            AutoBuilderWeightings result = new(                
                weightings.Types
                , resistantAll: 0.0
                , stabCoverageAll: 0.0
                , coverageOnOffensive: 0.0
                , resistancesOnDefensive: 0.0
                , moveSetBalanceWeighting: 0.0
                , stabBalanceWeighting: 0.0
                , resistanceBalanceWeighting: 0.0
                , weaknessBalanceWeighting: 0.0
                , baseStatTotalWeighting: 0.0
                , baseStatHpWeighting: 0.0
                , baseStatAttWeighting: 0.0
                , baseStatDefWeighting: 0.0
                , baseStatSpAttWeighting: 0.0
                , baseStatSpDefWeighting: 0.0
                , baseStatSpeWeighting: 0.0
            );

            if (team.CountPokemon() == 0)
                return result;

            // scale the base stat weightings by the total weighting
            weightings.BaseStatHp *= weightings.BaseStatTotal;
            weightings.BaseStatAtt *= weightings.BaseStatTotal;
            weightings.BaseStatDef *= weightings.BaseStatTotal;
            weightings.BaseStatSpAtt *= weightings.BaseStatTotal;
            weightings.BaseStatSpDef *= weightings.BaseStatTotal;
            weightings.BaseStatSpe *= weightings.BaseStatTotal;

            double totalTypes = weightings.Types.Where((t) => t.Value).Count();

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
            if (weightings.STABCoverageAll > 0
                && totalTypes > 0)
            {
                result.STABCoverageAll = weightings.STABCoverageAll;
                double scorePerType = result.STABCoverageAll / totalTypes;
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.Types[type] // only reduce if the type is being counted
                        && STABcoverage.TryGetValue(type, out int count) && count < 1)
                    {
                        result.STABCoverageAll -= scorePerType;
                    }
                }
            }

            // Resistant All score
            if (weightings.ResistantAll > 0
                && totalTypes > 0)
            {
                result.ResistantAll = weightings.ResistantAll;
                double scorePerType = result.ResistantAll / totalTypes;
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.Types[type] // only reduce if the type is being counted
                        && resistances.TryGetValue(type, out int count) && count < 1)
                    {
                        result.ResistantAll -= scorePerType;
                    }
                }
            }

            // offensive pokemon have good coverage score
            if (weightings.CoverageOnOffensive > 0)
            {
                result.CoverageOnOffensive = CalculateCoverageScore(team, weightings) * weightings.CoverageOnOffensive;
            }

            // defensive pokemon have good resistances score
            if (weightings.ResistancesOnDefensive > 0)
            {
                result.ResistancesOnDefensive = CalculateResistancesScore(team, weightings) * weightings.ResistancesOnDefensive;
            }

            // resistance balance score
            if (weightings.ResistanceBalance > 0
                && totalTypes > 0)
            {
                // create a score 0.0 - 1.0 based on how many resistances the team has
                // vs the types we're interested in
                int totalInterestedResistances = 0;
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.Types[type])
                    {
                        totalInterestedResistances += resistances[type];
                    }
                }
                result.ResistanceBalance = 1.0 - Math.Pow((2 * totalTypes - 1) / (2 * totalTypes), totalInterestedResistances);

                double resistancesSD = CalculateStandardDeviation(resistances, weightings);
                result.ResistanceBalance = Math.Pow(result.ResistanceBalance, 1.0 + resistancesSD);
                result.ResistanceBalance *= weightings.ResistanceBalance;
            }

            // weaknesses balance score
            if (weightings.WeaknessBalance > 0
                && totalTypes > 0)
            {
                // create a score 0.0 - 1.0 based on how many weaknesses the team has
                // vs the types we're interested in
                int totalInterestedWeaknesses = 0;
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.Types[type])
                    {
                        totalInterestedWeaknesses += weaknesses[type];
                    }
                }
                result.WeaknessBalance = 1.0 - Math.Pow((2 * totalTypes - 1) / (2 * totalTypes), totalInterestedWeaknesses);

                double weaknessesSD = CalculateStandardDeviation(weaknesses, weightings);
                result.WeaknessBalance = Math.Pow(result.WeaknessBalance, 1.0 + weaknessesSD);
                result.WeaknessBalance *= weightings.WeaknessBalance;
            }

            // move coverage balance score
            if (weightings.MoveSetBalance > 0
                && totalTypes > 0)
            {
                // create a score 0.0 - 1.0 based on how good the move coverage is
                // vs the types we're interested in
                int totalInterestedMoveCoverage = 0;
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.Types[type])
                    {
                        totalInterestedMoveCoverage += movecoverage[type];
                    }
                }
                result.MoveSetBalance = 1.0 - Math.Pow((2 * totalTypes - 1) / (2 * totalTypes), totalInterestedMoveCoverage);

                double moveBalanceSD = CalculateStandardDeviation(movecoverage, weightings);
                result.MoveSetBalance = Math.Pow(result.MoveSetBalance, 1.0 + moveBalanceSD);
                result.MoveSetBalance *= weightings.MoveSetBalance;
            }

            // STAB coverage balance score
            if (weightings.StabBalance > 0
                && totalTypes > 0)
            {
                // create a score 0.0 - 1.0 based on how good the STAB coverage is
                // vs the types we're interested in
                int totalInterestedSTABCoverage = 0;
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.Types[type])
                    {
                        totalInterestedSTABCoverage += STABcoverage[type];
                    }
                }
                result.StabBalance = 1.0 - Math.Pow((2 * totalTypes - 1) / (2 * totalTypes), totalInterestedSTABCoverage);

                double stabBalanceSD = CalculateStandardDeviation(STABcoverage, weightings);
                result.StabBalance = Math.Pow(result.StabBalance, 1.0 + stabBalanceSD);
                result.StabBalance *= weightings.StabBalance;
            }

            // base stat scores
            if (weightings.BaseStatTotal > 0)
            {
                CalculateStatsScore(team, weightings, result);
            }

            return result;
        }

        // Aah GCSE maths... this seems much easier than I thought it was when I was 15
        private static double CalculateStandardDeviation(Dictionary<string, int> typeDictionary, AutoBuilderWeightings weightings)
        {
            double totalTypes = weightings.Types.Where((t) => t.Value).Count();
            double mean = 0;
            foreach (string type in Globals.AllTypes)
            {
                if (weightings.Types[type]
                    && typeDictionary.TryGetValue(type, out int typeCount)) mean += typeCount;
            }
            mean /= totalTypes;

            double variance = 0;
            foreach (string type in Globals.AllTypes)
            {
                if (weightings.Types[type]
                    && typeDictionary.TryGetValue(type, out int typeCount)) variance += Math.Pow(typeCount - mean, 2);
            }
            variance /= totalTypes;

            return Math.Sqrt(variance);
        }

        private static void CalculateStatsScore(PokemonTeam team, AutoBuilderWeightings weightings, AutoBuilderWeightings score)
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

            foreach (KeyValuePair<string, int> kvp in statTotals)
            {
                switch (kvp.Key)
                {
                    case "hp":
                        score.BaseStatHp    = (kvp.Value / 600.0) * weightings.BaseStatTotal * weightings.BaseStatHp;
                        break;
                    case "attack":
                        score.BaseStatAtt   = (kvp.Value / 600.0) * weightings.BaseStatTotal * weightings.BaseStatAtt;
                        break;
                    case "special-attack":
                        score.BaseStatSpAtt = (kvp.Value / 600.0) * weightings.BaseStatTotal * weightings.BaseStatSpAtt;
                        break;
                    case "defense":
                        score.BaseStatDef   = (kvp.Value / 600.0) * weightings.BaseStatTotal * weightings.BaseStatDef;
                        break;
                    case "special-defense":
                        score.BaseStatSpDef = (kvp.Value / 600.0) * weightings.BaseStatTotal * weightings.BaseStatSpDef;
                        break;
                    case "speed":
                        score.BaseStatSpe   = (kvp.Value / 600.0) * weightings.BaseStatTotal * weightings.BaseStatSpe;
                        break;
                }
            }
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
                    if (weightings.Types[type])
                    {
                        if (p.IsTypeCoveredBySTAB(type)) countCoverage++;
                        if (p.IsTypeCoveredByMove(type)) countCoverage++;
                    }
                }
                // only really care about the highest offensive stat
                double highestOffStat = Math.Max(p.GetBaseStat("attack"), p.GetBaseStat("special-attack"));
                double totalOffStats = highestOffStat + p.GetBaseStat("speed");
                // scale def stats to roughly same size as offense
                double totalDefStats = 0.66 * (p.GetBaseStat("hp") + p.GetBaseStat("defense") + p.GetBaseStat("special-defense"));
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
            double totalTypes = weightings.Types.Where((t) => t.Value).Count();

            foreach (SmartPokemon? p in team.Pokemon)
            {
                if (p is null)
                    continue;

                double countResistances = 0;
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.Types[type])
                    {
                        countResistances += 1.0 / (p.GetResistance(type) == 0 ? 0.25 : p.GetResistance(type));
                    }
                }
                countResistances -= totalTypes; // normal resistance is counted as 1, so minus the number of types to get a real value
                double totalOffStats = p.GetBaseStat("attack") + p.GetBaseStat("special-attack") + p.GetBaseStat("speed");
                double totalDefStats = p.GetBaseStat("hp") + p.GetBaseStat("defense") + p.GetBaseStat("special-defense");
                double defensiveFactor = totalDefStats / totalOffStats;

                resistancesScore += ((defensiveFactor * countResistances) + (10.0 / defensiveFactor)) / 150.0;
            }

            if (resistancesScore > 1.0)
                resistancesScore = 1.0;

            return resistancesScore;
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
    }   
}
