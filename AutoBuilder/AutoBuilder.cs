using Accord.Genetic;
using PokeApiNet;
using PokemonDataModel;
using Utility;

namespace AutoBuilder
{
    // contains doubles that represent the importance of each parameter for team generation
    // all doubles are between 0 and 1.0
    public class AutoBuilderWeightings(
        Dictionary<string, bool> typeWeightings,
        double resistanceAll = 1.0,
        double resistanceBalance = 1.0,
        double resistanceAmount = 1.0,
        double weaknessAmount = 1.0,
        double weaknessBalance = 1.0,
        double stabAll = 1.0,
        double stabBalance = 1.0,
        double stabAmount = 1.0,
        double moveSetAll = 1.0,
        double moveSetBalance = 1.0,
        double moveSetAmount = 1.0,
        double coverageOnOffensive = 1.0,
        double resistancesOnDefensive = 1.0,
        double baseStatTotal = 1.0,
        double baseStatHp = 1.0,
        double baseStatAtt = 1.0,
        double baseStatDef = 1.0,
        double baseStatSpAtt = 1.0,
        double baseStatSpDef = 1.0,
        double baseStatSpe = 1.0,
        bool allowMultipleMegas = false,
        bool allowMultipleGmax = false
    )
    {
        public Dictionary<string, bool> Types = typeWeightings;

        // resistances
        public double ResistanceAll { get; set; } = resistanceAll;
        public double ResistanceBalance { get; set; } = resistanceBalance;
        public double ResistanceAmount { get; set; } = resistanceAmount;

        // weaknesses
        public double WeaknessAmount { get; set; } = weaknessAmount;
        public double WeaknessBalance { get; set; } = weaknessBalance;

        // STAB
        public double StabAll { get; set; } = stabAll;
        public double StabBalance { get; set; } = stabBalance;
        public double StabAmount { get; set; } = stabAmount;

        // Moves
        public double MoveSetAll { get; set; } = moveSetAll;
        public double MoveSetBalance { get; set; } = moveSetBalance;
        public double MoveSetAmount { get; set; } = moveSetAmount;

        // misc weightings
        public double CoverageOnOffensive { get; set; } = coverageOnOffensive;
        public double ResistancesOnDefensive { get; set; } = resistancesOnDefensive;

        // base stats
        public double BaseStatTotal { get; set; } = baseStatTotal; // scales other stat weightings
        public double BaseStatHp { get; set; } = baseStatHp;
        public double BaseStatAtt { get; set; } = baseStatAtt;
        public double BaseStatDef { get; set; } = baseStatDef;
        public double BaseStatSpAtt { get; set; } = baseStatSpAtt;
        public double BaseStatSpDef { get; set; } = baseStatSpDef;
        public double BaseStatSpe { get; set; } = baseStatSpe;

        // generation settings
        public bool AllowMultipleMegas { get; set; } = allowMultipleMegas;
        public bool AllowMultipleGmax { get; set; } = allowMultipleGmax;

        public AutoBuilderWeightings()
            : this(MakeDefaultTypeWeightings()) { }

        // copy constructor
        public AutoBuilderWeightings(AutoBuilderWeightings clone)
            : this(
                clone.Types,
                resistanceAll: clone.ResistanceAll,
                resistanceBalance: clone.ResistanceBalance,
                resistanceAmount: clone.ResistanceAmount,
                weaknessBalance: clone.WeaknessBalance,
                weaknessAmount: clone.WeaknessAmount,
                stabAll: clone.StabAll,
                stabBalance: clone.StabBalance,
                stabAmount: clone.StabAmount,
                moveSetAll: clone.MoveSetAll,
                moveSetBalance: clone.MoveSetBalance,
                moveSetAmount: clone.MoveSetAmount,
                coverageOnOffensive: clone.CoverageOnOffensive,
                resistancesOnDefensive: clone.ResistancesOnDefensive,
                baseStatTotal: clone.BaseStatTotal,
                baseStatHp: clone.BaseStatHp,
                baseStatAtt: clone.BaseStatAtt,
                baseStatDef: clone.BaseStatDef,
                baseStatSpAtt: clone.BaseStatSpAtt,
                baseStatSpDef: clone.BaseStatSpDef,
                baseStatSpe: clone.BaseStatSpe
            ) { }

        public double SumWeightings()
        {
            double sum = 0;

            sum += ResistanceAll;
            sum += ResistanceBalance;
            sum += ResistanceAmount;

            sum += WeaknessBalance;
            sum += WeaknessAmount;

            sum += StabAll;
            sum += StabBalance;
            sum += StabAmount;

            sum += MoveSetAll;
            sum += MoveSetBalance;
            sum += MoveSetAmount;

            sum += CoverageOnOffensive;
            sum += ResistancesOnDefensive;

            sum += BaseStatHp;
            sum += BaseStatAtt;
            sum += BaseStatDef;
            sum += BaseStatSpAtt;
            sum += BaseStatSpDef;
            sum += BaseStatSpe;

            return sum;
        }

        public static Dictionary<string, bool> MakeDefaultTypeWeightings()
        {
            Dictionary<string, bool> typeWeightings = [];
            foreach (string type in Globals.AllTypes)
            {
                typeWeightings[type] = true;
            }
            return typeWeightings;
        }
    }

