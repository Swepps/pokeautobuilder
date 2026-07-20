using Autobuilder;
using PokemonDataModel;
using Utility;
using Xunit;

namespace PokeAutobuilderTests
{
    // Tests for TeamScorer.CalculateScore (Autobuilder/TeamScorer.cs) - the scoring heart of the
    // genetic algorithm. Fully offline: uses TestFixtures.MakeScoringPokemon to inject exact
    // Defense/Attack/move-coverage values directly, sidestepping SmartPokemon's own type-chart
    // resolution (that's covered separately by SmartPokemonMultiplierTests) so these tests are
    // purely about the scoring math itself.
    public class AutobuilderScoreTests
    {
        // a weightings set with every component zeroed out, so tests can enable just the one
        // component they care about and isolate it from the rest of CalculateScore
        private static AutobuilderWeightings ZeroedWeightings(
            double resistanceAll = 0,
            double resistanceBalance = 0,
            double resistanceAmount = 0,
            double weaknessBalance = 0,
            double weaknessAmount = 0,
            double stabAll = 0,
            double stabBalance = 0,
            double stabAmount = 0,
            double moveSetAll = 0,
            double moveSetBalance = 0,
            double moveSetAmount = 0,
            double coverageOnOffensive = 0,
            double resistancesOnDefensive = 0,
            double baseStatTotal = 0,
            double baseStatHp = 0,
            double baseStatAtt = 0,
            double baseStatDef = 0,
            double baseStatSpAtt = 0,
            double baseStatSpDef = 0,
            double baseStatSpe = 0
        )
        {
            return new AutobuilderWeightings(
                AutobuilderWeightings.MakeDefaultTypeWeightings(),
                resistanceAll: resistanceAll,
                resistanceBalance: resistanceBalance,
                resistanceAmount: resistanceAmount,
                weaknessBalance: weaknessBalance,
                weaknessAmount: weaknessAmount,
                stabAll: stabAll,
                stabBalance: stabBalance,
                stabAmount: stabAmount,
                moveSetAll: moveSetAll,
                moveSetBalance: moveSetBalance,
                moveSetAmount: moveSetAmount,
                coverageOnOffensive: coverageOnOffensive,
                resistancesOnDefensive: resistancesOnDefensive,
                baseStatTotal: baseStatTotal,
                baseStatHp: baseStatHp,
                baseStatAtt: baseStatAtt,
                baseStatDef: baseStatDef,
                baseStatSpAtt: baseStatSpAtt,
                baseStatSpDef: baseStatSpDef,
                baseStatSpe: baseStatSpe
            );
        }

        private static PokemonTeam MakeTeam(params SmartPokemon?[] members)
        {
            PokemonTeam team = new();
            team.Pokemon.AddRange(members);
            return team;
        }

        [Fact]
        public void EmptyTeam_ReturnsZeroedScore()
        {
            PokemonTeam team = new();
            AutobuilderWeightings weightings = new(); // all defaults "on"

            AutobuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            Assert.Equal(0.0, result.SumWeightings());
        }

        // 1 - 0.2^(average / 100) - see CalculateStatCurveScore in TeamScorer.cs
        private static double ExpectedStatCurveScore(double average)
        {
            return 1.0 - Math.Pow(0.2, average / 100.0);
        }

        [Fact]
        public void BaseStatScore_IsCurvedAgainstPerMemberAverage()
        {
            var pokemon = TestFixtures.MakeScoringPokemon(
                "stat-mon",
                baseStats: new Dictionary<string, int> { { "hp", 80 } }
            );
            PokemonTeam team = MakeTeam(pokemon);

            AutobuilderWeightings weightings = ZeroedWeightings(baseStatTotal: 1.0, baseStatHp: 1.0);

            AutobuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            // averaged over team size (1 member here), then run through the diminishing-returns
            // curve rather than divided by a flat 600 that assumes a full 6-member team
            Assert.Equal(ExpectedStatCurveScore(80), result.BaseStatHp, precision: 10);
            // the other stat scores shouldn't have been touched since their weightings are 0
            Assert.Equal(0.0, result.BaseStatAtt);
        }

        [Fact]
        public void BaseStatScore_AverageOfOneHundredScoresAboutEightyPercent()
        {
            var pokemon = TestFixtures.MakeScoringPokemon(
                "stat-mon",
                baseStats: new Dictionary<string, int> { { "hp", 100 } }
            );
            PokemonTeam team = MakeTeam(pokemon);

            AutobuilderWeightings weightings = ZeroedWeightings(baseStatTotal: 1.0, baseStatHp: 1.0);

            AutobuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            // a "good" (green-band) stat average of 100 should land right around 80%, matching
            // the colour bands in PokemonStatsChartPane
            Assert.Equal(0.8, result.BaseStatHp, precision: 10);
        }

