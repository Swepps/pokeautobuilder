using Utility;
using System.Text.Json.Serialization;

namespace PokemonDataModel
{
    public class PokemonTeam
    {
        public static readonly int MaxTeamSize = 6;

        [JsonPropertyName("pokemon")]
        public List<SmartPokemon?> Pokemon { get; set; } = [];

        [JsonPropertyName("team_name")]
        private string name = "";
        public string Name
        {
            get
            {
                // construct a default name if they haven't set one
                if (string.IsNullOrEmpty(name))
                {
                    string defaultName = "";
                    foreach (SmartPokemon? p in Pokemon)
                    {
                        if (p is not null)
                        {
                            defaultName += StringUtils.PrettifyString(p.Name) + ", ";
                        }
                    }

                    if (defaultName.EndsWith(", "))
                    {
                        defaultName = defaultName.Substring(0, defaultName.Length - 2);
                    }

                    return defaultName;
                }

                return name;
            }

            set { name = value; }
        }

        [JsonIgnore]
        public bool IsEmpty
        {
            get
            {
                foreach (SmartPokemon? p in Pokemon)
                {
                    if (p != null)
                        return false;
                }
                return true;
            }
        }

        public PokemonTeam() 
        {
        }

        // copy constructor
		public PokemonTeam(PokemonTeam team)
		{
			for (int i = 0; i < team.Pokemon.Count; i++)
			{
                Pokemon.Add(team.Pokemon[i]);
			}

            Name = team.Name;
		}

		public bool ContainsDuplicates()
        {
            return Pokemon
                .Where(p => p != null)
                .Distinct()
                .Count() != Pokemon.Count(p => p != null);
        }

        // does this pokemon team share identical pokemon to the other team
        public bool IsSameTeam(PokemonTeam other)
        {
            if (CountPokemon() != other.CountPokemon()) return false;

            foreach (SmartPokemon? p in Pokemon)
            {
                if (!other.Pokemon.Contains(p)) return false;
            }

            return true;
        }

        public int CountMegaPokemon()
        {
            return Pokemon.Count(p => p != null && p.IsMega);
        }

        public int CountGmaxPokemon()
        {
            return Pokemon.Count(p => p != null && p.IsGmax);
        }

        public int CountPokemon()
        {
            int count = 0;
            for (int i = 0; i < Pokemon.Count; i++)
            {
                if (Pokemon[i] != null) count++;
            }
            return count;
        }

        // these functions should probs have been one function but oh well
        public int CountWeaknesses(string typeName)
        {
            int weaknesses = 0;
            for (int i = 0; i < Pokemon.Count; i++)
            {
                SmartPokemon? p = Pokemon[i];
                if (p == null) continue;

                if (p.Multipliers.Defense.TryGetValue(typeName, out double value)
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
            for (int i = 0; i < Pokemon.Count; i++)
            {
                SmartPokemon? p = Pokemon[i];
                if (p == null) continue;

                if (p.Multipliers.Defense.TryGetValue(typeName, out double value)
                    &&
                    value < 1.0)
                {
                    resistances++;
                }
            }

            return resistances;
        }

        public int CountSTABCoverage(string typeName)
        {
            int coverage = 0;
            for (int i = 0; i < Pokemon.Count; i++)
            {
                SmartPokemon? p = Pokemon[i];
                if (p == null) continue;

                if (p.IsTypeCoveredBySTAB(typeName))
                {
                    coverage++;
                }
            }

            return coverage;
        }

		public int CountMoveCoverage(string typeName)
		{
			int coverage = 0;
			for (int i = 0; i < Pokemon.Count; i++)
			{
				SmartPokemon? p = Pokemon[i];
				if (p == null) continue;

				if (p.IsTypeCoveredByMove(typeName))
				{
					coverage++;
				}
			}

			return coverage;
		}

        // Bulk equivalent of calling CountWeaknesses/CountResistances/CountSTABCoverage/
        // CountMoveCoverage once per type: those each re-scan every Pokemon on the team per type
        // (O(types x team size), 4 times over), which is the fitness function's hottest loop since
        // it runs once per chromosome per generation. This scans the team once and reads each
        // Pokemon's own (small) multiplier dictionaries instead, rather than checking every type
        // against every Pokemon.
        public (
            Dictionary<string, int> Weaknesses,
            Dictionary<string, int> Resistances,
            Dictionary<string, int> STABCoverage,
            Dictionary<string, int> MoveCoverage
        ) CountTypeCoverage()
        {
            Dictionary<string, int> weaknesses = [];
            Dictionary<string, int> resistances = [];
            Dictionary<string, int> stabCoverage = [];
            Dictionary<string, int> moveCoverage = [];

            foreach (string type in Globals.AllTypes)
            {
                weaknesses[type] = 0;
                resistances[type] = 0;
                stabCoverage[type] = 0;
                moveCoverage[type] = 0;
            }

            foreach (SmartPokemon? p in Pokemon)
            {
                if (p is null)
                    continue;

                foreach (KeyValuePair<string, double> kvp in p.Multipliers.Defense)
                {
                    if (kvp.Value > 1.0 && weaknesses.TryGetValue(kvp.Key, out int wCount))
                        weaknesses[kvp.Key] = wCount + 1;
                    else if (kvp.Value < 1.0 && resistances.TryGetValue(kvp.Key, out int rCount))
                        resistances[kvp.Key] = rCount + 1;
                }

                foreach (KeyValuePair<string, double> kvp in p.Multipliers.Attack)
                {
                    if (kvp.Value >= 2.0 && stabCoverage.TryGetValue(kvp.Key, out int sCount))
                        stabCoverage[kvp.Key] = sCount + 1;
                }

                foreach (KeyValuePair<string, double> kvp in p.SelectedMoves.AttackMultipliers)
                {
                    if (kvp.Value >= 2.0 && moveCoverage.TryGetValue(kvp.Key, out int mCount))
                        moveCoverage[kvp.Key] = mCount + 1;
                }
            }

            return (weaknesses, resistances, stabCoverage, moveCoverage);
        }

		public void SortById()
        {
            // now empty this team and refill with sorted list
            Pokemon.Sort((a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;

                return a.Id.CompareTo(b.Id);
            });
        }

        public override string ToString()
        {
            string team = "";
            foreach (SmartPokemon? p in Pokemon)
            {
                if (p is null)
                    team += "\"empty\"";
                else
                    team += '\"' + p.ToString() + '\"';
            }
            return team;
        }
    }
}
