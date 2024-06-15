using GeneticSharp;
using PokemonDataModel;

namespace AutoBuilder
{
    public class PokemonTeamGeneticAlgorithm
    {
        GeneticAlgorithm? _ga;
        Timer? _timer;
        public event Action? GenerationRan;
        public PokemonTeamFitness? Fitness { get; private set; }
        public PokemonTeamChromosome? BestChromosome => _ga != null ? _ga.BestChromosome as PokemonTeamChromosome : null;
        public int GenerationsNumber => _ga != null ? _ga.GenerationsNumber : 0;
        public bool IsRunning => _timer != null;

        public void Initialize(int populationsize, PokemonStorage storage, PokemonTeam lockedMembers, AutoBuilderWeightings weightings)
        {
            Stop();
            Fitness = new PokemonTeamFitness(weightings);
            var chromosome = new PokemonTeamChromosome(storage, lockedMembers);

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

        public void Run()
        {
            if (!IsRunning)
            {
                // As is there no way to use a new thread on WebAssembly right now, we wil use a timer
                // to start a new generation each 1 microsecond.
                _timer = new Timer(new TimerCallback(_ =>
                {
                    if (_ga is null)
                        return;

                    _ga.Termination = new GenerationNumberTermination(_ga.GenerationsNumber + 1);
                    if (_ga.GenerationsNumber > 0)
                        _ga.Resume();
                    else
                        _ga.Start();
                    GenerationRan?.Invoke();
                }), null, 0, 1);
            }
        }

        public void Stop()
        {
            if (IsRunning && _timer is not null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
