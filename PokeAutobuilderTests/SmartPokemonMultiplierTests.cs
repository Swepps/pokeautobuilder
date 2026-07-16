using PokemonDataModel;
using Xunit;

namespace PokeAutobuilderTests
{
    using Type = PokeApiNet.Type;

    // Tests for SmartPokemon's type-effectiveness aggregation and ability-override logic
    // (SmartPokemon.UpdateMultipliers, in PokemonDataModel/Pokemon.cs). All fully offline - no
    // PokeApiService calls - using synthetic types registered via TestFixtures.MakeType so they
    // can't collide with the real type data the network-backed tests in this project load.
    public class SmartPokemonMultiplierTests
    {
        // each test class owns its own chart - no shared static state between classes
        private readonly TypeChart _chart = new();

        [Fact]
        public void SingleType_PopulatesDefenseAndAttackFromDamageRelations()
        {
            Type t = TestFixtures.MakeType(_chart,
                "test-basic",
                doubleDamageFrom: ["test-weak-to"],
                halfDamageFrom: ["test-resist"],
                noDamageFrom: ["test-immune"],
                doubleDamageTo: ["test-super-effective"],
                halfDamageTo: ["test-not-very-effective"],
                noDamageTo: ["test-no-effect"]
            );

            var pokemon = TestFixtures.MakePokemon(_chart,"basic-mon", types: [t]);

            Assert.Equal(2.0, pokemon.GetResistance("test-weak-to"));
            Assert.Equal(0.5, pokemon.GetResistance("test-resist"));
            Assert.Equal(0.0, pokemon.GetResistance("test-immune"));
            Assert.Equal(1.0, pokemon.GetResistance("test-untouched")); // defaults to neutral

            Assert.Equal(2.0, pokemon.Multipliers.Attack["test-super-effective"]);
            Assert.Equal(0.5, pokemon.Multipliers.Attack["test-not-very-effective"]);
            Assert.Equal(0.0, pokemon.Multipliers.Attack["test-no-effect"]);
        }

        [Fact]
        public void DualType_StacksWeaknessesMultiplicatively()
        {
            Type a = TestFixtures.MakeType(_chart,"test-dual-a", doubleDamageFrom: ["test-target"]);
            Type b = TestFixtures.MakeType(_chart,"test-dual-b", doubleDamageFrom: ["test-target"]);

            var pokemon = TestFixtures.MakePokemon(_chart,"dual-weak-mon", types: [a, b]);

            // both types independently weak (2.0x) should stack multiplicatively to 4.0x, not just 2.0x
            Assert.Equal(4.0, pokemon.GetResistance("test-target"));
        }

        [Fact]
        public void DualType_ResistAndWeakCancelToNeutral()
        {
            Type resist = TestFixtures.MakeType(_chart,"test-resist-type", halfDamageFrom: ["test-target"]);
            Type weak = TestFixtures.MakeType(_chart,"test-weak-type", doubleDamageFrom: ["test-target"]);

            var pokemon = TestFixtures.MakePokemon(_chart,"neutral-mon", types: [resist, weak]);

            // 0.5 * 2.0 should cancel out to a neutral 1.0
            Assert.Equal(1.0, pokemon.GetResistance("test-target"));
        }

        [Theory]
        [InlineData(true)] // immune type processed first
        [InlineData(false)] // immune type processed second
        public void DualType_ImmunityOverridesWeaknessRegardlessOfOrder(bool immuneFirst)
        {
            Type immune = TestFixtures.MakeType(_chart,"test-immune-type", noDamageFrom: ["test-target"]);
            Type weak = TestFixtures.MakeType(_chart,"test-weak-type-2", doubleDamageFrom: ["test-target"]);

            List<Type> types = immuneFirst ? [immune, weak] : [weak, immune];
            var pokemon = TestFixtures.MakePokemon(_chart,"immune-mon", types: types);

            Assert.Equal(0.0, pokemon.GetResistance("test-target"));
        }

        [Fact]
        public void DualType_AttackEffectivenessUsesMaxNotStacking()
        {
            // if this incorrectly multiplied like Defense does, this would be 4.0 instead of 2.0
            Type a = TestFixtures.MakeType(_chart,"test-atk-a", doubleDamageTo: ["test-target"]);
            Type b = TestFixtures.MakeType(_chart,"test-atk-b", doubleDamageTo: ["test-target"]);

            var pokemon = TestFixtures.MakePokemon(_chart,"dual-attack-mon", types: [a, b]);

            Assert.Equal(2.0, pokemon.Multipliers.Attack["test-target"]);
        }

        [Fact]
        public void Ability_Levitate_OverridesInnateGroundWeakness()
        {
            // "ground" is a real type name because the levitate case in UpdateMultipliers hardcodes it
            Type heavy = TestFixtures.MakeType(_chart,"test-heavy", doubleDamageFrom: ["ground"]);

            var withoutLevitate = TestFixtures.MakePokemon(_chart,"heavy-mon", types: [heavy]);
            Assert.Equal(2.0, withoutLevitate.GetResistance("ground"));

            var withLevitate = TestFixtures.MakePokemon(_chart,
                "levitate-mon",
                types: [heavy],
                abilityName: "levitate"
            );
            Assert.Equal(0.0, withLevitate.GetResistance("ground"));
        }

        [Fact]
        public void Ability_ThickFat_HalvesFireAndIceWhenNoExistingMultiplier()
        {
            Type plain = TestFixtures.MakeType(_chart,"test-plain");

            var pokemon = TestFixtures.MakePokemon(_chart,"thick-fat-mon", types: [plain], abilityName: "thick-fat");

            Assert.Equal(0.5, pokemon.GetResistance("fire"));
            Assert.Equal(0.5, pokemon.GetResistance("ice"));
        }

        [Fact]
        public void Ability_ThickFat_StacksWithExistingFireWeakness()
        {
            Type fireWeak = TestFixtures.MakeType(_chart,"test-fire-weak", doubleDamageFrom: ["fire"]);

            var pokemon = TestFixtures.MakePokemon(_chart,
                "thick-fat-fire-mon",
                types: [fireWeak],
                abilityName: "thick-fat"
            );

            // 2.0 (innate weakness) * 0.5 (thick fat) = 1.0, back to neutral
            Assert.Equal(1.0, pokemon.GetResistance("fire"));
        }

        [Fact]
        public void Ability_SolidRock_Reduces_OnlySuperEffectiveDamage()
        {
            Type mixed = TestFixtures.MakeType(_chart,
                "test-mixed",
                doubleDamageFrom: ["test-weak"],
                halfDamageFrom: ["test-resist"]
            );

            var pokemon = TestFixtures.MakePokemon(_chart,"solid-rock-mon", types: [mixed], abilityName: "solid-rock");

            Assert.Equal(1.5, pokemon.GetResistance("test-weak")); // 2.0 * 0.75
            Assert.Equal(0.5, pokemon.GetResistance("test-resist")); // untouched, wasn't >= 2.0
        }

        [Fact]
        public void Ability_WonderGuard_OnlyTakesDamageFromGenuineWeaknesses()
        {
            // "fire"/"water" are real type names because wonder-guard iterates Globals.AllTypes
            Type single = TestFixtures.MakeType(_chart,"test-single", doubleDamageFrom: ["fire"]);

            var pokemon = TestFixtures.MakePokemon(_chart,"wonder-guard-mon", types: [single], abilityName: "wonder-guard");

            Assert.Equal(2.0, pokemon.GetResistance("fire")); // genuine weakness left alone
            Assert.Equal(0.0, pokemon.GetResistance("water")); // no weakness -> forced immune
        }
    }
}
