using Utility;
using System.Text.Json.Serialization;

namespace PokemonDataModel
{
    public class PokemonTeam
    {
        public static readonly int MaxTeamSize = 6;

        [JsonPropertyName("pokemon")]
        public List<SmartPokemon?> Pokemon { get; set; } = [];

        [JsonPropertyName("ruleset")]
        public BoxRules Ruleset { get; set; } = BoxRules.Unrestricted();

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
            Ruleset = team.Ruleset;
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

        // order-independent identity for this team's composition (sorted Pokemon IDs). Two teams
        // with the same composition always produce the same key, regardless of slot order, so this
        // can back a HashSet-based dedupe check instead of comparing every pair with IsSameTeam.
        public string GetCompositionKey()
        {
            return string.Join(",", Pokemon.Where(p => p is not null).Select(p => p!.Id).OrderBy(id => id));
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

                if (p.GetMultipliers(Ruleset.Id).Defense.TryGetValue(typeName, out double value)
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

                if (p.GetMultipliers(Ruleset.Id).Defense.TryGetValue(typeName, out double value)
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

                if (p.IsTypeCoveredBySTAB(typeName, Ruleset.Id))
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

            // seeded with the full type universe, not just Ruleset.EnabledTypes - TeamScorer's
            // helpers (and CalculateBreakdown's ruleset-agnostic "objective score", which always
            // scores against every type regardless of this team's ruleset) index these dictionaries
            // by Globals.AllTypes directly and expect every key to exist. A disabled type still
            // naturally reports zero coverage here, since the derived TypeChart never gives it any
            // multiplier entries in the first place - which types get *scored* is entirely down to
            // which ones a caller's weightings.Types marks as enabled, not which keys exist here.
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

                Multipliers multipliers = p.GetMultipliers(Ruleset.Id);

                foreach (KeyValuePair<string, double> kvp in multipliers.Defense)
                {
                    if (kvp.Value > 1.0 && weaknesses.TryGetValue(kvp.Key, out int wCount))
                        weaknesses[kvp.Key] = wCount + 1;
                    else if (kvp.Value < 1.0 && resistances.TryGetValue(kvp.Key, out int rCount))
                        resistances[kvp.Key] = rCount + 1;
                }

                foreach (KeyValuePair<string, double> kvp in multipliers.Attack)
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
