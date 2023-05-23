﻿using PokeApiNet;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace autoteambuilder
{
    using Type = PokeApiNet.Type;

    public struct Multipliers
    {
        public Dictionary<string, double> defense;
        public Dictionary<string, bool> coverage;

        public Multipliers()
        {
            defense = new Dictionary<string, double>();
            coverage = new Dictionary<string, bool>();
        }
    }

    public class SmartPokemon : Pokemon
    {
        public Multipliers? Multipliers;

        public ObservableCollection<string> Resistances { get; set; }
        public ObservableCollection<string> Weaknesses { get; set; }
        public ObservableCollection<string> Coverage { get; set; }

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

            Resistances = GetDefenseResistList();
            Weaknesses = GetDefenseWeakList();
            Coverage = GetCoverageList();
        }

        public double GetEffectivenessTo(string typeName)
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

        public bool IsCoveredBySTAB(string typeName)
        {
            Multipliers multipliers = GetMultipliers();
            if (multipliers.coverage.TryGetValue(typeName, out bool covered))
            {
                return covered;
            }
            else
            {
                return false;
            }
        }

        public Multipliers GetMultipliers()
        {
            Multipliers ??= PokeApiHandler.GetPokemonMultipliersAsync(this).Result;

            return (Multipliers)Multipliers;
        }

        private ObservableCollection<string> GetDefenseResistList()
        {
            ObservableCollection<string> ret = new ObservableCollection<string>();
            foreach (Type type in MainWindow.AllTypes)
            {
                if (GetEffectivenessTo(type.Name) < 1.0)
                    ret.Add(type.Name);
            }

            return ret;
        }

        private ObservableCollection<string> GetDefenseWeakList()
        {
            ObservableCollection<string> ret = new ObservableCollection<string>();
            foreach (Type type in MainWindow.AllTypes)
            {
                if (GetEffectivenessTo(type.Name) > 1.0)
                    ret.Add(type.Name);
            }

            return ret;
        }

        private ObservableCollection<string> GetCoverageList()
        {
            ObservableCollection<string> ret = new ObservableCollection<string>();
            foreach (Type type in MainWindow.AllTypes)
            {
                if (IsCoveredBySTAB(type.Name))
                    ret.Add(type.Name);
            }

            return ret;
        }

        public override string ToString()
        {
            return LowercaseToNamecaseConverter.FirstCharToUpper(Name);
        }
    }
}