        [Fact]
        public void BaseStatScore_ApproachesButNeverReachesOneForExtremeStats()
        {
            var pokemon = TestFixtures.MakeScoringPokemon(
                "stat-mon",
                baseStats: new Dictionary<string, int> { { "hp", 255 } } // Blissey-level HP
            );
            PokemonTeam team = MakeTeam(pokemon);

            AutobuilderWeightings weightings = ZeroedWeightings(baseStatTotal: 1.0, baseStatHp: 1.0);

            AutobuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            Assert.True(result.BaseStatHp < 1.0);
            Assert.True(result.BaseStatHp > 0.9);
        }

        [Fact]
        public void BaseStatScore_SmallerTeamIsNotDevaluedRelativeToFullTeam()
        {
            var soloMon = TestFixtures.MakeScoringPokemon(
                "solo-mon",
                baseStats: new Dictionary<string, int> { { "hp", 90 } }
            );
            PokemonTeam soloTeam = MakeTeam(soloMon);

            var fullTeam = MakeTeam(
                TestFixtures.MakeScoringPokemon("mon-1", baseStats: new Dictionary<string, int> { { "hp", 90 } }),
                TestFixtures.MakeScoringPokemon("mon-2", baseStats: new Dictionary<string, int> { { "hp", 90 } }),
                TestFixtures.MakeScoringPokemon("mon-3", baseStats: new Dictionary<string, int> { { "hp", 90 } }),
                TestFixtures.MakeScoringPokemon("mon-4", baseStats: new Dictionary<string, int> { { "hp", 90 } }),
                TestFixtures.MakeScoringPokemon("mon-5", baseStats: new Dictionary<string, int> { { "hp", 90 } }),
                TestFixtures.MakeScoringPokemon("mon-6", baseStats: new Dictionary<string, int> { { "hp", 90 } })
            );

            AutobuilderWeightings weightings = ZeroedWeightings(baseStatTotal: 1.0, baseStatHp: 1.0);

            AutobuilderWeightings soloResult = TeamScorer.CalculateScore(soloTeam, weightings);
            AutobuilderWeightings fullResult = TeamScorer.CalculateScore(fullTeam, weightings);

            // same average stat quality per member should score the same regardless of team size
            Assert.Equal(fullResult.BaseStatHp, soloResult.BaseStatHp);
        }

        [Fact]
        public void ResistanceAll_IsFullScoreWhenEveryTypeIsCovered()
        {
            Dictionary<string, double> defense = Globals.AllTypes.ToDictionary(t => t, _ => 0.5);
            var pokemon = TestFixtures.MakeScoringPokemon("fully-resistant-mon", defense: defense);
            PokemonTeam team = MakeTeam(pokemon);

            AutobuilderWeightings weightings = ZeroedWeightings(resistanceAll: 1.0);

            AutobuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            Assert.Equal(1.0, result.ResistanceAll, precision: 10);
        }

        [Fact]
        public void ResistanceAll_PenalizesEachUncoveredType()
        {
            // resistant to every type except one
            Dictionary<string, double> defense = Globals
                .AllTypes.Where(t => t != "dragon")
                .ToDictionary(t => t, _ => 0.5);
            var pokemon = TestFixtures.MakeScoringPokemon("almost-resistant-mon", defense: defense);
            PokemonTeam team = MakeTeam(pokemon);

            AutobuilderWeightings weightings = ZeroedWeightings(resistanceAll: 1.0);

            AutobuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            double expected = 1.0 - (1.0 / Globals.AllTypes.Count);
            Assert.Equal(expected, result.ResistanceAll, precision: 10);
        }

        [Fact]
        public void WeaknessAmount_IsPerfectWithNoWeaknesses()
        {
            var pokemon = TestFixtures.MakeScoringPokemon("no-weakness-mon");
            PokemonTeam team = MakeTeam(pokemon);

            AutobuilderWeightings weightings = ZeroedWeightings(weaknessAmount: 1.0);

            AutobuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            Assert.Equal(1.0, result.WeaknessAmount, precision: 10);
        }

        [Fact]
        public void WeaknessAmount_DecreasesAsWeaknessCountIncreases_ButNeverReachesZero()
        {
            Dictionary<string, double> fewWeaknesses = new() { { "fire", 2.0 } };
            Dictionary<string, double> manyWeaknesses = Globals.AllTypes.ToDictionary(t => t, _ => 2.0);

            var fewMon = TestFixtures.MakeScoringPokemon("few-weak-mon", defense: fewWeaknesses);
            var manyMon = TestFixtures.MakeScoringPokemon("many-weak-mon", defense: manyWeaknesses);

            AutobuilderWeightings weightings = ZeroedWeightings(weaknessAmount: 1.0);

            AutobuilderWeightings fewResult = TeamScorer.CalculateScore(MakeTeam(fewMon), weightings);
            AutobuilderWeightings manyResult = TeamScorer.CalculateScore(MakeTeam(manyMon), weightings);

            Assert.True(fewResult.WeaknessAmount < 1.0);
            Assert.True(manyResult.WeaknessAmount < fewResult.WeaknessAmount);
            Assert.True(manyResult.WeaknessAmount > 0.0);
        }

