using Accord.Genetic;
using PokeApiNet;

namespace blazorServerApp.Data
{
    using Type = PokeApiNet.Type;

    internal class TeamBuilder
    {
        public static double TypeDefenseWeighting = 1.0;
        public static double TypeCoverageWeighting = 1.0;
        public static Dictionary<string, double> BaseStatWeightings = new Dictionary<string, double>()
        {
            { "hp", 1.0 },
            { "attack", 1.0 },
            { "special-attack", 1.0 },
            { "defense", 1.0 },
            { "special-defense", 1.0 },
            { "speed", 1.0 },
        };

        // The main juice of the team building. this is what decides how good a team is
        public static double CalculateScore(PokemonTeam team)
        {
            if (team.CountPokemon() == 0)
                return 0;

            // gather some information about the types in the team
            List<SmartType> typeArray = Globals.AllTypes;
            Dictionary<Type, int> weaknesses = new Dictionary<Type, int>();
            Dictionary<Type, int> resistances = new Dictionary<Type, int>();
            Dictionary<Type, int> coverage = new Dictionary<Type, int>();

            double totalWeaknesses = 0;
            double totalResistances = 0;
            double totalCoverage = 0;
            foreach (Type t in typeArray)
            {
                totalWeaknesses += weaknesses[t] = team.CountWeaknesses(t.Name);
                totalResistances += resistances[t] = team.CountResistances(t.Name);
                totalCoverage += coverage[t] = team.CountCoverage(t.Name);
            }

            // defense score
            double defenseScore = 0;
            if (TypeDefenseWeighting > 0)
            {
                double weaknessesSD = CalculateStandardDeviation(weaknesses);
                double resistancesSD = CalculateStandardDeviation(resistances);

                defenseScore = 10 - (weaknessesSD + (2 * resistancesSD) + (totalWeaknesses / totalResistances));
                defenseScore *= TypeDefenseWeighting;
            }

            // coverage score
            double coverageScore = 0;
            if (TypeCoverageWeighting > 0)
            {
                coverageScore = CalculateCoverageScore(coverage, totalCoverage);
                coverageScore *= TypeCoverageWeighting;
            }

            double statsScore = CalculateStatsScore(team, BaseStatWeightings);

            double score = defenseScore + coverageScore + statsScore;
            score *= team.CountPokemon() / 6.0;

            return score;
        }

        // Aah GCSE maths... this seems much easier than I thought it was when I was 15
        private static double CalculateStandardDeviation(Dictionary<Type, int> typeDictionary)
        {
            List<SmartType> typeArray = Globals.AllTypes;
            double mean = 0;
            foreach (Type type in typeArray)
            {
                mean += typeDictionary[type];
            }
            mean /= typeArray.Count;

            double variance = 0;
            foreach (Type type in typeArray)
            {
                variance += Math.Pow((typeDictionary[type] - mean), 2);
            }
            variance /= typeArray.Count;

            return Math.Sqrt(variance);
        }

        private static double CalculateCoverageScore(Dictionary<Type, int> typeDictionary, double totalCoverage)
        {
            List<SmartType> typeArray = Globals.AllTypes;
            double score = 5.0 + Math.Sqrt(totalCoverage);
            double scoreForType = score / typeArray.Count;

            // give up to 10 points for having at least 1 coverage of each type
            foreach (Type type in typeArray)
            {
                if (!typeDictionary.ContainsKey(type))
                    score -= scoreForType;
            }

            // deduct points for having a spread of coverage with high variance
            score -= CalculateStandardDeviation(typeDictionary);

            return score;
        }

