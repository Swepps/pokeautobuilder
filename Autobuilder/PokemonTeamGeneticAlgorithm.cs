using GeneticSharp;
using PokemonDataModel;
using System.Linq;

namespace Autobuilder
{
    public class PokemonTeamGeneticAlgorithm
    {
        GeneticAlgorithm? _ga;
        CancellationTokenSource? _cts;
        public event Action<PokemonTeamGeneticAlgorithm>? GenerationRan;
        public PokemonTeamFitness? Fitness { get; private set; }
        public PokemonTeamChromosome? BestChromosome => _ga != null ? _ga.BestChromosome as PokemonTeamChromosome : null;

        // the full, fitness-evaluated population for the generation that just finished - lets
        // callers compute population-wide metrics (average fitness, distinct compositions seen)
        // instead of only ever seeing the single best chromosome
        public IReadOnlyList<PokemonTeamChromosome> CurrentPopulation =>
            _ga != null
                ? _ga.Population.CurrentGeneration.Chromosomes.OfType<PokemonTeamChromosome>().ToList()
                : Array.Empty<PokemonTeamChromosome>();

        public int GenerationsNumber => _ga != null ? _ga.GenerationsNumber : 0;
        public bool IsRunning => _cts != null;

        public void Initialize(int populationsize, PokemonBox box, PokemonTeam lockedMembers, AutobuilderWeightings weightings)
        {
            Stop();
            Fitness = new PokemonTeamFitness(weightings);
            var chromosome = new PokemonTeamChromosome(box, lockedMembers);

            // Domain-specific operators (see each class for the full rationale):
            // - PokemonTeamCrossover/PokemonTeamMutation treat a team as a *set* of box members
            //   and never produce duplicate-carrying teams, which the fitness function scores 0.
            //   The mutation is also what injects box members absent from the initial population.
            // - TournamentSelection picks by rank, not raw fitness. All viable teams score in a
            //   narrow band (roughly 13-15), so fitness-proportionate selection (roulette wheel)
            //   gives the best team barely more reproduction chance than a mediocre one and the
            //   search degenerates into random drift.
            // - GenerationElitistReinsertion guarantees the best teams found so far stay in the
            //   breeding population every generation.
            var crossover = new PokemonTeamCrossover(box);
            var mutation = new PokemonTeamMutation(box);
            var selection = new TournamentSelection(2);
            var reinsertion = new GenerationElitistReinsertion();
            var population = new Population(populationsize, populationsize, chromosome);

            _ga = new GeneticAlgorithm(population, Fitness, selection, crossover, mutation)
            {
                Reinsertion = reinsertion,
                MutationProbability = 0.2f,
            };
        }

        public void RunInBackground()
        {
            if (IsRunning)
                return;

            _cts = new CancellationTokenSource();

            // WebAssembly has no real background thread to hand this off to (and even the newer
            // WasmEnableThreads support needs cross-origin isolation headers our GitHub Pages host
            // can't set), so this just runs one generation at a time on the UI thread, yielding
            // back to the browser's event loop between each so it stays responsive.
            _ = RunLoopAsync(_cts.Token);
        }

        async Task RunLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                RunOneGeneration();
                await Task.Delay(1);
            }
        }

        // run it synchronously for a number of iterations
        public void Run(int numGenerations)
        {
            for (int i = 0; i < numGenerations; i++)
            {
                if (_ga is null)
                    return;

                RunOneGeneration();
            }
        }

        void RunOneGeneration()
        {
            if (_ga is null)
                return;

            _ga.Termination = new GenerationNumberTermination(_ga.GenerationsNumber + 1);
            if (_ga.GenerationsNumber > 0)
                _ga.Resume();
            else
                _ga.Start();
            GenerationRan?.Invoke(this);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
