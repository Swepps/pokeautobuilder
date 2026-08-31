using System.Data;
using System.Text.Json.Serialization;
using PokeApiNet;
using Utility;

namespace PokemonDataModel
{
    using Type = PokeApiNet.Type;

    public class SmartPokemon : Pokemon, IPokemonSearchable
    {
        public PokemonAbility? SelectedAbility { get; set; }
        public PokemonMoveset SelectedMoves { get; set; }

        public List<string> Resistances { get; init; }
        public List<string> Weaknesses { get; init; }
        public List<string> STABCoverage { get; init; }
        public List<string> MoveCoverage { get; init; }

        // Bundles a resolved-types snapshot with its multipliers for one ruleset. A Pokemon can be
        // attached to boxes/teams under different rulesets at once (it's shared by reference across
        // them), so each ruleset gets its own independent entry rather than one ambient pair of
        // fields that the most-recently-touched ruleset would clobber.
        private sealed class RulesetCache
        {
            public List<Type> LoadedTypes = [];
            public Multipliers Multipliers = new();
        }

        [JsonIgnore]
        private readonly Dictionary<RulesetId, RulesetCache> _rulesetCaches = new();

        [JsonIgnore]
        private PokemonSpecies? _loadedSpecies { get; set; }

        [JsonIgnore]
        private Generation? _generation { get; set; }

        [JsonIgnore]
        public new List<PokemonMove> Moves;

        [JsonIgnore]
        public bool IsMega
        {
            get { return Name.Split('-').Contains("mega"); }
        }

        [JsonIgnore]
        public bool IsGmax
        {
            get { return Name.Split('-').Contains("gmax"); }
        }

        public static async Task<SmartPokemon> BuildSmartPokemonAsync(
            Pokemon basePokemon,
            PokeApiService apiService,
            TypeChart typeChart
        )
        {
            PokemonSpecies? species =
                await apiService.GetPokemonSpeciesAsync(basePokemon.Species.Name)
                ?? throw new Exception(
                    "Could not load species information from " + basePokemon.Name
                );

            Generation? generation =
                await apiService.GetGenerationAsync(species)
                ?? throw new Exception(
                    "Could not load generation information from " + species.Name
                );

            return new SmartPokemon(basePokemon, species, generation, typeChart);
        }

        public SmartPokemon(
            Pokemon pokemon,
            PokemonSpecies loadedSpecies,
            Generation generation,
            TypeChart typeChart
        )
        {
            // build our own copy constructor since we can't cast
            Id = pokemon.Id;
            Name = pokemon.Name;
            BaseExperience = pokemon.BaseExperience;
            Height = pokemon.Height;
            IsDefault = pokemon.IsDefault;
            Order = pokemon.Order;
            Weight = pokemon.Weight;
            Abilities = pokemon.Abilities;
            Forms = pokemon.Forms;
            GameIndicies = pokemon.GameIndicies;
            HeldItems = pokemon.HeldItems;
            LocationAreaEncounters = pokemon.LocationAreaEncounters;
            Moves = []; // load this later because it's huge
            PastTypes = pokemon.PastTypes;
            Sprites = pokemon.Sprites;
            Species = pokemon.Species;
            Stats = pokemon.Stats;
            Types = pokemon.Types;

            // async value collected from builder function
            _loadedSpecies = loadedSpecies;
            _generation = generation;

            // smart variables that make this pokemon class more useful
            SelectedAbility = Abilities.FirstOrDefault();
            SelectedMoves = new PokemonMoveset();
            // needs to be done before lists can be generated but after ability is selected. A newly
            // built Pokemon always starts with its Unrestricted-ruleset entry pre-warmed for free;
            // other rulesets are warmed lazily once the Pokemon is attached to a box/team using them.
            InitializeTypes(typeChart, BoxRules.Unrestricted());
            Resistances = GetDefenseResistList();
            Weaknesses = GetDefenseWeakList();
            STABCoverage = GetSTABCoverageList();
            MoveCoverage = GetMoveCoverageList();
        }

