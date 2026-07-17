using System.Diagnostics;
using System.Text.Json;
using Autobuilder;
using PokemonDataModel;
using Xunit;
using Xunit.Abstractions;

namespace PokeAutobuilderTests
{
    // Measures how reliably the genetic algorithm finds the best-scoring team on a realistic
    // box (Fixtures/luna-box.json, 48 pokemon). With 48C6 = ~12.3M possible teams the true
    // optimum can be brute-forced, giving an exact yardstick: run the GA many times at the UI's
    // default settings and count how often it actually reaches the optimum.
    //
    // These are diagnostics, not regression tests - run explicitly with:
    //   dotnet test --filter "FullyQualifiedName~GAReliabilityBenchmark"
    public class GAReliabilityBenchmark : IAsyncLifetime
    {
        private readonly TypeChart typeChart = new();
        private readonly ITestOutputHelper output;
        private PokemonBox box = new();

        public GAReliabilityBenchmark(ITestOutputHelper output)
        {
            this.output = output;
        }

        public Task DisposeAsync() => Task.CompletedTask;

        public async Task InitializeAsync()
        {
            PokeApiService apiService = new(new HttpClient(), typeChart);
            await TestFixtures.EnsureRealTypesLoadedAsync(apiService, typeChart);

            string fixturePath = Path.Combine(
                AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "luna-box.json");
            string json = await File.ReadAllTextAsync(fixturePath);

            JsonSerializerOptions options = new();
            options.Converters.Add(new SmartPokemonJsonConverter(typeChart));
            box = JsonSerializer.Deserialize<PokemonBox>(json, options)!;
            Assert.NotEmpty(box.Pokemon);
        }

        // Mirrors PokemonTeamFitness.Evaluate for a known-duplicate-free team.
        private static double ScoreTeam(PokemonTeam team, AutobuilderWeightings weightings)
        {
            double fitness = TeamScorer.CalculateScore(team, weightings).SumWeightings();

            if (fitness < 0
                || (!weightings.AllowMultipleMegas && team.CountMegaPokemon() > 1)
                || (!weightings.AllowMultipleGmax && team.CountGmaxPokemon() > 1))
                return 0;

            return fitness;
        }

        [Fact]
        public void BruteForceOptimum()
        {
            AutobuilderWeightings weightings = new();
            SmartPokemon[] all = box.Pokemon.ToArray();
            int n = all.Length;
            output.WriteLine($"box size: {n}, total teams: {Combinations(n, 6):N0}");

            Stopwatch sw = Stopwatch.StartNew();

            object gate = new();
            double globalBest = double.MinValue;
            int[]? globalBestCombo = null;
            List<(double score, int[] combo)> top = new();

            Parallel.For(0, n - 5, i =>
            {
                PokemonTeam team = new();
                for (int s = 0; s < 6; s++) team.Pokemon.Add(null);
                double localBest = double.MinValue;
                int[]? localCombo = null;
                List<(double, int[])> localTop = new();

                for (int j = i + 1; j < n - 4; j++)
                    for (int k = j + 1; k < n - 3; k++)
                        for (int l = k + 1; l < n - 2; l++)
                            for (int m = l + 1; m < n - 1; m++)
                                for (int o = m + 1; o < n; o++)
                                {
                                    team.Pokemon[0] = all[i];
                                    team.Pokemon[1] = all[j];
                                    team.Pokemon[2] = all[k];
                                    team.Pokemon[3] = all[l];
                                    team.Pokemon[4] = all[m];
                                    team.Pokemon[5] = all[o];

                                    double score = ScoreTeam(team, weightings);
                                    if (score > localBest)
                                    {
                                        localBest = score;
                                        localCombo = new[] { i, j, k, l, m, o };
                                    }
                                    localTop.Add((score, new[] { i, j, k, l, m, o }));
                                    if (localTop.Count > 2000)
                                    {
                                        localTop = localTop.OrderByDescending(t => t.Item1).Take(10).ToList();
                                    }
                                }

                localTop = localTop.OrderByDescending(t => t.Item1).Take(10).ToList();
                lock (gate)
                {
                    if (localBest > globalBest)
                    {
                        globalBest = localBest;
                        globalBestCombo = localCombo;
                    }
                    top.AddRange(localTop);
                }
            });

            sw.Stop();
            output.WriteLine($"brute force took {sw.Elapsed.TotalSeconds:0.0}s");
            output.WriteLine($"optimum fitness: {globalBest:0.000000}");
            output.WriteLine($"optimum team: {string.Join(", ", globalBestCombo!.Select(x => all[x].Name))}");

            output.WriteLine("top 10 teams:");
            foreach (var (score, combo) in top.OrderByDescending(t => t.score).Take(10))
            {
                output.WriteLine($"  {score:0.000000}: {string.Join(", ", combo.Select(x => all[x].Name))}");
            }
        }

        [Fact]
        public void MeasureGAReliability()
        {
            const int Runs = 20;
            const int PopulationSize = 250;   // UI default
            const int Generations = 50;       // UI default

            AutobuilderWeightings weightings = new();

            List<double> results = new();
            List<string> resultTeams = new();

            for (int run = 0; run < Runs; run++)
            {
                PokemonTeamGeneticAlgorithm ga = new();

                double bestFitness = 0;
                PokemonTeam bestTeam = new();
                ga.GenerationRan += g =>
                {
                    if (g.BestChromosome?.Fitness is double f && f > bestFitness)
                    {
                        bestFitness = f;
                        bestTeam = g.BestChromosome.GetTeam();
                    }
                };

                PokemonTeam lockedMembers = new();
                for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
                    lockedMembers.Pokemon.Add(null);

                ga.Initialize(PopulationSize, box, lockedMembers, weightings);
                ga.Run(Generations);

                results.Add(bestFitness);
                string teamStr = string.Join(", ",
                    bestTeam.Pokemon.Where(p => p != null).Select(p => p!.Name).OrderBy(s => s));
                resultTeams.Add(teamStr);
                output.WriteLine($"run {run + 1,2}: best={bestFitness:0.000000}  [{teamStr}]");
            }

            output.WriteLine("");
            output.WriteLine($"mean:   {results.Average():0.000000}");
            output.WriteLine($"min:    {results.Min():0.000000}");
            output.WriteLine($"max:    {results.Max():0.000000}");
            output.WriteLine($"stddev: {StdDev(results):0.000000}");
            output.WriteLine($"distinct final teams: {resultTeams.Distinct().Count()} / {Runs}");
        }

        private static double StdDev(List<double> values)
        {
            double mean = values.Average();
            return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / values.Count);
        }

        private static long Combinations(int n, int k)
        {
            long result = 1;
            for (int i = 0; i < k; i++) result = result * (n - i) / (i + 1);
            return result;
        }
    }
}
