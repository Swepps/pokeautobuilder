using System.Diagnostics;
using Autobuilder;
using PokemonDataModel;
using Xunit;
using Xunit.Abstractions;

namespace PokeAutobuilderTests
{
    // Throwaway benchmark for the PokemonTeam type-coverage counting hot path in
    // TeamScorer.CalculateScore (the genetic algorithm's fitness function, called once per
    // chromosome per generation). Not a correctness test - run with:
    //   dotnet test --filter "FullyQualifiedName~CalculateScoreBenchmark"
    public class CalculateScoreBenchmark
    {
        private readonly ITestOutputHelper _output;

        public CalculateScoreBenchmark(ITestOutputHelper output)
        {
            _output = output;
        }

        private static PokemonTeam MakeRealisticTeam()
        {
            // Roughly what a real dual-type Pokemon's multipliers look like after
            // UpdateMultipliers - most of the 18 types present with a mix of values.
            Dictionary<string, double> defenseTemplate = new()
            {
                ["normal"] = 1.0,
                ["fire"] = 0.5,
                ["water"] = 2.0,
                ["electric"] = 0.5,
                ["grass"] = 0.25,
                ["ice"] = 2.0,
                ["fighting"] = 1.0,
                ["poison"] = 0.5,
                ["ground"] = 0.0,
                ["flying"] = 2.0,
                ["psychic"] = 1.0,
                ["bug"] = 0.5,
                ["rock"] = 1.0,
                ["ghost"] = 1.0,
                ["dragon"] = 1.0,
                ["dark"] = 1.0,
                ["steel"] = 0.5,
                ["fairy"] = 1.0,
            };

            PokemonTeam team = new();
            string[] stabTypes =
            [
                "fire",
                "water",
                "electric",
                "grass",
                "psychic",
                "dragon",
            ];
            for (int i = 0; i < 6; i++)
            {
                SmartPokemon mon = TestFixtures.MakeScoringPokemon(
                    $"benchmon-{i}",
                    defense: defenseTemplate,
                    attack: new Dictionary<string, double> { [stabTypes[i]] = 2.0 },
                    moveCoverage: [stabTypes[i], stabTypes[(i + 1) % stabTypes.Length]],
                    baseStats: new Dictionary<string, int>
                    {
                        ["hp"] = 80,
                        ["attack"] = 90,
                        ["defense"] = 80,
                        ["special-attack"] = 90,
                        ["special-defense"] = 80,
                        ["speed"] = 100,
                    }
                );
                team.Pokemon.Add(mon);
            }

            return team;
        }

        [Fact]
        public void BenchmarkCalculateScore()
        {
            PokemonTeam team = MakeRealisticTeam();
            AutobuilderWeightings weightings = new();

            const int warmupIterations = 1_000;
            const int measuredIterations = 100_000;

            for (int i = 0; i < warmupIterations; i++)
            {
                TeamScorer.CalculateScore(team, weightings);
            }

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < measuredIterations; i++)
            {
                TeamScorer.CalculateScore(team, weightings);
            }
            sw.Stop();

            double perCallMicroseconds = sw.Elapsed.TotalMilliseconds * 1000.0 / measuredIterations;
            _output.WriteLine(
                $"{measuredIterations} calls in {sw.ElapsedMilliseconds}ms "
                    + $"({perCallMicroseconds:F2}us/call, {measuredIterations / sw.Elapsed.TotalSeconds:F0} calls/sec)"
            );
        }
    }
}