        [JsonConstructor]
        public SmartPokemon(
            int Id,
            string Name,
            int? BaseExperience,
            int Height,
            bool IsDefault,
            int Order,
            int Weight,
            List<PokemonAbility> Abilities,
            List<NamedApiResource<PokemonForm>> Forms,
            List<VersionGameIndex> GameIndicies,
            List<PokemonHeldItem> HeldItems,
            string LocationAreaEncounters,
            List<PokemonMove> Moves,
            List<PokemonPastTypes> PastTypes,
            PokemonSprites Sprites,
            NamedApiResource<PokemonSpecies> Species,
            List<PokemonStat> Stats,
            List<PokemonType> Types,
            PokemonAbility SelectedAbility,
            PokemonMoveset SelectedMoves,
            List<string> Resistances,
            List<string> Weaknesses,
            List<string> STABCoverage,
            List<string> MoveCoverage
        )
        {
            // pokemon member variables
            this.Id = Id;
            this.Name = Name;
            this.BaseExperience = BaseExperience;
            this.Height = Height;
            this.IsDefault = IsDefault;
            this.Order = Order;
            this.Weight = Weight;
            this.Abilities = Abilities;
            this.Forms = Forms;
            this.GameIndicies = GameIndicies;
            this.HeldItems = HeldItems;
            this.LocationAreaEncounters = LocationAreaEncounters;
            this.Moves = new(); // load this as and when because it's huge
            this.PastTypes = PastTypes;
            this.Sprites = Sprites;
            this.Species = Species;
            this.Stats = Stats;
            this.Types = Types;

            // smart pokemon member variables
            this.SelectedAbility = SelectedAbility;
            this.SelectedMoves = SelectedMoves;
            this.Resistances = Resistances;
            this.Weaknesses = Weaknesses;
            this.STABCoverage = STABCoverage;
            this.MoveCoverage = MoveCoverage;

            // the ruleset cache stays empty here - the JSON constructor is invoked by
            // System.Text.Json during deserialization, which can't supply a TypeChart.
            // SmartPokemonJsonConverter (registered on every persistence path) calls
            // InitializeTypes immediately after deserializing, so callers never see an
            // uninitialized instance.
        }

        // Resolves this pokemon's type names into full Type objects (with damage relations) via
        // the given chart and computes/caches the type-effectiveness multipliers for `rules` only -
        // re-callable per ruleset (e.g. once for Unrestricted, again for a box's Gen-1 ruleset), and
        // re-callable for a ruleset already cached to refresh it (e.g. after the chart it was built
        // from changes). Uses GetTypesForGeneration rather than the raw (always-current) Types list,
        // so a Gen-1 ruleset resolves e.g. Clefable as pure Normal, not "Fairy with the type hidden".
        public void InitializeTypes(TypeChart typeChart, BoxRules rules)
        {
            RulesetCache cache = GetOrCreateCache(rules.Id);
            cache.LoadedTypes.Clear();
            foreach (PokemonType t in GetTypesForGeneration(rules.Generation))
            {
                cache.LoadedTypes.Add(typeChart.Resolve(t.Type.Name));
            }

            RecomputeMultipliers(cache);
        }

        // What this Pokemon's types actually were as of the given generation, per PokeAPI's
        // past_types (each entry's Generation is "the final generation in which the Pokemon had
        // this data" - so the applicable entry is the one with the smallest generation number that
        // is still >= the target, and no entry applying at all means the target generation is at or
        // after the current typing). Returns the current Types unchanged when generation is null
        // (Unrestricted) or the Pokemon has no past_types entries (its type has never changed).
        public IReadOnlyList<PokemonType> GetTypesForGeneration(int? generation)
        {
            if (generation is null)
                return Types;

            PokemonPastTypes? applicable = PastTypes
                .Where(pt => PokemonGenerationNames.Parse(pt.Generation.Name) >= generation)
                .OrderBy(pt => PokemonGenerationNames.Parse(pt.Generation.Name))
                .FirstOrDefault();

            return applicable?.Types ?? Types;
        }