        private static double CalculateStatsScore(PokemonTeam team, Dictionary<string, double> statWeightings)
        {
            double score = 0;

            Dictionary<string, int> statTotals = new Dictionary<string, int>();

            foreach (SmartPokemon? pokemon in team)
            {
                if (pokemon == null)
                    continue;

                foreach (PokemonStat stat in pokemon.Stats)
                {
                    if (statTotals.ContainsKey(stat.Stat.Name))
                        statTotals[stat.Stat.Name] += stat.BaseStat;
                    else
                        statTotals[stat.Stat.Name] = stat.BaseStat;
                }
            }

            foreach (KeyValuePair<string, int> kvp in statTotals)
            {
                score += (kvp.Value / 300.0) * statWeightings[kvp.Key];
            }

            // --- commented this out because it didn't work very well and was unnecessary computation time
            // try to balance the att and def values according to their weightings
            //double attDiffScore = 0;
            //double defDiffScore = 0;
            
            //if (statWeightings["attack"] > 0 && statWeightings["special-attack"] > 0)
            //    attDiffScore = Math.Abs((statTotals["attack"] / statWeightings["attack"]) - (statTotals["special-attack"] / statWeightings["special-attack"]));

            //if (statWeightings["defense"] > 0 && statWeightings["special-defense"] > 0)
            //    defDiffScore = Math.Abs((statTotals["defense"] / statWeightings["defense"]) - (statTotals["special-defense"] / statWeightings["special-defense"]));

            //attDiffScore /= 100;
            //attDiffScore = Math.Sqrt(attDiffScore);

            //defDiffScore /= 100;
            //defDiffScore = Math.Sqrt(defDiffScore);

            //score -= attDiffScore + defDiffScore;

            return score;
        }



        public static PokemonTeam BuildTeam(PokemonStorage availablePokemon, PokemonTeam? lockedMembers)
        {
            if (lockedMembers == null)
            {
                lockedMembers = new PokemonTeam();
            }
            else if (lockedMembers.CountPokemon() >= 6 || availablePokemon.Count < 6)
            {
                // they're all locked!
                return lockedMembers;
            }

            PokemonTeam bestTeam = new PokemonTeam();
            double bestTeamScore = 0;

            // using the clever combinations code to quickly iterate through each combination of teams
            // we are selecting (6 - lockedMembers.Count) from all the pokemon in the box
            foreach (SmartPokemon[] remainingTeamCombination in Combinations<SmartPokemon>(availablePokemon.ToArray(), 6 - lockedMembers.CountPokemon()))
            {
                // check that we're not already using one of these selected pokemon in our team already
                // this is inefficient but the lists will always be small so should be pretty quick
                foreach (SmartPokemon entry in remainingTeamCombination)
                {
                    foreach (SmartPokemon? pokemon in lockedMembers)
                    {
                        if (entry == pokemon)
                            continue;
                    }
                }

                // create a new team using lockedMembers and chosen combination
                PokemonTeam newTeam = new PokemonTeam();
                int combinIdx = 0;
                for (int i = 0; i < 6; i++)
                {
                    if (lockedMembers[i] != null)
                    {
                        newTeam[i] = lockedMembers[i];
                    }
                    else if (combinIdx < remainingTeamCombination.Length)
                    {
                        newTeam[i] = remainingTeamCombination[combinIdx];
                        combinIdx++;
                    }
                }

                // check to see if new team is better
                double newTeamScore = CalculateScore(newTeam);
                if (newTeamScore > bestTeamScore)
                {
                    bestTeam = newTeam;
                    bestTeamScore = newTeamScore;
                }
            }

            return bestTeam;
        }

        // clever piece of combinations code I stole from the internet!

        // Enumerate all possible m-size combinations of [0, 1, ..., n-1] array
        // in lexicographic order (first [0, 1, 2, ..., m-1]).
        private static IEnumerable<int[]> Combinations(int m, int n)
        {
            int[] result = new int[m];
            Stack<int> stack = new Stack<int>(m);
            stack.Push(0);
            while (stack.Count > 0)
            {
                int index = stack.Count - 1;
                int value = stack.Pop();
                while (value < n)
                {
                    result[index++] = value++;
                    stack.Push(value);
                    if (index != m) continue;
                    yield return (int[])result.Clone(); // thanks to @xanatos
                                                        //yield return result;
                    break;
                }
            }
        }