        [Fact]
        public void ResistanceBalance_RewardsEvenDistributionOverConcentrated()
        {
            // resistant to every type exactly once each -> perfectly even, stddev == 0
            Dictionary<string, double> even = Globals.AllTypes.ToDictionary(t => t, _ => 0.5);
            // resistant to only a handful of types -> uneven, stddev > 0
            Dictionary<string, double> concentrated = new()
            {
                { "fire", 0.5 },
                { "water", 0.5 },
                { "grass", 0.5 },
            };

            var evenMon = TestFixtures.MakeScoringPokemon("even-mon", defense: even);
            var concentratedMon = TestFixtures.MakeScoringPokemon("concentrated-mon", defense: concentrated);

            AutobuilderWeightings weightings = ZeroedWeightings(resistanceBalance: 1.0);

            AutobuilderWeightings evenResult = TeamScorer.CalculateScore(MakeTeam(evenMon), weightings);
            AutobuilderWeightings concentratedResult = TeamScorer.CalculateScore(
                MakeTeam(concentratedMon),
                weightings
            );

            Assert.Equal(1.0, evenResult.ResistanceBalance, precision: 10);
            Assert.True(concentratedResult.ResistanceBalance < evenResult.ResistanceBalance);
        }

        // helper: a Pokemon whose STAB is super-effective against exactly the given types
        private static SmartPokemon MakeStabMon(string name, IEnumerable<string> coveredTypes)
        {
            return TestFixtures.MakeScoringPokemon(
                name,
                attack: coveredTypes.ToDictionary(t => t, _ => 2.0)
            );
        }

        [Fact]
        public void StabBalance_UniformCoverageScoresOne_RegardlessOfMagnitude()
        {
            // two Pokemon each covering every type -> count 2 for every type (uniform)
            var twoDeep = MakeTeam(
                MakeStabMon("all-1", Globals.AllTypes),
                MakeStabMon("all-2", Globals.AllTypes)
            );
            // three Pokemon each covering every type -> count 3 for every type (also uniform)
            var threeDeep = MakeTeam(
                MakeStabMon("all-3", Globals.AllTypes),
                MakeStabMon("all-4", Globals.AllTypes),
                MakeStabMon("all-5", Globals.AllTypes)
            );

            AutobuilderWeightings weightings = ZeroedWeightings(stabBalance: 1.0);

            AutobuilderWeightings twoResult = TeamScorer.CalculateScore(twoDeep, weightings);
            AutobuilderWeightings threeResult = TeamScorer.CalculateScore(threeDeep, weightings);

            // both are perfectly even, and the score is scale-invariant so the differing
            // magnitudes (2 vs 3 per type) don't change the result
            Assert.Equal(1.0, twoResult.StabBalance, precision: 10);
            Assert.Equal(1.0, threeResult.StabBalance, precision: 10);
        }

        [Fact]
        public void StabBalance_AllOnOneTypeScoresZero()
        {
            var team = MakeTeam(MakeStabMon("one-type-mon", ["fire"]));

            AutobuilderWeightings weightings = ZeroedWeightings(stabBalance: 1.0);

            AutobuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            Assert.Equal(0.0, result.StabBalance, precision: 10);
        }

        [Fact]
        public void StabBalance_NoCoverageScoresZero()
        {
            // a Pokemon with no super-effective STAB at all -> no coverage is maximally
            // unbalanced, not "uniformly zero" (the old stddev version scored this 1.0)
            var team = MakeTeam(TestFixtures.MakeScoringPokemon("no-coverage-mon"));

            AutobuilderWeightings weightings = ZeroedWeightings(stabBalance: 1.0);

            AutobuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            Assert.Equal(0.0, result.StabBalance, precision: 10);
        }

        [Fact]
        public void StabBalance_CoveringOnlyThreeTypesScoresLow()
        {
            // the "all-Water" shape: STAB super-effective against only fire/ground/rock
            var team = MakeTeam(MakeStabMon("water-mon", ["fire", "ground", "rock"]));

            AutobuilderWeightings weightings = ZeroedWeightings(stabBalance: 1.0);

            AutobuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            // for k evenly-covered types out of N the score is (k-1)/(N-1); here (3-1)/(18-1)
            Assert.Equal(2.0 / 17.0, result.StabBalance, precision: 10);
        }

