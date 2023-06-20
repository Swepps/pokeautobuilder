using Accord.MachineLearning;
using Accord.Math.Random;
using Newtonsoft.Json;
using PokeApiNet;
using System.Collections.ObjectModel;

namespace blazorWebAssemblyApp.Source
{
    using Type = PokeApiNet.Type;

    public struct Multipliers
    {
        public Dictionary<string, double> Defense;
        public Dictionary<string, bool> Coverage;

        public Multipliers()
        {
            Defense = new Dictionary<string, double>();
            Coverage = new Dictionary<string, bool>();
        }
    }

    public class SmartPokemon : Pokemon
    {
        public PokemonAbility? ChosenAbility { get; set; }
        public PokemonSpecies LoadedSpecies { get; set; }
        public Generation Generation { get; set; }
        public Multipliers Multipliers { get; init; }
        public List<string> Resistances { get; init; }
        public List<string> Weaknesses { get; init; }
        public List<string> Coverage { get; init; }

        public static async Task<SmartPokemon> BuildSmartPokemonAsync(Pokemon basePokemon)
        {
            Multipliers multipliers = await PokeApiHandler.GetPokemonMultipliersAsync(basePokemon);
            PokemonSpecies? species = await PokeApiHandler.GetPokemonSpeciesAsync(basePokemon.Species.Name);
            if (species is null)
                throw new Exception("Could not load species information from " + basePokemon.Name);
            Generation? generation = await PokeApiHandler.GetGenerationAsync(species);
            if (generation is null)
                throw new Exception("Could not load generation information from " + species.Name);

            return new SmartPokemon(basePokemon, species, generation, multipliers);
        }


        public SmartPokemon(Pokemon pokemon, PokemonSpecies loadedSpecies, Generation generation, Multipliers multipliers)
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
            Generation = generation;
            Multipliers = multipliers;

            // smart variables that make this pokemon class more useful
            ChosenAbility = Abilities[0];
            Resistances = GetDefenseResistList();
            Weaknesses = GetDefenseWeakList();
            Coverage = GetCoverageList();
        }

        public double GetEffectivenessTo(string typeName)
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
            if (Multipliers.Coverage.TryGetValue(typeName, out bool covered))
            {
                return covered;
            }
            else
            {
                return false;
            }
        }

        public bool SetChosenAbility(string abilityName)
        {
            PokemonAbility? ab = Abilities.Where( a => a.Ability.Name == abilityName ).FirstOrDefault();
            if (ab != null )
            {
                ChosenAbility = ab;
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
                double eff = GetEffectivenessTo(type);

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
                if (GetEffectivenessTo(type) > 1.0)
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
