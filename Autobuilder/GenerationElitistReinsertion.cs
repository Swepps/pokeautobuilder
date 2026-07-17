using GeneticSharp;

namespace Autobuilder
{
    // Like GeneticSharp's ElitistReinsertion, but takes elites from the whole previous
    // generation instead of from the selected parents. The stock version only preserves
    // chromosomes that selection happened to pick as parents - under weak selection pressure
    // the actual best team frequently isn't among them, so the best solution found so far
    // regularly falls out of the breeding population (visible as the per-generation best
    // fitness fluctuating downwards). Drawing elites from the generation itself guarantees
    // the top teams always survive.
    //
    // Returned lists may exceed the population size; Generation.End() sorts by fitness and
    // truncates back down, so the extra elites simply compete with the offspring.
    public class GenerationElitistReinsertion : ReinsertionBase
    {
        private readonly int _minElites;

        public GenerationElitistReinsertion(int minElites = 2)
            : base(false, true)
        {
            _minElites = minElites;
        }

        protected override IList<IChromosome> PerformSelectChromosomes(
            IPopulation population, IList<IChromosome> offspring, IList<IChromosome> parents)
        {
            // at this point population.CurrentGeneration is still the (fully evaluated)
            // previous generation - the new one is created from what we return here
            int needed = Math.Max(_minElites, population.MinSize - offspring.Count);

            IEnumerable<IChromosome> elites = population.CurrentGeneration.Chromosomes
                .Where(c => c.Fitness.HasValue)
                .OrderByDescending(c => c.Fitness)
                .Take(needed);

            foreach (IChromosome elite in elites)
            {
                offspring.Add(elite);
            }

            return offspring;
        }
    }
}
