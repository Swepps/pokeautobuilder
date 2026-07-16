using AutoBuilder;
using PokemonDataModel;
using Utility;
using Xunit;

namespace PokeAutobuilderTests
{
    // Tests for TeamScorer.CalculateScore (AutoBuilder/TeamScorer.cs) - the scoring heart of the
    // genetic algorithm. Fully offline: uses TestFixtures.MakeScoringPokemon to inject exact
    // Defense/Attack/move-coverage values directly, sidestepping SmartPokemon's own type-chart
    // resolution (that's covered separately by SmartPokemonMultiplierTests) so these tests are
    // purely about the scoring math itself.
    public class AutoBuilderScoreTests
    {
        // a weightings set with every component zeroed out, so tests can enable just the one
        // component they care about and isolate it from the rest of CalculateScore
        private static AutoBuilderWeightings ZeroedWeightings(
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
            return new AutoBuilderWeightings(
                AutoBuilderWeightings.MakeDefaultTypeWeightings(),
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
            AutoBuilderWeightings weightings = new(); // all defaults "on"

            AutoBuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            Assert.Equal(0.0, result.SumWeightings());
        }

        [Fact]
        public void BaseStatScore_IsSumAcrossTeamNormalizedBy600()
        {
            var pokemon = TestFixtures.MakeScoringPokemon(
                "stat-mon",
                baseStats: new Dictionary<string, int> { { "hp", 300 } }
            );
            PokemonTeam team = MakeTeam(pokemon);

            AutoBuilderWeightings weightings = ZeroedWeightings(baseStatTotal: 1.0, baseStatHp: 1.0);

            AutoBuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            Assert.Equal(300 / 600.0, result.BaseStatHp);
            // the other stat scores shouldn't have been touched since their weightings are 0
            Assert.Equal(0.0, result.BaseStatAtt);
        }

        [Fact]
        public void ResistanceAll_IsFullScoreWhenEveryTypeIsCovered()
        {
            Dictionary<string, double> defense = Globals.AllTypes.ToDictionary(t => t, _ => 0.5);
            var pokemon = TestFixtures.MakeScoringPokemon("fully-resistant-mon", defense: defense);
            PokemonTeam team = MakeTeam(pokemon);

            AutoBuilderWeightings weightings = ZeroedWeightings(resistanceAll: 1.0);

            AutoBuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

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

            AutoBuilderWeightings weightings = ZeroedWeightings(resistanceAll: 1.0);

            AutoBuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            double expected = 1.0 - (1.0 / Globals.AllTypes.Count);
            Assert.Equal(expected, result.ResistanceAll, precision: 10);
        }

        [Fact]
        public void WeaknessAmount_IsPerfectWithNoWeaknesses()
        {
            var pokemon = TestFixtures.MakeScoringPokemon("no-weakness-mon");
            PokemonTeam team = MakeTeam(pokemon);

            AutoBuilderWeightings weightings = ZeroedWeightings(weaknessAmount: 1.0);

            AutoBuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

            Assert.Equal(1.0, result.WeaknessAmount, precision: 10);
        }

        [Fact]
        public void WeaknessAmount_DecreasesAsWeaknessCountIncreases_ButNeverReachesZero()
        {
            Dictionary<string, double> fewWeaknesses = new() { { "fire", 2.0 } };
            Dictionary<string, double> manyWeaknesses = Globals.AllTypes.ToDictionary(t => t, _ => 2.0);

            var fewMon = TestFixtures.MakeScoringPokemon("few-weak-mon", defense: fewWeaknesses);
            var manyMon = TestFixtures.MakeScoringPokemon("many-weak-mon", defense: manyWeaknesses);

            AutoBuilderWeightings weightings = ZeroedWeightings(weaknessAmount: 1.0);

            AutoBuilderWeightings fewResult = TeamScorer.CalculateScore(MakeTeam(fewMon), weightings);
            AutoBuilderWeightings manyResult = TeamScorer.CalculateScore(MakeTeam(manyMon), weightings);

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

            AutoBuilderWeightings weightings = ZeroedWeightings(resistanceBalance: 1.0);

            AutoBuilderWeightings evenResult = TeamScorer.CalculateScore(MakeTeam(evenMon), weightings);
            AutoBuilderWeightings concentratedResult = TeamScorer.CalculateScore(
                MakeTeam(concentratedMon),
                weightings
            );

            Assert.Equal(1.0, evenResult.ResistanceBalance, precision: 10);
            Assert.True(concentratedResult.ResistanceBalance < evenResult.ResistanceBalance);
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

            AutoBuilderWeightings weightings = ZeroedWeightings(moveSetAll: 1.0);

            AutoBuilderWeightings fullResult = TeamScorer.CalculateScore(
                MakeTeam(fullCoverageMon),
                weightings
            );
            AutoBuilderWeightings partialResult = TeamScorer.CalculateScore(
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

            AutoBuilderWeightings weightings = ZeroedWeightings(coverageOnOffensive: 1.0);

            AutoBuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

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

            AutoBuilderWeightings weightings = ZeroedWeightings(resistancesOnDefensive: 1.0);

            AutoBuilderWeightings result = TeamScorer.CalculateScore(team, weightings);

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