        public static IEnumerable<T[]> Combinations<T>(T[] array, int m)
        {
            if (array.Length < m)
                throw new ArgumentException("Array length can't be less than number of selected elements");
            if (m < 1)
                throw new ArgumentException("Number of selected elements can't be less than 1");
            T[] result = new T[m];
            foreach (int[] j in Combinations(m, array.Length))
            {
                for (int i = 0; i < m; i++)
                {
                    result[i] = array[j[i]];
                }
                yield return result;
            }
        }



        public static PokemonTeam BuildTeamGenetic(PokemonStorage availablePokemon, PokemonTeam? lockedMembers)
        {
            if (lockedMembers == null)
            {
                lockedMembers = new PokemonTeam();
            }
            else if (lockedMembers.CountPokemon() >= 6 || availablePokemon.Count < 6)
            {
                // they're all locked!
                return lockedMembers;
            }

            Population population = new Population(500, new PokemonTeamChromosome(availablePokemon, lockedMembers), new PokemonTeamChromosome.FitnessFunction(), new EliteSelection());

            for (int i = 0; i < 100; i++)
            {
                population.RunEpoch();
                //Trace.WriteLine(population.FitnessMax);
            }

            return ((PokemonTeamChromosome)population.BestChromosome).Team;
        }
    }

    internal class PokemonTeamChromosome : ChromosomeBase
    {
        static Random Random = new Random();

        private readonly PokemonStorage _storage;
        private readonly PokemonTeam _lockedMembers;

        public PokemonTeam Team;

        public PokemonTeamChromosome(PokemonStorage storage, PokemonTeam lockedMembers)
        {
            _storage = storage;
            _lockedMembers = lockedMembers;
            Team = new PokemonTeam();
            Generate();
        }

        public PokemonTeamChromosome(PokemonTeam team, PokemonStorage storage, PokemonTeam lockedMembers)
        {
            _storage = storage;
            _lockedMembers = lockedMembers;
            Team = new PokemonTeam();
            for (int i = 0; i < 6; i++)
            {
                Team[i] = team[i];
            }
        }

        public override IChromosome Clone()
        {
            return new PokemonTeamChromosome(Team, _storage, _lockedMembers);
        }

        public override IChromosome CreateNew()
        {
            PokemonTeamChromosome chrom = new PokemonTeamChromosome(_storage, _lockedMembers);
            chrom.Generate();
            return chrom;
        }

        public override void Crossover(IChromosome pair)
        {
            PokemonTeamChromosome? otherChrom = pair as PokemonTeamChromosome;
            if (otherChrom == null)
                return;

            int randIdx = Random.Next(6);
            for (; randIdx < 6; randIdx++)
            {
                if (!Team.Contains(otherChrom.Team[randIdx]))
                    Team[randIdx] = otherChrom.Team[randIdx];
            }
        }

        public override void Generate()
        {
            Team = _storage.GetRandomTeam(_lockedMembers);
        }

        public override void Mutate()
        {
            // can't mutate if the data set is less than team size
            if (_storage.Count <= 6 || _lockedMembers.CountPokemon() >= 6)
                return;

            // make sure we don't swap out any locked members
            int randIdx = Random.Next(6);
            while (_lockedMembers[randIdx] != null)
                randIdx = Random.Next(6);

            // now mutate
            SmartPokemon randPokemon = _storage.GetRandomPokemon();
            while (Team.Contains(randPokemon))
            {
                randPokemon = _storage.GetRandomPokemon();
            }
            Team[randIdx] = randPokemon;
        }

        public class FitnessFunction : IFitnessFunction
        {
            public double Evaluate(IChromosome chromosome)
            {
                PokemonTeamChromosome? pokeChrom = chromosome as PokemonTeamChromosome;
                if (pokeChrom == null)
                    return 0;

                return TeamBuilder.CalculateScore(pokeChrom.Team);
            }
        }
    }
}