        // What this Pokemon's base stats actually were as of the given generation, per PokeAPI's
        // past_stats (PastStatsTable). Unlike GetTypesForGeneration, an entry's stats are a sparse
        // delta rather than a full replacement - e.g. Pikachu's Gen 1 entry only carries "special",
        // its Gen 5 entry only carries defense/special-defense, and a Gen-1 lookup needs both
        // layered together. So every entry with Generation >= target is applied as an overlay onto
        // the current-day stats, working from the *largest* qualifying generation down to the
        // smallest - the smallest (closest to the target) is applied last and wins any conflict,
        // matching GetTypesForGeneration's "smallest qualifying generation" rule while still letting
        // an older, unrelated-stat delta (Gen 1's Special) coexist with a newer one (Gen 5's
        // Defense) instead of one replacing the other outright.
        //
        // "special" (Gen 1's single stat covering both offense and defense) is a distinct key in the
        // table from "special-attack"/"special-defense" - once layering is done, a resolved
        // "special" value is written into both of those keys, so callers never need to know Gen 1
        // worked differently. A Pokemon with no qualifying entries at all for a Gen 1 lookup didn't
        // exist yet in Gen 1 (every Gen-1-native species has an explicit Special entry) - approximated
        // by collapsing special-defense onto the current special-attack value, rather than showing a
        // Gen-1 Pokemon with a stat split that couldn't have existed.
        public IReadOnlyDictionary<string, int> GetStatsForGeneration(int? generation)
        {
            Dictionary<string, int> resolved = Stats.ToDictionary(s => s.Stat.Name, s => s.BaseStat);

            if (generation is null)
                return resolved;

            if (PastStatsTable.ByPokemonId.TryGetValue(Id, out IReadOnlyList<PastStatsEntry>? entries))
            {
                foreach (
                    PastStatsEntry entry in entries
                        .Where(e => e.Generation >= generation)
                        .OrderByDescending(e => e.Generation)
                )
                {
                    foreach ((string statName, int value) in entry.Stats)
                    {
                        resolved[statName] = value;
                    }
                }
            }

            if (resolved.TryGetValue("special", out int special))
            {
                resolved["special-attack"] = special;
                resolved["special-defense"] = special;
                resolved.Remove("special");
            }
            else if (generation == 1)
            {
                resolved["special-defense"] = resolved["special-attack"];
            }

            return resolved;
        }

        // A single base stat for the given generation (null = current day). Throws on an unknown
        // stat name rather than the old sentinel-value (-1) behavior - every caller passes a
        // hardcoded, known-valid name, so a typo should fail loudly instead of silently corrupting
        // a score calculation.
        public int GetBaseStat(string statName, int? generation = null)
        {
            IReadOnlyDictionary<string, int> stats = GetStatsForGeneration(generation);
            if (!stats.TryGetValue(statName, out int value))
                throw new ArgumentException($"Unknown stat name '{statName}'", nameof(statName));

            return value;
        }

        // Base stats shaped for display: HP/Attack/Defense/Speed plus, under a Gen 1 ruleset, a
        // single "Special" bar instead of separate Sp. Atk/Sp. Def ones (Gen 1 never had two - see
        // GetStatsForGeneration) - so a chart iterating this list doesn't need to know about the
        // Gen 1 special case itself. Sp. Atk and Sp. Def are always equal under Gen 1 by
        // construction, so either one stands in for "Special".
        public IReadOnlyList<(string Label, int Value)> GetBaseStatsForDisplay(int? generation)
        {
            IReadOnlyDictionary<string, int> stats = GetStatsForGeneration(generation);

            if (generation == 1)
            {
                return
                [
                    ("HP", stats["hp"]),
                    ("Attack", stats["attack"]),
                    ("Special", stats["special-attack"]),
                    ("Defense", stats["defense"]),
                    ("Speed", stats["speed"]),
                ];
            }

            return
            [
                ("HP", stats["hp"]),
                ("Attack", stats["attack"]),
                ("Sp. Atk", stats["special-attack"]),
                ("Defense", stats["defense"]),
                ("Sp. Def", stats["special-defense"]),
                ("Speed", stats["speed"]),
            ];
        }

        public int GetBaseStatsTotal(int? generation = null)
        {
            return GetBaseStatsForDisplay(generation).Sum(s => s.Value);
        }

        public bool HasInitializedRuleset(RulesetId rulesetId) => _rulesetCaches.ContainsKey(rulesetId);

