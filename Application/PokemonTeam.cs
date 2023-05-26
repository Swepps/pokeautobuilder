using autoteambuilder;
using PokeApiNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace autoteambuilder
{
    using Type = PokeApiNet.Type;

    public class PokemonTeam : ObservableCollection<SmartPokemon?>
    {
        public static readonly int MaxTeamSize = 6;

        public bool IsEmpty
        {
            get
            {
                foreach (SmartPokemon? p in this)
                {
                    if (p != null)
                        return true;
                }
                return false;
            }
            private set => IsEmpty = value;
        }

        public PokemonTeam() 
        {
            // need 6 null pokemon in the team at the beginning
            for (int i = 0; i < MaxTeamSize; i++)
            {
                Add(null);
            }
        }
        public int CountPokemon()
        {
            int count = 0;
            for (int i = 0; i < MaxTeamSize; i++)
            {
                SmartPokemon? p = this[i];
                if (p != null) count++;
            }
            return count;
        }

        // these functions should probs have been one function but oh well
        public int CountWeaknesses(string typeName)
        {
            int weaknesses = 0;
            for (int i = 0; i < MaxTeamSize; i++)
            {
                SmartPokemon? p = this[i];
                if (p == null) continue;

                Multipliers multipliers = p.GetMultipliers();

                if (multipliers.defense.TryGetValue(typeName, out double value)
                    &&
                    value > 1.0)
                {
                    weaknesses++;
                }
            }

            return weaknesses;
        }

        public int CountResistances(string typeName)
        {
            int resistances = 0;
            for (int i = 0; i < MaxTeamSize; i++)
            {
                SmartPokemon? p = this[i];
                if (p == null) continue;

                Multipliers multipliers = p.GetMultipliers();

                if (multipliers.defense.TryGetValue(typeName, out double value)
                    &&
                    value < 1.0)
                {
                    resistances++;
                }
            }

            return resistances;
        }

        public int CountCoverage(string typeName)
        {
            int coverage = 0;
            for (int i = 0; i < MaxTeamSize; i++)
            {
                SmartPokemon? p = this[i];
                if (p == null) continue;

                Multipliers multipliers = p.GetMultipliers();

                if (multipliers.coverage.TryGetValue(typeName, out bool value)
                    &&
                    value)
                {
                    coverage++;
                }
            }

            return coverage;
        }
    }
}
