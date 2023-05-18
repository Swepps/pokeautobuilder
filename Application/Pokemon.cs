using PokeApiNet;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace autoteambuilder
{
    using Type = PokeApiNet.Type;

    public struct Multipliers
    {
        public Dictionary<string, double> defense;
        public Dictionary<string, double> attack;

        public Multipliers()
        {
            defense = new Dictionary<string, double>();
            attack = new Dictionary<string, double>();
        }
    }

    public class SmartPokemon : Pokemon
    {
        public Multipliers? Multipliers;

        public SmartPokemon(Pokemon pokemon)
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
        }

        public double GetAttackEffectiveness(string typeName)
        {
            Multipliers multipliers = GetMultipliers();
            if (multipliers.defense.TryGetValue(typeName, out double attEff))
            {
                return attEff;
            }
            else
            {
                return 1.0;
            }
        }

        public double GetDefenseffectiveness(string typeName)
        {
            Multipliers multipliers = GetMultipliers();
            if (multipliers.attack.TryGetValue(typeName, out double attEff))
            {
                return attEff;
            }
            else
            {
                return 1.0;
            }
        }

        public Multipliers GetMultipliers()
        {
            Multipliers ??= PokeApiHandler.GetPokemonMultipliersAsync(this).Result;

            return (Multipliers)Multipliers;
        }

        public override string ToString()
        {
            string name = Name;
            //if (Variant != "")
            //    name += " - " + Variant;

            return name;
        }
    }
}