        public Multipliers GetMultipliers(RulesetId rulesetId)
        {
            if (_rulesetCaches.TryGetValue(rulesetId, out RulesetCache? cache))
            {
                return cache.Multipliers;
            }

            throw new InvalidOperationException(
                $"Multipliers for ruleset '{rulesetId.Value}' have not been computed for {Name} - "
                    + "call InitializeTypes for this ruleset first."
            );
        }

        private RulesetCache GetOrCreateCache(RulesetId rulesetId)
        {
            if (!_rulesetCaches.TryGetValue(rulesetId, out RulesetCache? cache))
            {
                cache = new RulesetCache();
                _rulesetCaches[rulesetId] = cache;
            }

            return cache;
        }

        public async Task<PokemonSpecies> GetSpeciesAsync(PokeApiService apiService)
        {
            if (_loadedSpecies is null)
                await LoadFromAPI(apiService);

            return _loadedSpecies!;
        }

        public async Task<Generation> GetGenerationAsync(PokeApiService apiService)
        {
            if (_generation is null)
                await LoadFromAPI(apiService);

            return _generation!;
        }

        public async Task<List<PokemonMove>> GetMovesAsync(PokeApiService apiService)
        {
            if (Moves is null || Moves.Count == 0)
                await LoadFromAPI(apiService);

            return Moves!;
        }

        public async Task LoadFromAPI(PokeApiService apiService)
        {
            _loadedSpecies =
                await apiService.GetPokemonSpeciesAsync(Species.Name)
                ?? throw new Exception("Could not load species information from " + Name);
            _generation =
                await apiService.GetGenerationAsync(_loadedSpecies)
                ?? throw new Exception(
                    "Could not load generation information from " + Species.Name
                );
            Moves = await apiService.GetPokemonMovesAsync(this);
        }

        public async Task<List<PokemonMove>> SearchAvailableMoves(
            string searchTerm,
            PokeApiService apiService
        )
        {
            List<PokemonMove> results = await GetMovesAsync(apiService);
            results = results
                .Where(move => move.Move.Name.Contains(searchTerm))
                .OrderBy(move => move.Move.Name)
                .ToList();
            return results;
        }

        public async Task<PokemonMove?> GetSelectedMoveResource(int index, PokeApiService apiService)
        {
            if (index < 0 || index >= PokemonMoveset.MaxMovesetSize)
                return null;
            if (SelectedMoves.GetMoveNames()[index] is null)
                return null;

            string? moveName = SelectedMoves.GetMoveNames()[index]!;
            List<PokemonMove> moves = await GetMovesAsync(apiService);
            return moves.Find(m => m.Move.Name == moveName);
        }

        public double GetResistance(string typeName, RulesetId rulesetId)
        {
            if (GetMultipliers(rulesetId).Defense.TryGetValue(typeName, out double attEff))
            {
                return attEff;
            }
            else
            {
                return 1.0;
            }
        }

        public bool IsTypeCoveredBySTAB(string typeName, RulesetId rulesetId)
        {
            if (GetMultipliers(rulesetId).Attack.TryGetValue(typeName, out double defEff))
            {
                return defEff >= 2.0;
            }
            else
            {
                return false;
            }
        }

        public bool IsTypeCoveredByMove(string typeName)
        {
            return SelectedMoves.HasCoverageAgainst(typeName);
        }

        public bool SelectAbility(string abilityName)
        {
            PokemonAbility? ab = Abilities
                .Where(a => a.Ability.Name == abilityName)
                .FirstOrDefault();
            if (ab != null)
            {
                SelectedAbility = ab;
                // recompute every ruleset this Pokemon has ever been evaluated against - each cache
                // entry already has its own resolved LoadedTypes, so no TypeChart is needed here
                foreach (RulesetCache cache in _rulesetCaches.Values)
                {
                    RecomputeMultipliers(cache);
                }
                return true;
            }
            return false;
        }

        public async Task<bool> SelectMoveAsync(int index, Move? move, PokeApiService apiService)
        {
            if (index >= 0 && index < PokemonMoveset.MaxMovesetSize)
            {
                await SelectedMoves.SetAt(index, move, apiService);
                return true;
            }
            return false;
        }

