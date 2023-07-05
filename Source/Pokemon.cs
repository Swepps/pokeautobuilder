using Accord.MachineLearning;
using Accord.Math.Distances;
using Accord.Math.Random;
using Newtonsoft.Json;
using PokeApiNet;
using pokeAutoBuilder.Source.Services;
using System.Collections.ObjectModel;
using System.Data;

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

        public void Clear()
        {
            Defense.Clear(); Attack.Clear();
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
                    await sp.SelectMove(i, await PokeApiService.Instance!.GetMoveAsync(moveName));
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
        public List<string> STABCoverage { get; init; }
        public List<string> MoveCoverage { get; init; }

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

            // smart variables that make this pokemon class more useful
            SelectedAbility = Abilities[0];
            SelectedMoves = new PokemonMoveset();
			Multipliers = new Multipliers();
			UpdateMultipliers(); // needs to be done before lists can be generated but after ability is selected
			Resistances = GetDefenseResistList();
            Weaknesses = GetDefenseWeakList();
            STABCoverage = GetSTABCoverageList();
            MoveCoverage = GetMoveCoverageList();
        }

        public List<PokemonMove> SearchAvailableMoves(string searchTerm)
        {
            List<PokemonMove> results = Moves.Where(move => move.Move.Name.Contains(searchTerm)).OrderBy(move => move.Move.Name).ToList();
            return results;
        }

        public PokemonMove? GetSelectedMoveResource(int index)
        {
            if (index < 0 || index >= PokemonMoveset.MaxMovesetSize) return null;
            if (SelectedMoves[index] is null) return null;

            Move move = SelectedMoves[index]!;
            return Moves.Find(m => m.Move.Name == move.Name);
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
            PokemonAbility? ab = Abilities.Where( a => a.Ability.Name == abilityName ).FirstOrDefault();
            if (ab != null )
            {
                SelectedAbility = ab;
                UpdateMultipliers();
                return true;
            }
            return false;
        }

        public async Task<bool> SelectMove(int index, Move? move)
        {
            if (index >= 0 && index < PokemonMoveset.MaxMovesetSize)
            {
                await SelectedMoves.SetAt(index, move);
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
                if (Multipliers.Defense.TryGetValue(type, out double value)
                    &&
                    value < 1.0)
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

            // now we've got multipliers for the types we need to check for any abilties which affect them
            switch (SelectedAbility.Ability.Name)
            {
                // Dry Skin makes a pokemon immune to water attacks
                case "dry-skin":
                    Multipliers.Defense["water"] = 0;
                    break;

                // Earth Eater makes a pokemon immune to ground attacks
                case "earth-eater":
                    Multipliers.Defense["ground"] = 0;
                    break;

				// Filter reduces super effective attacks by 25%
				case "filter":
                    foreach (KeyValuePair<string, double> kvp in Multipliers.Defense)
                    {
						if (kvp.Value >= 2.0)
                        {
                            Multipliers.Defense[kvp.Key] = kvp.Value * 0.75;
                        }
					}					
					break;

				// Flash Fire makes a pokemon immune to fire attacks
				case "flash-fire":
					Multipliers.Defense["fire"] = 0;
					break;

				// Fluffy makes a pokemon take double damage from fire attacks
				case "fluffy":
					if (Multipliers.Defense.ContainsKey("fire"))
					{
						Multipliers.Defense["fire"] = Multipliers.Defense["fire"] * 2.0;
					}
					else
					{
						Multipliers.Defense["fire"] = 2.0;
					}
					break;

				// heatproof makes a pokemon take half damage from fire attacks
				case "heatproof":
					if (Multipliers.Defense.ContainsKey("fire"))
					{
						Multipliers.Defense["fire"] = Multipliers.Defense["fire"] * 0.5;
					}
					else
					{
						Multipliers.Defense["fire"] = 0.5;
					}
					break;

				// Levitate makes a pokemon immune to ground attacks
				case "levitate":
					Multipliers.Defense["ground"] = 0;
					break;

				// Lightning rod makes a pokemon immune to electric attacks
				case "lightning-rod":
					Multipliers.Defense["electric"] = 0;
					break;

				// Motor Drive makes a pokemon immune to electric attacks
				case "motor-drive":
					Multipliers.Defense["electric"] = 0;
                    break;

				// Prism Armor reduces super effective attacks by 25%
				case "prism-armor":
					foreach (KeyValuePair<string, double> kvp in Multipliers.Defense)
					{
						if (kvp.Value >= 2.0)
						{
							Multipliers.Defense[kvp.Key] = kvp.Value * 0.75;
						}
					}
					break;

				// purifying sale makes a pokemon take half damage from ghost attacks
				case "purifying-salt":
					if (Multipliers.Defense.ContainsKey("ghost"))
					{
						Multipliers.Defense["ghost"] = Multipliers.Defense["ghost"] * 0.5;
					}
					else
					{
						Multipliers.Defense["ghost"] = 0.5;
					}
					break;

				// Sap Sipper makes a pokemon immune to grass attacks
				case "sap-sipper":
					Multipliers.Defense["grass"] = 0;
					break;


				// Solid Rock reduces super effective attacks by 25%
				case "solid-rock":
					foreach (KeyValuePair<string, double> kvp in Multipliers.Defense)
					{
						if (kvp.Value >= 2.0)
						{
							Multipliers.Defense[kvp.Key] = kvp.Value * 0.75;
						}
					}
					break;

				// Storm drain makes a pokemon immune to water attacks
				case "storm-drain":
					Multipliers.Defense["water"] = 0;
					break;

				// thick fat makes a pokemon take half damage from fire and ice attacks
				case "thick-fat":
					if (Multipliers.Defense.ContainsKey("fire"))
					{
						Multipliers.Defense["fire"] = Multipliers.Defense["fire"] * 0.5;
					}
					else
					{
						Multipliers.Defense["fire"] = 0.5;
					}
					if (Multipliers.Defense.ContainsKey("ice"))
					{
						Multipliers.Defense["ice"] = Multipliers.Defense["ice"] * 0.5;
					}
					else
					{
						Multipliers.Defense["ice"] = 0.5;
					}
					break;

				// Volt absorb makes a pokemon immune to electric attacks
				case "volt-absorb":
					Multipliers.Defense["electric"] = 0;
					break;

				// water absorb makes a pokemon immune to water attacks
				case "water-absorb":
					Multipliers.Defense["water"] = 0;
					break;

				// water bubble makes a pokemon take half damage from fire attacks
				case "water-bubble":
					if (Multipliers.Defense.ContainsKey("fire"))
					{
						Multipliers.Defense["fire"] = Multipliers.Defense["fire"] * 0.5;
					}
					else
					{
						Multipliers.Defense["fire"] = 0.5;
					}
					break;

				// Well-baked body makes a pokemon immune to fire attacks
				case "well-baked-body":
					Multipliers.Defense["fire"] = 0;
					break;

                // wonder guard makes a pokemon immune to all types which aren't super-effective
                case "wonder-guard":
					foreach (string type in Globals.AllTypes)
					{
						if (Multipliers.Defense.ContainsKey(type))
						{
							if (Multipliers.Defense[type] <= 1.0)
                                Multipliers.Defense[type] = 0;
						}
                        else
                        {
							Multipliers.Defense[type] = 0;
						}
					}
					break;
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
            return StringUtils.FirstCharToUpper(Name);
        }
    }
}
