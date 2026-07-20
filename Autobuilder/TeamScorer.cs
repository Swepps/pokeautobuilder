using Accord.Genetic;
using PokeApiNet;
using PokemonDataModel;
using Utility;

namespace Autobuilder
{
    // Scores how good a team is against a set of weightings - the fitness function at the heart of
    // the genetic algorithm.
    public class TeamScorer
    {
        // The main juice of the team building. this is what decides how good a team is
        public static AutobuilderWeightings CalculateScore(
            PokemonTeam team,
            AutobuilderWeightings weightings
        )
        {
            // use another list of weightings to get the individual score from each parameter
            AutobuilderWeightings result = new(
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
            // Resistance/Weakness/STAB/MoveSet each score the same three ways - "All" (full marks
            // unless some enabled type isn't covered at all; weaknesses have no such score, since
            // "weak to everything" isn't something we reward the absence of), "Balance" (even
            // spread across enabled types), and "Amount" (more is better, except weaknesses invert
            // the curve since more weaknesses is worse) - so those three shapes are factored out
            // below instead of repeating each one per dimension.

            // - Resistance scores -
            if (weightings.ResistanceAll > 0 && totalTypes > 0)
                result.ResistanceAll = CalculateAllTypesScore(resistances, weightings.ResistanceAll, totalTypes, weightings.Types);
            if (weightings.ResistanceBalance > 0 && totalTypes > 0)
                result.ResistanceBalance = CalculateBalanceScore(resistances, weightings.ResistanceBalance, weightings);
            if (weightings.ResistanceAmount > 0 && totalTypes > 0)
                result.ResistanceAmount = CalculateAmountScore(resistances, weightings.ResistanceAmount, totalTypes, weightings.Types, invert: false);

            // - Weaknesses scores -
            if (weightings.WeaknessBalance > 0 && totalTypes > 0)
                result.WeaknessBalance = CalculateBalanceScore(weaknesses, weightings.WeaknessBalance, weightings);
            if (weightings.WeaknessAmount > 0 && totalTypes > 0)
                result.WeaknessAmount = CalculateAmountScore(weaknesses, weightings.WeaknessAmount, totalTypes, weightings.Types, invert: true);

            // - STAB scores -
            if (weightings.StabAll > 0 && totalTypes > 0)
                result.StabAll = CalculateAllTypesScore(STABcoverage, weightings.StabAll, totalTypes, weightings.Types);
            if (weightings.StabBalance > 0 && totalTypes > 0)
                result.StabBalance = CalculateBalanceScore(STABcoverage, weightings.StabBalance, weightings);
            if (weightings.StabAmount > 0 && totalTypes > 0)
                result.StabAmount = CalculateAmountScore(STABcoverage, weightings.StabAmount, totalTypes, weightings.Types, invert: false);

            // - Move set scores -
            if (weightings.MoveSetAll > 0 && totalTypes > 0)
                result.MoveSetAll = CalculateAllTypesScore(movecoverage, weightings.MoveSetAll, totalTypes, weightings.Types);
            if (weightings.MoveSetBalance > 0 && totalTypes > 0)
                result.MoveSetBalance = CalculateBalanceScore(movecoverage, weightings.MoveSetBalance, weightings);
            if (weightings.MoveSetAmount > 0 && totalTypes > 0)
                result.MoveSetAmount = CalculateAmountScore(movecoverage, weightings.MoveSetAmount, totalTypes, weightings.Types, invert: false);

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

        // Groups the flat per-category scores into named groups for display (see
        // TeamScoreBreakdown). Always scores against default (all-1.0) weightings, so this is the
        // team's objective score independent of whatever weight sliders a user has configured.
        public static TeamScoreBreakdown CalculateBreakdown(PokemonTeam team)
        {
            AutobuilderWeightings scores = CalculateScore(team, new AutobuilderWeightings());

            List<ScoreGroup> groups =
            [
                new(
                    "STAB Coverage",
                    [
                        new("All Types", "Has STAB coverage against every enabled type", scores.StabAll),
                        new("Balance", "STAB coverage is evenly spread across types", scores.StabBalance),
                        new("Amount", "Has a high amount of total STAB coverage", scores.StabAmount),
                    ]
                ),
                new(
                    "Move Coverage",
                    [
                        new("All Types", "Has move coverage against every enabled type", scores.MoveSetAll),
                        new("Balance", "Move coverage is evenly spread across types", scores.MoveSetBalance),
                        new("Amount", "Has a high amount of total move coverage", scores.MoveSetAmount),
                    ]
                ),
                new(
                    "Resistances",
                    [
                        new("All Types", "Resists every enabled type with at least one team member", scores.ResistanceAll),
                        new("Balance", "Resistances are evenly spread across types", scores.ResistanceBalance),
                        new("Amount", "Has a high amount of total type resistances", scores.ResistanceAmount),
                    ]
                ),
                new(
                    "Weaknesses",
                    [
                        new("Balance", "Weaknesses are evenly spread across types", scores.WeaknessBalance),
                        new("Few Weaknesses", "Has a low amount of total type weaknesses - unlike the other bars on this page, this one is fuller the fewer weaknesses the team has", scores.WeaknessAmount),
                    ]
                ),
                new(
                    "Synergy",
                    [
                        new("Offensive Coverage", "Offensive Pokémon have good type coverage", scores.CoverageOnOffensive),
                        new("Defensive Resistances", "Defensive Pokémon have good type resistances", scores.ResistancesOnDefensive),
                    ]
                ),
                new(
                    "Stat Spread",
                    [
                        new("HP", "Team's combined HP base stat", scores.BaseStatHp),
                        new("Attack", "Team's combined Attack base stat", scores.BaseStatAtt),
                        new("Sp. Attack", "Team's combined Special Attack base stat", scores.BaseStatSpAtt),
                        new("Defense", "Team's combined Defense base stat", scores.BaseStatDef),
                        new("Sp. Defense", "Team's combined Special Defense base stat", scores.BaseStatSpDef),
                        new("Speed", "Team's combined Speed base stat", scores.BaseStatSpe),
                    ]
                ),
            ];

            return new TeamScoreBreakdown(groups, scores.SumWeightings());
        }

        // "All" pattern: starts at full weight and loses an even share for every enabled type this
        // dictionary doesn't cover at all. Shared by ResistanceAll/StabAll/MoveSetAll.
        private static double CalculateAllTypesScore(
            Dictionary<string, int> typeCoverage,
            double weighting,
            double totalTypes,
            Dictionary<string, bool> enabledTypes
        )
        {
            double score = weighting;
            double scorePerType = score / totalTypes;
            foreach (string type in Globals.AllTypes)
            {
                if (
                    enabledTypes[type] // only reduce if the type is being counted
                    && typeCoverage.TryGetValue(type, out int count)
                    && count < 1
                )
                {
                    score -= scorePerType;
                }
            }
            return score;
        }

        // "Balance" pattern: rewards an even spread across enabled types by measuring how much a
        // team's coverage overlaps a perfectly uniform distribution - the histogram-intersection
        // Sum(min(p_i, 1/N)) of the coverage proportions p_i against the uniform 1/N. That overlap
        // runs from 1/N (all coverage piled onto a single type) up to 1 (uniform), and is rescaled
        // onto [0, 1] so the worst possible spread scores 0 and a perfectly even one scores 1.
        // Because it works on proportions rather than raw counts it's scale-invariant: the same
        // shape of coverage scores the same whatever the team size (the old standard-deviation
        // version scored the identical shape differently just because the counts were bigger).
        // Shared by all four dimensions (Resistance/Weakness/Stab/MoveSet).
        private static double CalculateBalanceScore(
            Dictionary<string, int> typeCoverage,
            double weighting,
            AutobuilderWeightings weightings
        )
        {
            double totalTypes = weightings.Types.Where((t) => t.Value).Count();

            int total = 0;
            foreach (string type in Globals.AllTypes)
            {
                if (weightings.Types[type] && typeCoverage.TryGetValue(type, out int count))
                    total += count;
            }

            // no coverage at all is maximally unbalanced, not "uniformly zero"
            if (total == 0)
                return 0.0;
            // with a single enabled type any coverage is trivially uniform (and the rescale below
            // would divide by zero)
            if (totalTypes <= 1)
                return weighting;

            double uniform = 1.0 / totalTypes;
            double overlap = 0.0; // Sum(min(p_i, 1/N)) - maxes at 1 for a uniform spread
            foreach (string type in Globals.AllTypes)
            {
                if (weightings.Types[type] && typeCoverage.TryGetValue(type, out int count))
                    overlap += Math.Min(count / (double)total, uniform);
            }

            double balance = (overlap - uniform) / (1.0 - uniform);
            return balance * weighting;
        }

        // "Amount" pattern: semi-logarithmic scale so more coverage approaches (but never reaches)
        // a perfect score. Shared by all four dimensions; weaknesses invert the curve since a
        // higher weakness count should score worse, not better.
        private static double CalculateAmountScore(
            Dictionary<string, int> typeCoverage,
            double weighting,
            double totalTypes,
            Dictionary<string, bool> enabledTypes,
            bool invert
        )
        {
            int totalInterested = 0;
            foreach (string type in Globals.AllTypes)
            {
                if (enabledTypes[type])
                {
                    totalInterested += typeCoverage[type];
                }
            }

            double curve = Math.Pow((2 * totalTypes - 1) / (2 * totalTypes), 2 * totalInterested);
            return (invert ? curve : 1.0 - curve) * weighting;
        }

        // "good" per-member base stat average that CalculateStatCurveScore treats as ~80% -
        // chosen to line up with where PokemonStatsChartPane's colour bands turn from green
        // ("good") towards teal ("great"), so a score in the 0.8 region reads the same way the
        // stats chart does.
        private const double GoodBaseStatPerPokemon = 100.0;

        // Diminishing-returns curve for "more is better" stat averages: approaches but never
        // reaches a perfect score, so a handful of exceptional Pokemon can't instantly max out
        // the score, and there's no flat plateau once the underlying average passes some fixed
        // threshold. This is a continuous cousin of the semi-log curve CalculateAmountScore uses
        // for type-coverage counts - same "1 - r^x" shape, just over a continuous average
        // instead of a discrete count.
        private static double CalculateStatCurveScore(double average)
        {
            return 1.0 - Math.Pow(0.2, average / GoodBaseStatPerPokemon);
        }

        private static void CalculateStatsScore(
            PokemonTeam team,
            AutobuilderWeightings weightings,
            AutobuilderWeightings score
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

            int teamSize = team.CountPokemon();

            foreach (KeyValuePair<string, int> kvp in statTotals)
            {
                double normalizedStat = CalculateStatCurveScore((double)kvp.Value / teamSize);
                switch (kvp.Key)
                {
                    case "hp":
                        score.BaseStatHp =
                            normalizedStat * weightings.BaseStatTotal * weightings.BaseStatHp;
                        break;
                    case "attack":
                        score.BaseStatAtt =
                            normalizedStat * weightings.BaseStatTotal * weightings.BaseStatAtt;
                        break;
                    case "special-attack":
                        score.BaseStatSpAtt =
                            normalizedStat
                            * weightings.BaseStatTotal
                            * weightings.BaseStatSpAtt;
                        break;
                    case "defense":
                        score.BaseStatDef =
                            normalizedStat * weightings.BaseStatTotal * weightings.BaseStatDef;
                        break;
                    case "special-defense":
                        score.BaseStatSpDef =
                            normalizedStat
                            * weightings.BaseStatTotal
                            * weightings.BaseStatSpDef;
                        break;
                    case "speed":
                        score.BaseStatSpe =
                            normalizedStat * weightings.BaseStatTotal * weightings.BaseStatSpe;
                        break;
                }
            }
        }

        private static double CalculateCoverageScore(
            PokemonTeam team,
            AutobuilderWeightings weightings
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
            AutobuilderWeightings weightings
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