    public class AutoBuilder
    {
        // The main juice of the team building. this is what decides how good a team is
        public static AutoBuilderWeightings CalculateScore(
            PokemonTeam team,
            AutoBuilderWeightings weightings
        )
        {
            // use another list of weightings to get the individual score from each parameter
            AutoBuilderWeightings result = new(
                weightings.Types,
                resistanceAll: 0.0,
                resistanceBalance: 0.0,
                resistanceAmount: 0.0,
                weaknessBalance: 0.0,
                weaknessAmount: 0.0,
                stabAll: 0.0,
                stabBalance: 0.0,
                stabAmount: 0.0,
                moveSetAll: 0.0,
                moveSetBalance: 0.0,
                moveSetAmount: 0.0,
                coverageOnOffensive: 0.0,
                resistancesOnDefensive: 0.0,
                baseStatTotal: 0.0,
                baseStatHp: 0.0,
                baseStatAtt: 0.0,
                baseStatDef: 0.0,
                baseStatSpAtt: 0.0,
                baseStatSpDef: 0.0,
                baseStatSpe: 0.0
            );

            if (team.CountPokemon() == 0)
                return result;

            double totalTypes = weightings.Types.Where((t) => t.Value).Count();

            // gather some information about the types in the team
            var (weaknesses, resistances, STABcoverage, movecoverage) = team.CountTypeCoverage();

            // --- calculate the scores ---

            // - Resistance scores -
            // Resistant to all types
            if (weightings.ResistanceAll > 0 && totalTypes > 0)
            {
                result.ResistanceAll = weightings.ResistanceAll;
                double scorePerType = result.ResistanceAll / totalTypes;
                foreach (string type in Globals.AllTypes)
                {
                    if (
                        weightings.Types[type] // only reduce if the type is being counted
                        && resistances.TryGetValue(type, out int count)
                        && count < 1
                    )
                    {
                        result.ResistanceAll -= scorePerType;
                    }
                }
            }
            // resistance balance score
            if (weightings.ResistanceBalance > 0 && totalTypes > 0)
            {
                double resistancesSD = CalculateStandardDeviation(resistances, weightings);
                result.ResistanceBalance = 1.0 - (0.2 * resistancesSD);
                result.ResistanceBalance *= weightings.ResistanceBalance;
            }
            // resistance amount score
            if (weightings.ResistanceAmount > 0 && totalTypes > 0)
            {
                int totalInterestedResistances = 0;
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.Types[type])
                    {
                        totalInterestedResistances += resistances[type];
                    }
                }
                // use a semi-logarithmic algorithm to make increasing resistances scale closer to (but never reach) 1.0
                result.ResistanceAmount =
                    1.0
                    - Math.Pow(
                        (2 * totalTypes - 1) / (2 * totalTypes),
                        2 * totalInterestedResistances
                    );
                result.ResistanceAmount *= weightings.ResistanceAmount;
            }

