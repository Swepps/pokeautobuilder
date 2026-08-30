using System.Text.Json.Serialization;
using Utility;

namespace PokemonDataModel
{
    // A flat, fully-resolved set of rules a box (or a team built from one) plays under. Presets
    // (e.g. ForGeneration) just return a concrete instance - "customizing a preset" means copying
    // and editing the returned record, there's no merge/override resolution anywhere.
    public sealed record BoxRules
    {
        public const int CurrentSchemaVersion = 1;

        public int SchemaVersion { get; init; } = CurrentSchemaVersion;

        // UI label only (e.g. "Generation 5") - carries no resolution behavior of its own.
        public string? PresetId { get; init; }

        public List<string> DisabledTypes { get; init; } = [];

        // Which generation this ruleset represents, if any - drives which game/Pokedex search
        // locations are considered compatible with a box using this ruleset (see
        // PokemonStoragePage's search-location filtering). Null for Unrestricted (anything goes)
        // or a future fully-custom ruleset with no single generation identity. Kept separate from
        // PresetId (a UI label with no resolution behavior) because this one does drive behavior.
        public int? Generation { get; init; }

        // Whether the mechanic exists at all in this ruleset - Mega Evolution and Gigantamax are
        // generation-specific features, not universal ones like "allow multiple". Distinct from
        // AllowMultipleMegas/Gmax below: that pair only matters once the mechanic is allowed at all.
        public bool AllowMegas { get; init; } = true;
        public bool AllowGmax { get; init; } = true;

        public bool AllowMultipleMegas { get; init; }
        public bool AllowMultipleGmax { get; init; }

        // Computed, not stored - always re-derivable from DisabledTypes, so keeping these out of
        // persisted JSON keeps a saved box/team's data minimal and avoids two derived values
        // silently going stale relative to DisabledTypes if it's ever hand-edited.
        [JsonIgnore]
        public IReadOnlyList<string> EnabledTypes =>
            Globals.AllTypes.Where(t => !DisabledTypes.Contains(t)).ToList();

        // Identity/cache key for derived TypeCharts, per-ruleset Pokemon multiplier caches, and
        // ruleset-equality comparisons elsewhere (e.g. whether switching search location in the
        // team builder counts as a different ruleset). Deliberately excludes SchemaVersion/
        // PresetId, neither of which affects behavior on its own. Generation IS included despite
        // being derivable-adjacent to DisabledTypes - historical type resolution (a species' actual
        // types in a given generation, e.g. Clefable as Normal pre-Gen 6) depends on the exact
        // generation number, not just which types are disabled, and several generations (2-5) share
        // an identical DisabledTypes/AllowMegas/AllowGmax combination despite being meaningfully
        // different rulesets for that purpose.
        [JsonIgnore]
        public RulesetId Id =>
            new(
                string.Join(",", DisabledTypes.OrderBy(t => t, StringComparer.Ordinal))
                    + "|gen="
                    + (Generation?.ToString() ?? "none")
                    + "|megas="
                    + AllowMegas
                    + "|multiMegas="
                    + AllowMultipleMegas
                    + "|gmax="
                    + AllowGmax
                    + "|multiGmax="
                    + AllowMultipleGmax
            );

        public static BoxRules Unrestricted(
            bool allowMultipleMegas = false,
            bool allowMultipleGmax = false
        ) =>
            new()
            {
                DisabledTypes = [],
                AllowMegas = true,
                AllowGmax = true,
                AllowMultipleMegas = allowMultipleMegas,
                AllowMultipleGmax = allowMultipleGmax,
            };

        // Disables types that didn't exist yet in the given generation: Dark/Steel arrived in Gen
        // II, Fairy in Gen VI. No other type in Globals.AllTypes has ever been added or removed.
        // Mega Evolution exists in Gen 6-7; Gigantamax only in Gen 8 - both approximated at the
        // generation level for now (e.g. Let's Go Pikachu/Eevee and Legends: Arceus/BDSP are
        // classified under Gen 7/8 respectively by PokeAPI despite lacking Mega/Gmax in-game; a
        // future customizable ruleset can override this per-box).
        public static BoxRules ForGeneration(
            int generation,
            bool allowMultipleMegas = false,
            bool allowMultipleGmax = false
        )
        {
            List<string> disabled = [];
            if (generation < 2)
                disabled.AddRange(["dark", "steel"]);
            if (generation < 6)
                disabled.Add("fairy");

            return new BoxRules
            {
                PresetId = $"Generation {generation}",
                Generation = generation,
                DisabledTypes = disabled,
                AllowMegas = generation is 6 or 7,
                AllowGmax = generation is 8,
                AllowMultipleMegas = allowMultipleMegas,
                AllowMultipleGmax = allowMultipleGmax,
            };
        }
    }

    // Small value-equatable wrapper so a BoxRules' identity can key a Dictionary directly.
    public readonly record struct RulesetId(string Value)
    {
        public static readonly RulesetId Unrestricted = BoxRules.Unrestricted().Id;
    }
}