        [Fact]
        public void StabBalance_OneTypeSlightlyUnderDipsJustBelowOne()
        {
            // three Pokemon covering every type, but the third skips ground -> ground has count
            // 2 while every other type has count 3
            var team = MakeTeam(
                MakeStabMon("full-1", Globals.AllTypes),
                MakeStabMon("full-2", Globals.AllTypes),
                MakeStabMon("skips-ground", Globals.AllTypes.Where(t => t != "ground"))
            );

            AutobuilderWeightings weightings = ZeroedWeightings(stabBalance: 1.0);

            AutobuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            // total = 17*3 + 2 = 53; only ground falls below the uniform 1/18 share
            double uniform = 1.0 / 18.0;
            double overlap = 17 * uniform + 2.0 / 53.0;
            double expected = (overlap - uniform) / (1.0 - uniform);

            Assert.Equal(expected, result.StabBalance, precision: 10);
            // barely below a perfect score - a single type one short out of three
            Assert.True(result.StabBalance < 1.0);
            Assert.True(result.StabBalance > 0.97);
        }

        [Fact]
        public void MoveSetAll_IsFullScoreOnlyWhenEveryTypeHasCoverage()
        {
            var fullCoverageMon = TestFixtures.MakeScoringPokemon(
                "full-coverage-mon",
                moveCoverage: Globals.AllTypes
            );
            var partialCoverageMon = TestFixtures.MakeScoringPokemon(
                "partial-coverage-mon",
                moveCoverage: Globals.AllTypes.Where(t => t != "dragon")
            );

            AutobuilderWeightings weightings = ZeroedWeightings(moveSetAll: 1.0);

            AutobuilderWeightings fullResult = TeamScorer.CalculateScore(
                MakeTeam(fullCoverageMon),
                weightings
            );
            AutobuilderWeightings partialResult = TeamScorer.CalculateScore(
                MakeTeam(partialCoverageMon),
                weightings
            );

            Assert.Equal(1.0, fullResult.MoveSetAll, precision: 10);
            Assert.True(partialResult.MoveSetAll < 1.0);
        }

        [Fact]
        public void CoverageOnOffensive_MatchesHandCalculatedScore()
        {
            var pokemon = TestFixtures.MakeScoringPokemon(
                "coverage-mon",
                attack: new Dictionary<string, double> { { "fire", 2.0 }, { "water", 2.0 } },
                moveCoverage: ["grass"],
                baseStats: new Dictionary<string, int>
                {
                    { "attack", 100 },
                    { "special-attack", 50 },
                    { "speed", 50 },
                    { "hp", 50 },
                    { "defense", 50 },
                    { "special-defense", 50 },
                }
            );
            PokemonTeam team = MakeTeam(pokemon);

            AutobuilderWeightings weightings = ZeroedWeightings(coverageOnOffensive: 1.0);

            AutobuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            // countCoverage = 2 STAB types (fire, water) + 1 move-covered type (grass) = 3
            // offensiveFactor = (100 + 50) / (0.66 * (50 + 50 + 50)) = 150 / 99
            double offensiveFactor = 150.0 / 99.0;
            double expected = ((offensiveFactor * 3) + (10.0 / offensiveFactor)) / 150.0;
            Assert.Equal(expected, result.CoverageOnOffensive, precision: 10);
        }

        [Fact]
        public void ResistancesOnDefensive_MatchesHandCalculatedScore()
        {
            var pokemon = TestFixtures.MakeScoringPokemon(
                "resist-mon",
                defense: new Dictionary<string, double>
                {
                    { "fire", 0.5 },
                    { "water", 0.0 }, // immune - exercises the 0 => 0.25 special case
                    { "grass", 2.0 }, // weakness - should reduce the score
                },
                baseStats: new Dictionary<string, int>
                {
                    { "attack", 50 },
                    { "special-attack", 50 },
                    { "speed", 50 },
                    { "hp", 100 },
                    { "defense", 100 },
                    { "special-defense", 100 },
                }
            );
            PokemonTeam team = MakeTeam(pokemon);

            AutobuilderWeightings weightings = ZeroedWeightings(resistancesOnDefensive: 1.0);

            AutobuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            // countResistances = (1/0.5 - 1) + (1/0.25 - 1) + (1/2.0 - 1) = 1.0 + 3.0 - 0.5 = 3.5
            // (types not in the Defense dict are neutral and contribute 0, per the comment in
            // CalculateResistancesScore)
            // defensiveFactor = (100 + 100 + 100) / (50 + 50 + 50) = 2.0
            double expected = ((2.0 * 3.5) + (10.0 / 2.0)) / 150.0;
            Assert.Equal(0.08, expected, precision: 10); // sanity-check the hand calculation itself
            Assert.Equal(expected, result.ResistancesOnDefensive, precision: 10);
        }
    }
}
