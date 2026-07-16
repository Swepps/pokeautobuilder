using GeneticSharp;
using PokemonDataModel;

namespace Autobuilder
{
    public class PokemonTeamGeneticAlgorithm
    {
        GeneticAlgorithm? _ga;
        CancellationTokenSource? _cts;
        public event Action<PokemonTeamGeneticAlgorithm>? GenerationRan;
        public PokemonTeamFitness? Fitness { get; private set; }
        public PokemonTeamChromosome? BestChromosome => _ga != null ? _ga.BestChromosome as PokemonTeamChromosome : null;
        public int GenerationsNumber => _ga != null ? _ga.GenerationsNumber : 0;
        public bool IsRunning => _cts != null;

        public void Initialize(int populationsize, PokemonBox box, PokemonTeam lockedMembers, AutobuilderWeightings weightings)
        {
            Stop();
            Fitness = new PokemonTeamFitness(weightings);
            var chromosome = new PokemonTeamChromosome(box, lockedMembers);

            // OrderedCrossover and ReverseSequenceMutation are classic TSP operators built for
            // permutation encodings (every gene value appears exactly once, order matters).
            // A team isn't a permutation - genes are drawn independently from a much larger box
            // and team order doesn't affect score - so those operators don't fit:
            // ReverseSequenceMutation only reorders positions, which never changes the resulting
            // team, so it could never introduce a Pokemon that wasn't in the initial population.
            // UniformCrossover recombines genes per-slot without assuming a permutation, and
            // UniformMutation(allGenesMutable: true) uses PokemonTeamChromosome.GenerateGene to
            // swap in a fresh random Pokemon per slot, which is what actually injects new genetic
            // material generation over generation.
            var crossover = new UniformCrossover();
            var mutation = new UniformMutation(true);
            var selection = new RouletteWheelSelection();
            var population = new Population(populationsize, populationsize, chromosome);

            _ga = new GeneticAlgorithm(population, Fitness, selection, crossover, mutation);
            //_ga.CrossoverProbability = 1.0f;
            _ga.MutationProbability = 0.2f;
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
