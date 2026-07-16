using PokeApiNet;

namespace PokemonDataModel
{
    using Type = PokeApiNet.Type;

    // Shared "which types does this hit super/not-very/not effectively" merge logic used by both
    // a Pokemon's own attacking types (SmartPokemon.Multipliers.Attack) and a moveset's move types
    // (PokemonMoveset.AttackMultipliers) - the offensive half of type-effectiveness is identical in
    // both cases, just applied to a different underlying list of types.
    public static class TypeEffectiveness
    {
        public static void ApplyOffensiveRelations(Dictionary<string, double> attackMultipliers, TypeRelations relations)
        {
            MergeMax(attackMultipliers, relations.NoDamageTo, 0.0);
            MergeMax(attackMultipliers, relations.HalfDamageTo, 0.5);
            MergeMax(attackMultipliers, relations.DoubleDamageTo, 2.0);
        }

        // takes the highest effectiveness found so far for each affected type - relevant when a
        // dual-type Pokemon or a multi-move moveset can hit the same target type more than once
        // at different effectiveness
        private static void MergeMax(
            Dictionary<string, double> multipliers,
            IEnumerable<NamedApiResource<Type>> types,
            double value
        )
        {
            foreach (NamedApiResource<Type> namedType in types)
            {
                multipliers[namedType.Name] = multipliers.TryGetValue(namedType.Name, out double existing)
                    ? Math.Max(existing, value)
                    : value;
            }
        }
    }
}
