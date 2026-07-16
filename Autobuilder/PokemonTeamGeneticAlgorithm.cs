using GeneticSharp;
using PokemonDataModel;

namespace Autobuilder
{
    public class PokemonTeamGeneticAlgorithm
    {
        GeneticAlgorithm? _ga;
        Timer? _timer;
        public event Action<PokemonTeamGeneticAlgorithm>? GenerationRan;
        public PokemonTeamFitness? Fitness { get; private set; }
        public PokemonTeamChromosome? BestChromosome => _ga != null ? _ga.BestChromosome as PokemonTeamChromosome : null;
        public int GenerationsNumber => _ga != null ? _ga.GenerationsNumber : 0;
        public bool IsRunning => _timer != null;

        public void Initialize(int populationsize, PokemonBox box, PokemonTeam lockedMembers, AutobuilderWeightings weightings)
        {
            Stop();
            Fitness = new PokemonTeamFitness(weightings);
            var chromosome = new PokemonTeamChromosome(box, lockedMembers);

            // This operators are classic genetic algorithm operators that lead to a good solution on TSP,
            // but you can try others combinations and see what result you get.
            var crossover = new OrderedCrossover();
            var mutation = new ReverseSequenceMutation();
            var selection = new RouletteWheelSelection();
            var population = new Population(populationsize, populationsize, chromosome);

            _ga = new GeneticAlgorithm(population, Fitness, selection, crossover, mutation);
            //_ga.CrossoverProbability = 1.0f;
            _ga.MutationProbability = 0.2f;
        }

        readonly object _lock = new();

        public void RunInBackground()
        {
            lock (_lock)
            {
                if (IsRunning)
                    return;

                // There is no way to use a new thread on WebAssembly right now, so we use a timer
                // to run one generation per tick, letting the UI thread stay responsive in between.
                _timer = new Timer(new TimerCallback(_ =>
                {
                    lock (_lock)
                    {
                        RunOneGeneration();
                    }
                }), null, 0, 1);
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
            lock (_lock)
            {
                if (IsRunning && _timer is not null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }
    }
}
