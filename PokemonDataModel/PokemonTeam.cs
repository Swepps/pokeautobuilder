using Utility;
using System.Text.Json.Serialization;

namespace PokemonDataModel
{
    public class PokemonTeam
    {
        public static readonly int MaxTeamSize = 6;

        [JsonPropertyName("pokemon")]
        public List<SmartPokemon?> Pokemon { get; set; } = new();

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
                            defaultName += StringUtils.FirstCharToUpper(p.Name) + ", ";
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
            // fill team with 6 null pokemon
            for (int i = 0; i < MaxTeamSize; i++)
            {
                Pokemon.Add(null);
            }
        }

        public bool ContainsDuplicates()
        {
            return Pokemon.Distinct().Count() != Pokemon.Count;
        }

        public int CountPokemon()
        {
            int count = 0;
            for (int i = 0; i < MaxTeamSize; i++)
            {
                SmartPokemon? p = Pokemon[i];
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
            for (int i = 0; i < MaxTeamSize; i++)
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
            for (int i = 0; i < MaxTeamSize; i++)
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
			for (int i = 0; i < MaxTeamSize; i++)
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

		public void SortById()
        {
            // order any non null members into a new list
            List<SmartPokemon?> team = Pokemon.Where(p => p is not null).OrderBy(p => p!.Id).ToList();

            // now empty this team and refill with sorted list
            Pokemon.Clear();
            for (int i = 0; i < MaxTeamSize; ++i)
            {
                if (i < team.Count)
                    Pokemon.Add(team[i]);
                else
                    Pokemon.Add(null);
            }
        }
    }
}