        public int CountTotalCoverage(BoxRules rules)
        {
            int count = 0;
            foreach (string type in rules.EnabledTypes)
            {
                if (IsTypeCoveredBySTAB(type, rules.Id) || IsTypeCoveredByMove(type))
                    count++;
            }

            return count;
        }

        public int CountTotalResistances(BoxRules rules)
        {
            int count = 0;
            foreach (string type in rules.EnabledTypes)
            {
                if (GetMultipliers(rules.Id).Defense.TryGetValue(type, out double value) && value < 1.0)
                {
                    count++;
                }
            }

            return count;
        }

        private void RecomputeMultipliers(RulesetCache cache)
        {
            cache.Multipliers.Clear();

            foreach (Type type in cache.LoadedTypes)
            {
                TypeRelations tr = type.DamageRelations;

                TypeEffectiveness.ApplyOffensiveRelations(cache.Multipliers.Attack, tr);

                // defensive side is unique to a Pokemon's own types (movesets don't have one), and
                // combines multiplicatively rather than taking the max, since a dual-type Pokemon's
                // resistances/weaknesses stack (e.g. 4x weak when both types are weak to the same type)
                foreach (var namedType in tr.NoDamageFrom)
                {
                    // always set this to 0
                    cache.Multipliers.Defense[namedType.Name] = 0;
                }
                foreach (var namedType in tr.HalfDamageFrom)
                {
                    cache.Multipliers.Defense[namedType.Name] = cache.Multipliers.Defense.TryGetValue(
                        namedType.Name,
                        out double existingHalf
                    )
                        ? existingHalf * 0.5
                        : 0.5;
                }
                foreach (var namedType in tr.DoubleDamageFrom)
                {
                    cache.Multipliers.Defense[namedType.Name] = cache.Multipliers.Defense.TryGetValue(
                        namedType.Name,
                        out double existingDouble
                    )
                        ? existingDouble * 2.0
                        : 2.0;
                }
            }

            if (SelectedAbility is not null)
            {
                AbilityEffects.Apply(SelectedAbility.Ability.Name, cache.Multipliers);
            }
        }

        private List<string> GetDefenseResistList()
        {
            List<string> ret = new List<string>();
            foreach (string type in Globals.AllTypes)
            {
                double eff = GetResistance(type, RulesetId.Unrestricted);

                if (eff < 1.0 && eff > 0)
                    ret.Add(type);
            }

            return ret;
        }

        private List<string> GetDefenseWeakList()
        {
            List<string> ret = new List<string>();
            foreach (string type in Globals.AllTypes)
            {
                if (GetResistance(type, RulesetId.Unrestricted) > 1.0)
                    ret.Add(type);
            }

            return ret;
        }

        private List<string> GetSTABCoverageList()
        {
            List<string> ret = new List<string>();
            foreach (string type in Globals.AllTypes)
            {
                if (IsTypeCoveredBySTAB(type, RulesetId.Unrestricted))
                    ret.Add(type);
            }

            return ret;
        }

        private List<string> GetMoveCoverageList()
        {
            List<string> ret = new List<string>();
            foreach (string type in Globals.AllTypes)
            {
                if (IsTypeCoveredByMove(type))
                    ret.Add(type);
            }

            return ret;
        }

        public override string ToString()
        {
            return StringUtils.PrettifyString(Name);
        }

        public IEnumerable<NamedApiResource<Pokemon>> GetAllVarieties()
        {
            if (_loadedSpecies is null)
            {
                return [];
            }

            List<NamedApiResource<Pokemon>> varieties = [];

            foreach (PokemonSpeciesVariety variety in _loadedSpecies.Varieties)
            {
                varieties.Add(variety.Pokemon);
            }

            return varieties;
        }

        public async Task<IEnumerable<NamedApiResource<Pokemon>>> GetAllVarietiesAsync(
            PokeApiService apiService
        )
        {
            PokemonSpecies species = await GetSpeciesAsync(apiService);

            List<NamedApiResource<Pokemon>> varieties = [];

            foreach (PokemonSpeciesVariety variety in species.Varieties)
            {
                varieties.Add(variety.Pokemon);
            }

            return varieties;
        }
    }
}
