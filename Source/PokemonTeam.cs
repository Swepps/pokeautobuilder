using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace pokeAutoBuilder.Source
{
    public class PokemonTeamSerializable
    {
        public string Name { get; init; }
        public List<string> Team { get; init; }

        [JsonConstructor]
        public PokemonTeamSerializable(string name, List<string> team)
        {
            Name = name;
            Team = team;
        }

        public PokemonTeamSerializable(PokemonTeam team)
        {
            Name = team.Name;
            Team = new List<string>();
            foreach (SmartPokemon? p in team)
            {
                if (p is null)
                    Team.Add("");
                else
                    Team.Add(p.Name);
            }
        }

        public PokemonTeamSerializable()
        {
            Name = "";
            Team = new List<string>();
            for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
            {
                Team.Add("");
            }
        }

        public async Task<PokemonTeam> GetPokemonTeam()
        {
            PokemonTeam team = new PokemonTeam();
            var getTeamTasks = Team.Select(async (name, index) =>
            {
                if (!string.IsNullOrEmpty(name))
                {
                    SmartPokemon? sp = await PokeApiHandler.GetPokemonAsync(name);
                    if (sp is not null)
                        team[index] = sp;
                }
            });
            await Task.WhenAll(getTeamTasks);

            team.Name = Name;

            return team;
        }

		public override bool Equals(object? obj)
		{
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
             
            PokemonTeamSerializable? objTeam = obj as PokemonTeamSerializable;
            if (objTeam == null) return false;

            if (objTeam.Name != Name) return false;

            // close enough of an equals operation
            foreach (string pokemon in objTeam.Team)
            {
                if (!Team.Contains(pokemon)) return false;
            }

			return true;
		}

		public override int GetHashCode()
		{
            int code = Name.GetHashCode();
            foreach (string pokemon in Team)
            {
                code ^= pokemon.GetHashCode();
            }

            return code;
		}
	}

    public class PokemonTeam : List<SmartPokemon?>
    {
        public static readonly int MaxTeamSize = 6;

        private string name = "";
        public string Name
        {
            get
            {
                // construct a default name if they haven't set one
                if (string.IsNullOrEmpty(name))
                {
                    string defaultName = "";
                    foreach (SmartPokemon? p in this)
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
                foreach (SmartPokemon? p in this)
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
                SmartPokemon? p = this[i];
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

        public int CountCoverage(string typeName)
        {
            int coverage = 0;
            for (int i = 0; i < MaxTeamSize; i++)
            {
                SmartPokemon? p = this[i];
                if (p == null) continue;

                if (p.Multipliers.Coverage.TryGetValue(typeName, out bool value)
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
