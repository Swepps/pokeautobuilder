using System.Data;
using System.Text.Json.Serialization;
using PokeApiNet;
using Utility;

namespace PokemonDataModel
{
    using Type = PokeApiNet.Type;

    public class Multipliers
    {
        public Dictionary<string, double> Defense;
        public Dictionary<string, double> Attack;

        public Multipliers()
        {
            Defense = new Dictionary<string, double>();
            Attack = new Dictionary<string, double>();
        }

        public void Clear()
        {
            Defense.Clear();
            Attack.Clear();
        }
    }

    public class SmartPokemon : Pokemon, IPokemonSearchable
    {
        public PokemonAbility? SelectedAbility { get; set; }
        public PokemonMoveset SelectedMoves { get; set; }

        public List<string> Resistances { get; init; }
        public List<string> Weaknesses { get; init; }
        public List<string> STABCoverage { get; init; }
        public List<string> MoveCoverage { get; init; }

        [JsonIgnore]
        public List<Type> LoadedTypes { get; set; }

        [JsonIgnore]
        public Multipliers Multipliers { get; init; }

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
            PokeApiService apiService
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

            return new SmartPokemon(basePokemon, species, generation);
        }

        public SmartPokemon(Pokemon pokemon, PokemonSpecies loadedSpecies, Generation generation)
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

            // get the loaded types
            LoadedTypes = new();
            foreach (PokemonType t in Types)
            {
                LoadedTypes.Add(DataModelCache.LoadedTypes.First(lt => lt.Name == t.Type.Name));
            }

            // smart variables that make this pokemon class more useful
            SelectedAbility = Abilities.FirstOrDefault();
            SelectedMoves = new PokemonMoveset();
            Multipliers = new Multipliers();
            UpdateMultipliers(); // needs to be done before lists can be generated but after ability is selected
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

            // get the loaded types
            LoadedTypes = new();
            foreach (PokemonType t in this.Types)
            {
                LoadedTypes.Add(DataModelCache.LoadedTypes.First(lt => lt.Name == t.Type.Name));
            }

            Multipliers = new Multipliers();
            UpdateMultipliers();
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

        public double GetResistance(string typeName)
        {
            if (Multipliers.Defense.TryGetValue(typeName, out double attEff))
            {
                return attEff;
            }
            else
            {
                return 1.0;
            }
        }

        public bool IsTypeCoveredBySTAB(string typeName)
        {
            if (Multipliers.Attack.TryGetValue(typeName, out double defEff))
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
                UpdateMultipliers();
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

        public int GetBaseStat(string statName)
        {
            PokemonStat? stat = Stats.Find(x => x.Stat.Name == statName);
            if (stat == null)
                return -1;

            return stat.BaseStat;
        }

        public double[] GetBaseStatsArray()
        {
            double[] stats = new double[6];

            // make sure we're getting the correct stat in each position
            stats[0] = GetBaseStat("hp");
            stats[1] = GetBaseStat("attack");
            stats[2] = GetBaseStat("special-attack");
            stats[3] = GetBaseStat("defense");
            stats[4] = GetBaseStat("special-defense");
            stats[5] = GetBaseStat("speed");

            return stats;
        }

        public int GetBaseStatsTotal()
        {
            int total = 0;
            foreach (PokemonStat stat in Stats)
            {
                total += stat.BaseStat;
            }

            return total;
        }

        public int CountTotalCoverage()
        {
            int count = 0;
            foreach (string type in Globals.AllTypes)
            {
                if (IsTypeCoveredBySTAB(type) || IsTypeCoveredByMove(type))
                    count++;
            }

            return count;
        }

        public int CountTotalResistances()
        {
            int count = 0;
            foreach (string type in Globals.AllTypes)
            {
                if (Multipliers.Defense.TryGetValue(type, out double value) && value < 1.0)
                {
                    count++;
                }
            }

            return count;
        }

        private void UpdateMultipliers()
        {
            Multipliers.Clear();

            foreach (Type type in LoadedTypes)
            {
                // get lists of type name to check effectivenesses
                // don't need full type details, just the names
                TypeRelations tr = type.DamageRelations;
                var noDamageTo = tr.NoDamageTo;
                var noDamageFrom = tr.NoDamageFrom;
                var halfDamageTo = tr.HalfDamageTo;
                var halfDamageFrom = tr.HalfDamageFrom;
                var doubleDamageTo = tr.DoubleDamageTo;
                var doubleDamageFrom = tr.DoubleDamageFrom;

                // immune types
                foreach (var namedType in noDamageTo)
                {
                    if (Multipliers.Attack.ContainsKey(namedType.Name))
                    {
                        Multipliers.Attack[namedType.Name] = Math.Max(
                            Multipliers.Attack[namedType.Name],
                            0
                        );
                    }
                    else
                    {
                        Multipliers.Attack[namedType.Name] = 0;
                    }
                }
                foreach (var namedType in noDamageFrom)
                {
                    // always set this to 0
                    Multipliers.Defense[namedType.Name] = 0;
                }

                // resistant types
                foreach (var namedType in halfDamageTo)
                {
                    if (Multipliers.Attack.ContainsKey(namedType.Name))
                    {
                        Multipliers.Attack[namedType.Name] = Math.Max(
                            Multipliers.Attack[namedType.Name],
                            0.5
                        );
                    }
                    else
                    {
                        Multipliers.Attack[namedType.Name] = 0.5;
                    }
                }
                foreach (var namedType in halfDamageFrom)
                {
                    if (Multipliers.Defense.ContainsKey(namedType.Name))
                    {
                        Multipliers.Defense[namedType.Name] =
                            Multipliers.Defense[namedType.Name] * 0.5;
                    }
                    else
                    {
                        Multipliers.Defense[namedType.Name] = 0.5;
                    }
                }

                // super effective types
                foreach (var namedType in doubleDamageTo)
                {
                    if (Multipliers.Attack.ContainsKey(namedType.Name))
                    {
                        Multipliers.Attack[namedType.Name] = Math.Max(
                            Multipliers.Attack[namedType.Name],
                            2.0
                        );
                    }
                    else
                    {
                        Multipliers.Attack[namedType.Name] = 2.0;
                    }
                }
                foreach (var namedType in doubleDamageFrom)
                {
                    if (Multipliers.Defense.ContainsKey(namedType.Name))
                    {
                        Multipliers.Defense[namedType.Name] =
                            Multipliers.Defense[namedType.Name] * 2.0;
                    }
                    else
                    {
                        Multipliers.Defense[namedType.Name] = 2.0;
                    }
                }
            }

            if (SelectedAbility is not null)
            {
                AbilityEffects.Apply(SelectedAbility.Ability.Name, Multipliers);
            }
        }

        private List<string> GetDefenseResistList()
        {
            List<string> ret = new List<string>();
            foreach (string type in Globals.AllTypes)
            {
                double eff = GetResistance(type);

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
                if (GetResistance(type) > 1.0)
                    ret.Add(type);
            }

            return ret;
        }

        private List<string> GetSTABCoverageList()
        {
            List<string> ret = new List<string>();
            foreach (string type in Globals.AllTypes)
            {
                if (IsTypeCoveredBySTAB(type))
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