            // - Weaknesses scores -
            // weaknesses balance score
            if (weightings.WeaknessBalance > 0 && totalTypes > 0)
            {
                double weaknessesSD = CalculateStandardDeviation(weaknesses, weightings);
                result.WeaknessBalance = 1.0 - (0.2 * weaknessesSD);
                result.WeaknessBalance *= weightings.WeaknessBalance;
            }
            // weaknesses amount score
            if (weightings.WeaknessAmount > 0 && totalTypes > 0)
            {
                int totalInterestedWeaknesses = 0;
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.Types[type])
                    {
                        totalInterestedWeaknesses += weaknesses[type];
                    }
                }
                // use a semi-logarithmic algorithm to make increasing weakness scale closer to (but never reach) 0
                result.WeaknessAmount = Math.Pow(
                    (2 * totalTypes - 1) / (2 * totalTypes),
                    2 * totalInterestedWeaknesses
                );
                result.WeaknessAmount *= weightings.WeaknessAmount;
            }

            // - STAB scores -
            // STAB coverage againt all types
            if (weightings.StabAll > 0 && totalTypes > 0)
            {
                result.StabAll = weightings.StabAll;
                double scorePerType = result.StabAll / totalTypes;
                foreach (string type in Globals.AllTypes)
                {
                    if (
                        weightings.Types[type] // only reduce if the type is being counted
                        && STABcoverage.TryGetValue(type, out int count)
                        && count < 1
                    )
                    {
                        result.StabAll -= scorePerType;
                    }
                }
            }
            // STAB coverage balance score
            if (weightings.StabBalance > 0 && totalTypes > 0)
            {
                double stabBalanceSD = CalculateStandardDeviation(STABcoverage, weightings);
                result.StabBalance = 1.0 - (0.2 * stabBalanceSD);
                result.StabBalance *= weightings.StabBalance;
            }
            // STAB coverage amount
            if (weightings.StabAmount > 0 && totalTypes > 0)
            {
                int totalInterestedStabCoverage = 0;
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.Types[type])
                    {
                        totalInterestedStabCoverage += STABcoverage[type];
                    }
                }
                // use a semi-logarithmic algorithm to make increasing STAB coverage scale closer to (but never reach) 1.0
                result.StabAmount =
                    1.0
                    - Math.Pow(
                        (2 * totalTypes - 1) / (2 * totalTypes),
                        2 * totalInterestedStabCoverage
                    );
                result.StabAmount *= weightings.StabAmount;
            }

            // - Move set scores -
            // Move set coverage againt all types
            if (weightings.MoveSetAll > 0 && totalTypes > 0)
            {
                result.MoveSetAll = weightings.MoveSetAll;
                double scorePerType = result.MoveSetAll / totalTypes;
                foreach (string type in Globals.AllTypes)
                {
                    if (
                        weightings.Types[type] // only reduce if the type is being counted
                        && movecoverage.TryGetValue(type, out int count)
                        && count < 1
                    )
                    {
                        result.MoveSetAll -= scorePerType;
                    }
                }
            }
            // move coverage balance score
            if (weightings.MoveSetBalance > 0 && totalTypes > 0)
            {
                double moveBalanceSD = CalculateStandardDeviation(movecoverage, weightings);
                result.MoveSetBalance = 1.0 - (0.2 * moveBalanceSD);
                result.MoveSetBalance *= weightings.MoveSetBalance;
            }
            // move set coverage amount
            if (weightings.MoveSetAmount > 0 && totalTypes > 0)
            {
                int totalInterestedMoveSetCoverage = 0;
                foreach (string type in Globals.AllTypes)
                {
                    if (weightings.Types[type])
                    {
                        totalInterestedMoveSetCoverage += movecoverage[type];
                    }
                }
                // use a semi-logarithmic algorithm to make increasing STAB coverage scale closer to (but never reach) 1.0
                result.MoveSetAmount =
                    1.0
                    - Math.Pow(
                        (2 * totalTypes - 1) / (2 * totalTypes),
                        2 * totalInterestedMoveSetCoverage
                    );
                result.MoveSetAmount *= weightings.MoveSetAmount;
            }

            // - Misc Scores -
            // offensive pokemon have good coverage score
            if (weightings.CoverageOnOffensive > 0)
            {
                result.CoverageOnOffensive =
                    CalculateCoverageScore(team, weightings) * weightings.CoverageOnOffensive;
            }
            // defensive pokemon have good resistances score
            if (weightings.ResistancesOnDefensive > 0)
            {
                result.ResistancesOnDefensive =
                    CalculateResistancesScore(team, weightings) * weightings.ResistancesOnDefensive;
            }

            // - Base stat scores -
            if (weightings.BaseStatTotal > 0)
            {
                CalculateStatsScore(team, weightings, result);
            }

            return result;
        }

        // Aah GCSE maths... this seems much easier than I thought it was when I was 15
        private static double CalculateStandardDeviation(
            Dictionary<string, int> typeDictionary,
            AutoBuilderWeightings weightings
        )
        {
            double totalTypes = weightings.Types.Where((t) => t.Value).Count();
            double mean = 0;
            foreach (string type in Globals.AllTypes)
            {
                if (weightings.Types[type] && typeDictionary.TryGetValue(type, out int typeCount))
                    mean += typeCount;
            }
            mean /= totalTypes;

            double variance = 0;
            foreach (string type in Globals.AllTypes)
            {
                if (weightings.Types[type] && typeDictionary.TryGetValue(type, out int typeCount))
                    variance += Math.Pow(typeCount - mean, 2);
            }
            variance /= totalTypes;

            return Math.Sqrt(variance);
        }

        private static void CalculateStatsScore(
            PokemonTeam team,
            AutoBuilderWeightings weightings,
            AutoBuilderWeightings score
        )
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
                        score.BaseStatHp =
                            (kvp.Value / 600.0) * weightings.BaseStatTotal * weightings.BaseStatHp;
                        break;
                    case "attack":
                        score.BaseStatAtt =
                            (kvp.Value / 600.0) * weightings.BaseStatTotal * weightings.BaseStatAtt;
                        break;
                    case "special-attack":
                        score.BaseStatSpAtt =
                            (kvp.Value / 600.0)
                            * weightings.BaseStatTotal
                            * weightings.BaseStatSpAtt;
                        break;
                    case "defense":
                        score.BaseStatDef =
                            (kvp.Value / 600.0) * weightings.BaseStatTotal * weightings.BaseStatDef;
                        break;
                    case "special-defense":
                        score.BaseStatSpDef =
                            (kvp.Value / 600.0)
                            * weightings.BaseStatTotal
                            * weightings.BaseStatSpDef;
                        break;
                    case "speed":
                        score.BaseStatSpe =
                            (kvp.Value / 600.0) * weightings.BaseStatTotal * weightings.BaseStatSpe;
                        break;
                }
            }
        }

        private static double CalculateCoverageScore(
            PokemonTeam team,
            AutoBuilderWeightings weightings
        )
        {
            double coverageScore = 0;

            foreach (SmartPokemon? p in team.Pokemon)
            {
                if (p is null)
                    continue;

                // Scan the Pokemon's own (small) attack/move-coverage dictionaries instead of
                // checking every global type against them - equivalent to the
                // IsTypeCoveredBySTAB/IsTypeCoveredByMove checks below, just without re-testing
                // types that can't be covered.
                double countCoverage = 0;
                foreach (KeyValuePair<string, double> kvp in p.Multipliers.Attack)
                {
                    if (
                        kvp.Value >= 2.0
                        && weightings.Types.TryGetValue(kvp.Key, out bool isWeighted)
                        && isWeighted
                    )
                        countCoverage++;
                }
                foreach (KeyValuePair<string, double> kvp in p.SelectedMoves.AttackMultipliers)
                {
                    if (
                        kvp.Value >= 2.0
                        && weightings.Types.TryGetValue(kvp.Key, out bool isWeighted)
                        && isWeighted
                    )
                        countCoverage++;
                }
                // only really care about the highest offensive stat
                double highestOffStat = Math.Max(
                    p.GetBaseStat("attack"),
                    p.GetBaseStat("special-attack")
                );
                double totalOffStats = highestOffStat + p.GetBaseStat("speed");
                // scale def stats to roughly same size as offense
                double totalDefStats =
                    0.66
                    * (
                        p.GetBaseStat("hp")
                        + p.GetBaseStat("defense")
                        + p.GetBaseStat("special-defense")
                    );
                double offensiveFactor = totalOffStats / totalDefStats;

                coverageScore +=
                    ((offensiveFactor * countCoverage) + (10.0 / offensiveFactor)) / 150.0;
            }

            if (coverageScore > 1.0)
                coverageScore = 1.0;

            return coverageScore;
        }

        private static double CalculateResistancesScore(
            PokemonTeam team,
            AutoBuilderWeightings weightings
        )
        {
            double resistancesScore = 0;

            foreach (SmartPokemon? p in team.Pokemon)
            {
                if (p is null)
                    continue;

                // types absent from Defense default to a neutral 1.0 via GetResistance, which
                // contributes exactly 0 once the old "- totalTypes" normalization is applied
                // (1.0 / 1.0 - 1.0 = 0) - so only types actually present in Defense can affect
                // the result, and we can scan just those instead of every global type.
                double countResistances = 0;
                foreach (KeyValuePair<string, double> kvp in p.Multipliers.Defense)
                {
                    if (
                        !weightings.Types.TryGetValue(kvp.Key, out bool isWeighted)
                        || !isWeighted
                    )
                        continue;

                    double value = kvp.Value;
                    countResistances += 1.0 / (value == 0 ? 0.25 : value) - 1.0;
                }
                double totalOffStats =
                    p.GetBaseStat("attack")
                    + p.GetBaseStat("special-attack")
                    + p.GetBaseStat("speed");
                double totalDefStats =
                    p.GetBaseStat("hp")
                    + p.GetBaseStat("defense")
                    + p.GetBaseStat("special-defense");
                double defensiveFactor = totalDefStats / totalOffStats;

                resistancesScore +=
                    ((defensiveFactor * countResistances) + (10.0 / defensiveFactor)) / 150.0;
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
                    if (index != m)
                        continue;
                    yield return (int[])result.Clone(); // thanks to @xanatos
                    //yield return result;
                    break;
                }
            }
        }

        public static IEnumerable<T[]> Combinations<T>(T[] array, int m)
        {
            if (array.Length < m)
                throw new ArgumentException(
                    "Array length can't be less than number of selected elements"
                );
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
