using System.Linq;
using PokemonDataModel;
using Xunit;

namespace PokeAutobuilderTests
{
    // Tests for SmartPokemon.GetStatsForGeneration's layering of PastStatsTable deltas onto
    // current-day base stats, and the Gen 1 "special" collapse/fallback built on top of it. Uses
    // real PokeAPI pokemon ids (Butterfree/Pikachu/Alakazam/Mega Alakazam) with their real
    // current-day stats, so the expected historical values can be checked against known Gen 1-5
    // stat lines rather than synthetic data.
    public class SmartPokemonPastStatsTests
    {
        // Butterfree: current Sp. Atk 90/Sp. Def 80. past_stats: Gen 1 special=80, Gen 5
        // special-attack=80 (the Gen 6 physical/special split rebalance bumped it from 80 to 90).
        private static SmartPokemon MakeButterfree() =>
            TestFixtures.MakeScoringPokemon(
                "butterfree",
                id: 12,
                baseStats: new()
                {
                    ["hp"] = 60,
                    ["attack"] = 45,
                    ["defense"] = 50,
                    ["special-attack"] = 90,
                    ["special-defense"] = 80,
                    ["speed"] = 70,
                }
            );

        // Pikachu: current Def 40/Sp. Def 50. past_stats: Gen 1 special=50, Gen 5 defense=30 +
        // special-defense=40 - two entries touching different stats that must layer together
        // rather than one replacing the other.
        private static SmartPokemon MakePikachu() =>
            TestFixtures.MakeScoringPokemon(
                "pikachu",
                id: 25,
                baseStats: new()
                {
                    ["hp"] = 35,
                    ["attack"] = 55,
                    ["defense"] = 40,
                    ["special-attack"] = 50,
                    ["special-defense"] = 50,
                    ["speed"] = 90,
                }
            );

        // Alakazam: current Sp. Def 95. past_stats: Gen 1 special=135, Gen 5 special-defense=85
        // (the Gen 6 rebalance bumped it from 85 to 95).
        private static SmartPokemon MakeAlakazam() =>
            TestFixtures.MakeScoringPokemon(
                "alakazam",
                id: 65,
                baseStats: new()
                {
                    ["hp"] = 55,
                    ["attack"] = 50,
                    ["defense"] = 45,
                    ["special-attack"] = 135,
                    ["special-defense"] = 95,
                    ["speed"] = 120,
                }
            );

        // Mega Alakazam has its own pokemon id (10037) distinct from base Alakazam's (65), with its
        // own past_stats entry: Gen 6 special-defense=95 (bumped to 105 same as base Alakazam).
        private static SmartPokemon MakeMegaAlakazam() =>
            TestFixtures.MakeScoringPokemon(
                "alakazam-mega",
                id: 10037,
                baseStats: new()
                {
                    ["hp"] = 55,
                    ["attack"] = 50,
                    ["defense"] = 65,
                    ["special-attack"] = 175,
                    ["special-defense"] = 105,
                    ["speed"] = 150,
                }
            );

        [Fact]
        public void Butterfree_Gen1_CollapsesSpecialToBothSpAtkAndSpDef()
        {
            SmartPokemon butterfree = MakeButterfree();

            var stats = butterfree.GetStatsForGeneration(1);

            Assert.Equal(60, stats["hp"]);
            Assert.Equal(45, stats["attack"]);
            Assert.Equal(50, stats["defense"]);
            Assert.Equal(80, stats["special-attack"]);
            Assert.Equal(80, stats["special-defense"]);
            Assert.Equal(70, stats["speed"]);
        }

        [Fact]
        public void Butterfree_Gen3_UsesPreGen6SpAtkWithoutSpecialCollapse()
        {
            SmartPokemon butterfree = MakeButterfree();

            var stats = butterfree.GetStatsForGeneration(3);

            // the Gen 1 "special" entry doesn't qualify for target=3, so no collapse happens -
            // Sp. Atk/Sp. Def are independently resolved (80/80, the latter coincidentally
            // unchanged from current-day)
            Assert.Equal(80, stats["special-attack"]);
            Assert.Equal(80, stats["special-defense"]);
        }

        [Fact]
        public void Butterfree_CurrentGeneration_ReturnsUnmodifiedStats()
        {
            SmartPokemon butterfree = MakeButterfree();

            var stats = butterfree.GetStatsForGeneration(null);

            Assert.Equal(90, stats["special-attack"]);
            Assert.Equal(80, stats["special-defense"]);
        }

