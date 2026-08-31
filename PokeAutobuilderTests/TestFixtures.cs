using PokeApiNet;
using PokemonDataModel;
using Utility;

namespace PokeAutobuilderTests
{
    using Type = PokeApiNet.Type;

    // Builds SmartPokemon instances entirely offline (no PokeApiService calls), so business-logic
    // tests don't depend on network access or PokeAPI's actual data. Each test class owns its own
    // TypeChart instance and passes it in - no process-wide shared state between test classes.
    internal static class TestFixtures
    {
        public static async Task EnsureRealTypesLoadedAsync(
            PokeApiService apiService,
            TypeChart typeChart
        )
        {
            if (!typeChart.IsEmpty)
            {
                return;
            }

            typeChart.Populate(await apiService.GetAllTypesAsync());
        }

        // Registers a fake type in the given TypeChart so SmartPokemon can resolve it.
        public static Type MakeType(
            TypeChart typeChart,
            string name,
            IEnumerable<string>? doubleDamageFrom = null,
            IEnumerable<string>? halfDamageFrom = null,
            IEnumerable<string>? noDamageFrom = null,
            IEnumerable<string>? doubleDamageTo = null,
            IEnumerable<string>? halfDamageTo = null,
            IEnumerable<string>? noDamageTo = null
        )
        {
            Type type = new()
            {
                Name = name,
                DamageRelations = new TypeRelations
                {
                    DoubleDamageFrom = ToResourceList(doubleDamageFrom),
                    HalfDamageFrom = ToResourceList(halfDamageFrom),
                    NoDamageFrom = ToResourceList(noDamageFrom),
                    DoubleDamageTo = ToResourceList(doubleDamageTo),
                    HalfDamageTo = ToResourceList(halfDamageTo),
                    NoDamageTo = ToResourceList(noDamageTo),
                },
            };

            typeChart.Add(type);

            return type;
        }

        private static List<NamedApiResource<Type>> ToResourceList(IEnumerable<string>? names)
        {
            return (names ?? []).Select(n => new NamedApiResource<Type> { Name = n }).ToList();
        }

        // Builds a SmartPokemon with the given types/ability, exercising the real UpdateMultipliers
        // logic (type-effectiveness aggregation + ability overrides). Types must already be
        // registered in the given chart via MakeType.
        public static SmartPokemon MakePokemon(
            TypeChart typeChart,
            string name,
            IEnumerable<Type> types,
            string? abilityName = null,
            Dictionary<string, int>? baseStats = null,
            int id = 0
        )
        {
            List<PokemonType> pokemonTypes = types
                .Select(
                    (t, i) =>
                        new PokemonType
                        {
                            Slot = i + 1,
                            Type = new NamedApiResource<Type> { Name = t.Name },
                        }
                )
                .ToList();

            PokemonAbility ability = new()
            {
                Slot = 1,
                IsHidden = false,
                Ability = new NamedApiResource<Ability> { Name = abilityName ?? "" },
            };

            List<PokemonStat> stats = new List<string>
            {
                "hp",
                "attack",
                "defense",
                "special-attack",
                "special-defense",
                "speed",
            }
                .Select(
                    statName =>
                        new PokemonStat
                        {
                            Stat = new NamedApiResource<Stat> { Name = statName },
                            BaseStat = baseStats is not null && baseStats.TryGetValue(statName, out int v) ? v : 50,
                            Effort = 0,
                        }
                )
                .ToList();

            SmartPokemon pokemon = new(
                Id: id,
                Name: name,
                BaseExperience: null,
                Height: 0,
                IsDefault: true,
                Order: 0,
                Weight: 0,
                Abilities: [ability],
                Forms: [],
                GameIndicies: [],
                HeldItems: [],
                LocationAreaEncounters: "",
                Moves: [],
                PastTypes: [],
                Sprites: new PokemonSprites(),
                Species: new NamedApiResource<PokemonSpecies> { Name = name },
                Stats: stats,
                Types: pokemonTypes,
                SelectedAbility: ability,
                SelectedMoves: new PokemonMoveset(),
                Resistances: [],
                Weaknesses: [],
                STABCoverage: [],
                MoveCoverage: []
            );

            // the JSON constructor leaves types/multipliers unresolved (in the app, the
            // SmartPokemonJsonConverter does this immediately after deserializing)
            pokemon.InitializeTypes(typeChart, BoxRules.Unrestricted());

            return pokemon;
        }

        // Builds a SmartPokemon with no real type-chart resolution at all (Types is empty, so
        // UpdateMultipliers has nothing to compute) and lets the caller inject exact
        // Defense/Attack/move-coverage multipliers directly. Use this for tests that care about
        // team-level scoring math (TeamScorer.CalculateScore) rather than SmartPokemon's own
        // type-effectiveness logic - it sidesteps DataModelCache entirely.
        public static SmartPokemon MakeScoringPokemon(
            string name,
            Dictionary<string, double>? defense = null,
            Dictionary<string, double>? attack = null,
            IEnumerable<string>? moveCoverage = null,
            Dictionary<string, int>? baseStats = null,
            int id = 0
        )
        {
            // types are empty so the chart is never consulted - a throwaway empty one suffices,
            // keeping scoring-test call sites free of chart plumbing they don't care about
            SmartPokemon pokemon = MakePokemon(new TypeChart(), name, types: [], abilityName: null, baseStats: baseStats, id: id);

            Multipliers multipliers = pokemon.GetMultipliers(RulesetId.Unrestricted);

            if (defense is not null)
            {
                foreach ((string type, double value) in defense)
                {
                    multipliers.Defense[type] = value;
                }
            }

            if (attack is not null)
            {
                foreach ((string type, double value) in attack)
                {
                    multipliers.Attack[type] = value;
                }
            }

            if (moveCoverage is not null)
            {
                foreach (string type in moveCoverage)
                {
                    pokemon.SelectedMoves.AttackMultipliers[type] = 2.0;
                }
            }

            return pokemon;
        }
    }
}
