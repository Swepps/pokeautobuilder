using PokeApiNet;

namespace PokemonDataModel
{
    using Type = PokeApiNet.Type;

    // Holds the loaded Pokemon types (with their damage relations) for resolving a Pokemon's type
    // names into full Type objects. An instance class (registered in DI, populated by
    // ProfileService at startup) rather than a global static, so tests can use isolated charts and
    // future features can build alternative charts (e.g. per-generation type effectiveness).
    public class TypeChart
    {
        private readonly Dictionary<string, Type> _typesByName = new();
        private readonly Dictionary<RulesetId, TypeChart> _derivedByRuleset = new();

        public bool IsEmpty => _typesByName.Count == 0;

        public IReadOnlyCollection<Type> All => _typesByName.Values;

        public void Populate(IEnumerable<Type> types)
        {
            _typesByName.Clear();
            foreach (Type type in types)
            {
                _typesByName[type.Name] = type;
            }
        }

        public void Add(Type type)
        {
            _typesByName[type.Name] = type;
        }

        public Type Resolve(string typeName)
        {
            if (_typesByName.TryGetValue(typeName, out Type? type))
            {
                return type;
            }

            throw new InvalidOperationException(
                $"Type '{typeName}' is not present in the type chart"
                    + (IsEmpty ? " (the chart has not been populated)" : "")
            );
        }

        // Builds (or returns the cached) chart for a ruleset that disables some types: every
        // remaining type has the disabled names stripped out of its own damage relations (e.g. so
        // disabling fairy also removes it from dragon's double_damage_from - PokeAPI's data has no
        // historical override for that, it would otherwise silently leak through), and a disabled
        // type itself stays resolvable but with all its relations zeroed out (treated as neutral)
        // rather than throwing - nothing prevents a generation-incompatible Pokemon from sitting in
        // a box/team under this ruleset, and going neutral is far less disruptive than crashing.
        // Only ever called on the base (non-derived) chart instance.
        public TypeChart GetOrBuildDerived(RulesetId rulesetId, IReadOnlyCollection<string> disabledTypes)
        {
            if (disabledTypes.Count == 0)
            {
                return this;
            }

            if (_derivedByRuleset.TryGetValue(rulesetId, out TypeChart? cached))
            {
                return cached;
            }

            TypeChart derived = new();
            foreach (Type type in All)
            {
                bool isDisabled = disabledTypes.Contains(type.Name);
                TypeRelations relations = isDisabled
                    ? new TypeRelations
                    {
                        NoDamageTo = [],
                        HalfDamageTo = [],
                        DoubleDamageTo = [],
                        NoDamageFrom = [],
                        HalfDamageFrom = [],
                        DoubleDamageFrom = [],
                    }
                    : new TypeRelations
                    {
                        NoDamageTo = Strip(type.DamageRelations.NoDamageTo, disabledTypes),
                        HalfDamageTo = Strip(type.DamageRelations.HalfDamageTo, disabledTypes),
                        DoubleDamageTo = Strip(type.DamageRelations.DoubleDamageTo, disabledTypes),
                        NoDamageFrom = Strip(type.DamageRelations.NoDamageFrom, disabledTypes),
                        HalfDamageFrom = Strip(type.DamageRelations.HalfDamageFrom, disabledTypes),
                        DoubleDamageFrom = Strip(type.DamageRelations.DoubleDamageFrom, disabledTypes),
                    };

                derived.Add(new Type
                {
                    Id = type.Id,
                    Name = type.Name,
                    DamageRelations = relations,
                });
            }

            _derivedByRuleset[rulesetId] = derived;
            return derived;
        }

        private static List<NamedApiResource<Type>> Strip(
            List<NamedApiResource<Type>> relations,
            IReadOnlyCollection<string> disabledTypes
        ) => relations.Where(r => !disabledTypes.Contains(r.Name)).ToList();
    }
}