        [Fact]
        public void Pikachu_Gen1_LayersDefenseDeltaFromGen5UnderSpecialFromGen1()
        {
            SmartPokemon pikachu = MakePikachu();

            var stats = pikachu.GetStatsForGeneration(1);

            // Defense is restored from the Gen 5 entry (30) and is untouched by the special
            // collapse; Sp. Atk/Sp. Def both become the Gen 1 Special value (50), overwriting the
            // Gen 5 special-defense delta (40) since Gen 1's Special applies for a smaller
            // (closer to the target) qualifying generation
            Assert.Equal(35, stats["hp"]);
            Assert.Equal(55, stats["attack"]);
            Assert.Equal(30, stats["defense"]);
            Assert.Equal(50, stats["special-attack"]);
            Assert.Equal(50, stats["special-defense"]);
            Assert.Equal(90, stats["speed"]);
        }

        [Fact]
        public void Pikachu_Gen5_UsesDefenseDeltaWithoutSpecialCollapse()
        {
            SmartPokemon pikachu = MakePikachu();

            var stats = pikachu.GetStatsForGeneration(5);

            Assert.Equal(30, stats["defense"]);
            Assert.Equal(50, stats["special-attack"]);
            Assert.Equal(40, stats["special-defense"]);
        }

        [Fact]
        public void Alakazam_Gen1_CollapsesToPreSplitSpecial()
        {
            SmartPokemon alakazam = MakeAlakazam();

            var stats = alakazam.GetStatsForGeneration(1);

            Assert.Equal(135, stats["special-attack"]);
            Assert.Equal(135, stats["special-defense"]);
        }

        [Fact]
        public void Alakazam_Gen5_UsesPreGen6SpDefWithoutCollapse()
        {
            SmartPokemon alakazam = MakeAlakazam();

            var stats = alakazam.GetStatsForGeneration(5);

            Assert.Equal(135, stats["special-attack"]);
            Assert.Equal(85, stats["special-defense"]);
        }

        [Fact]
        public void MegaAlakazam_HasItsOwnPastStatsEntryDistinctFromBaseAlakazam()
        {
            SmartPokemon megaAlakazam = MakeMegaAlakazam();

            var gen6 = megaAlakazam.GetStatsForGeneration(6);
            var gen7 = megaAlakazam.GetStatsForGeneration(7);

            Assert.Equal(95, gen6["special-defense"]);
            Assert.Equal(105, gen7["special-defense"]);
        }

        [Fact]
        public void Gen1WithNoPastStatsEntry_FallsBackToCurrentSpAtkForBothSpecialStats()
        {
            // id 999999 has no PastStatsTable entry at all - approximating a Pokemon that didn't
            // exist yet in Gen 1 but is shown in a Gen 1 box/team anyway (never blocked, per the
            // ruleset's "never evict incompatible Pokemon" rule)
            SmartPokemon lucario = TestFixtures.MakeScoringPokemon(
                "lucario",
                id: 999999,
                baseStats: new()
                {
                    ["hp"] = 70,
                    ["attack"] = 110,
                    ["defense"] = 70,
                    ["special-attack"] = 115,
                    ["special-defense"] = 70,
                    ["speed"] = 90,
                }
            );

            var stats = lucario.GetStatsForGeneration(1);

            Assert.Equal(115, stats["special-attack"]);
            Assert.Equal(115, stats["special-defense"]);
        }

        [Fact]
        public void GetBaseStat_ReturnsResolvedValueForGeneration()
        {
            SmartPokemon alakazam = MakeAlakazam();

            Assert.Equal(135, alakazam.GetBaseStat("special-defense", 1));
            Assert.Equal(95, alakazam.GetBaseStat("special-defense", null));
        }

        [Fact]
        public void GetBaseStatsForDisplay_Gen1_ReturnsFiveBarsWithSingleSpecial()
        {
            SmartPokemon alakazam = MakeAlakazam();

            var display = alakazam.GetBaseStatsForDisplay(1);

            Assert.Equal(5, display.Count);
            Assert.Equal(("Special", 135), display.Single(s => s.Label == "Special"));
            Assert.DoesNotContain(display, s => s.Label is "Sp. Atk" or "Sp. Def");
        }

        [Fact]
        public void GetBaseStatsForDisplay_NonGen1_ReturnsSixBarsWithSplitSpecial()
        {
            SmartPokemon alakazam = MakeAlakazam();

            var display = alakazam.GetBaseStatsForDisplay(5);

            Assert.Equal(6, display.Count);
            Assert.Equal(135, display.Single(s => s.Label == "Sp. Atk").Value);
            Assert.Equal(85, display.Single(s => s.Label == "Sp. Def").Value);
        }

        [Fact]
        public void GetBaseStatsTotal_Gen1_CountsSpecialOnce()
        {
            SmartPokemon alakazam = MakeAlakazam();

            // 55 + 50 + 135 (Special, counted once) + 45 + 120 = 405, not 500
            Assert.Equal(405, alakazam.GetBaseStatsTotal(1));
        }
    }
}
