using PokeApiNet;
using PokemonDataModel;
using Utility;

namespace PokeAutobuilderTests
{
    using Type = PokeApiNet.Type;

    // Builds SmartPokemon instances entirely offline (no PokeApiService calls), so business-logic
    // tests don't depend on network access or PokeAPI's actual data.
    internal static class TestFixtures
    {
        // DataModelCache.LoadedTypes is a single process-wide cache shared by every test class, and
        // the network-backed tests use "do we already have all the real types" to decide whether to
        // fetch them. MakeType below adds synthetic types to that same cache, so this must check for
        // (and only add) the specific real types that are missing rather than relying on emptiness -
        // otherwise a test class that runs first and adds a couple of fake types would make the
        // "already loaded" check look satisfied and the real network-backed tests would never
        // actually fetch real type data.
        public static async Task EnsureRealTypesLoadedAsync(PokeApiService apiService)
        {
            if (Globals.AllTypes.All(name => DataModelCache.LoadedTypes.Any(t => t.Name == name)))
            {
                return;
            }

            List<Type> realTypes = await apiService.GetAllTypesAsync();
            foreach (Type type in realTypes)
            {
                if (!DataModelCache.LoadedTypes.Any(t => t.Name == type.Name))
                {
                    DataModelCache.LoadedTypes.Add(type);
                }
            }
        }

        // Registers a fake type in DataModelCache.LoadedTypes so SmartPokemon can resolve it.
        // Callers should use names that can't collide with the real type names the network-backed
        // tests in this project load (see Utility.Globals.AllTypes), e.g. prefix with "test-".
        public static Type MakeType(
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

            if (!DataModelCache.LoadedTypes.Any(t => t.Name == name))
            {
                DataModelCache.LoadedTypes.Add(type);
            }

            return type;
        }

        private static List<NamedApiResource<Type>> ToResourceList(IEnumerable<string>? names)
        {
            return (names ?? []).Select(n => new NamedApiResource<Type> { Name = n }).ToList();
        }

        // Builds a SmartPokemon with the given types/ability, exercising the real UpdateMultipliers
        // logic (type-effectiveness aggregation + ability overrides). Types must already be
        // registered via MakeType.
        public static SmartPokemon MakePokemon(
            string name,
            IEnumerable<Type> types,
            string? abilityName = null,
            Dictionary<string, int>? baseStats = null
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

            return new SmartPokemon(
                Id: 0,
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
        }

        // Builds a SmartPokemon with no real type-chart resolution at all (Types is empty, so
        // UpdateMultipliers has nothing to compute) and lets the caller inject exact
        // Defense/Attack/move-coverage multipliers directly. Use this for tests that care about
        // team-level scoring math (AutoBuilder.CalculateScore) rather than SmartPokemon's own
        // type-effectiveness logic - it sidesteps DataModelCache entirely.
        public static SmartPokemon MakeScoringPokemon(
            string name,
            Dictionary<string, double>? defense = null,
            Dictionary<string, double>? attack = null,
            IEnumerable<string>? moveCoverage = null,
            Dictionary<string, int>? baseStats = null
        )
        {
            SmartPokemon pokemon = MakePokemon(name, types: [], abilityName: null, baseStats: baseStats);

            if (defense is not null)
            {
                foreach ((string type, double value) in defense)
                {
                    pokemon.Multipliers.Defense[type] = value;
                }
            }

            if (attack is not null)
            {
                foreach ((string type, double value) in attack)
                {
                    pokemon.Multipliers.Attack[type] = value;
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
