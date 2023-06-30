using Accord.MachineLearning;
using Accord.Math.Random;
using Newtonsoft.Json;
using PokeApiNet;
using pokeAutoBuilder.Source.Services;
using System.Collections.ObjectModel;

namespace pokeAutoBuilder.Source
{
    using Type = PokeApiNet.Type;

    public struct Multipliers
    {
        public Dictionary<string, double> Defense;
        public Dictionary<string, double> Attack;

        public Multipliers()
        {
            Defense = new Dictionary<string, double>();
            Attack = new Dictionary<string, double>();
        }
    }

    public class SmartPokemonSerializable
    {
        public string Name { get; init; }
        public string SelectedAbility { get; init; }
        public List<string> SelectedMoves { get; init; }

        [JsonConstructor]
        public SmartPokemonSerializable(string Name, string SelectedAbility, List<string> SelectedMoves)
        {
            this.Name = Name;
            this.SelectedAbility = SelectedAbility;
            this.SelectedMoves = SelectedMoves;
        }

        public SmartPokemonSerializable(SmartPokemon p)
        {
            Name = p.Name;
            SelectedAbility = p.SelectedAbility.Ability.Name;
            SelectedMoves = new List<string>();
            foreach (Move? m in p.SelectedMoves)
            {
                if (m is null)
                    SelectedMoves.Add("");
                else
                    SelectedMoves.Add(m.Name);
            }
        }

        public SmartPokemonSerializable()
        {
            Name = "";
            SelectedAbility = "";
            SelectedMoves = new List<string>();
            for (int i = 0; i < PokemonMoveset.MaxMovesetSize; i++)
            {
                SelectedMoves.Add("");
            }
        }

        public async Task<SmartPokemon?> GetSmartPokemon()
        {
            // if it's an empty serialization then return a null pokemon
            if (string.IsNullOrEmpty(Name))
                return null;

            Pokemon? basePokemon = await PokeApiService.Instance!.GetPokemonAsync(Name);
            if (basePokemon == null)
                throw new Exception("Could not load pokemon information for");

            SmartPokemon sp = await SmartPokemon.BuildSmartPokemonAsync(basePokemon);
            sp.SelectAbility(SelectedAbility);

            PokemonMoveset moves = new PokemonMoveset();
            for (int i = 0; i < PokemonMoveset.MaxMovesetSize; i++)
            {
                string moveName = SelectedMoves[i];
                if (!string.IsNullOrEmpty(moveName))
                {
                    sp.SelectMove(i, await PokeApiService.Instance!.GetMoveAsync(moveName));
                }
            }

            return sp;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;

            SmartPokemonSerializable? objPoke = obj as SmartPokemonSerializable;
            if (objPoke == null) return false;

            // check name
            if (objPoke.Name != Name) return false;

            // check selected ability
            if (objPoke.SelectedAbility != SelectedAbility) return false;

            // check selected moves
            for (int i = 0; i < PokemonMoveset.MaxMovesetSize; i++)
            {
                if (objPoke.SelectedMoves[i] != SelectedMoves[i]) return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int code = Name.GetHashCode();

            return code;
        }
    }

    public class SmartPokemon : Pokemon
    {
        public PokemonAbility SelectedAbility { get; set; }
        public PokemonMoveset SelectedMoves { get; set; }

        public PokemonSpecies LoadedSpecies { get; set; }
        public List<Type> LoadedTypes { get; set; }
        public Generation Generation { get; set; }
        public Multipliers Multipliers { get; init; }
        public List<string> Resistances { get; init; }
        public List<string> Weaknesses { get; init; }
        public List<string> Coverage { get; init; }

        public static async Task<SmartPokemon> BuildSmartPokemonAsync(Pokemon basePokemon)
        {
            PokemonSpecies? species = await PokeApiService.Instance!.GetPokemonSpeciesAsync(basePokemon.Species.Name);
            if (species is null)
                throw new Exception("Could not load species information from " + basePokemon.Name);

            List<Type> types = await PokeApiService.Instance!.GetPokemonTypesAsync(basePokemon);
            if (types.Count == 0)
                throw new Exception("Could not load type information from " + basePokemon.Name);

            Generation? generation = await PokeApiService.Instance!.GetGenerationAsync(species);
            if (generation is null)
                throw new Exception("Could not load generation information from " + species.Name);

            return new SmartPokemon(basePokemon, species, types, generation);
        }


        public SmartPokemon(Pokemon pokemon, PokemonSpecies loadedSpecies, List<Type> loadedTypes, Generation generation)
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
            Moves = pokemon.Moves;
            PastTypes = pokemon.PastTypes;
            Sprites = pokemon.Sprites;
            Species = pokemon.Species;
            Stats = pokemon.Stats;
            Types = pokemon.Types;

            // async value collected from builder function
            LoadedSpecies = loadedSpecies;
            LoadedTypes = loadedTypes;
            Generation = generation;
            Multipliers = new Multipliers();
            UpdateMultipliers();

            // smart variables that make this pokemon class more useful
            SelectedAbility = Abilities[0];
            SelectedMoves = new PokemonMoveset();
            Resistances = GetDefenseResistList();
            Weaknesses = GetDefenseWeakList();
            Coverage = GetCoverageList();
        }

        private void UpdateMultipliers()
        {
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
                        Multipliers.Attack[namedType.Name] = Math.Max(Multipliers.Attack[namedType.Name], 0);
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
                        Multipliers.Attack[namedType.Name] = Math.Max(Multipliers.Attack[namedType.Name], 0.5);
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
                        Multipliers.Defense[namedType.Name] = Multipliers.Defense[namedType.Name] * 0.5;
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
                        Multipliers.Attack[namedType.Name] = Math.Max(Multipliers.Attack[namedType.Name], 2.0);
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
                        Multipliers.Defense[namedType.Name] = Multipliers.Defense[namedType.Name] * 2.0;
                    }
                    else
                    {
                        Multipliers.Defense[namedType.Name] = 2.0;
                    }
                }
            }
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

        public bool IsCoveredBySTAB(string typeName)
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

        public bool SelectAbility(string abilityName)
        {
            PokemonAbility? ab = Abilities.Where( a => a.Ability.Name == abilityName ).FirstOrDefault();
            if (ab != null )
            {
                SelectedAbility = ab;
                return true;
            }
            return false;
        }

        public bool SelectMove(int index, Move? move)
        {
            if (index >= 0 && index < PokemonMoveset.MaxMovesetSize)
            {
                SelectedMoves[index] = move;
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

        private List<string> GetCoverageList()
        {
            List<string> ret = new List<string>();
            foreach (string type in Globals.AllTypes)
            {
                if (IsCoveredBySTAB(type))
                    ret.Add(type);
            }

            return ret;
        }

        public override string ToString()
        {
            return StringUtils.FirstCharToUpper(Name);
        }
    }
}
