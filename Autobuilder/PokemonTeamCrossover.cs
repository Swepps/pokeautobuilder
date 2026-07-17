using GeneticSharp;
using PokemonDataModel;

namespace Autobuilder
{
    // Set-based crossover for Pokemon teams. Team score doesn't depend on slot order, so a team
    // is really a *set* of members - each child is built by drawing members from the combined
    // pool of both parents. Unlike a generic per-slot crossover (e.g. UniformCrossover), this can
    // never produce a team containing the same Pokemon twice, which PokemonTeamFitness would
    // otherwise score 0 and throw away. That matters more and more as the population converges:
    // good parents increasingly share members, and with a per-slot mix most of their children
    // would be invalid duplicates exactly when fine-tuning should be happening.
    public class PokemonTeamCrossover : CrossoverBase
    {
        private readonly PokemonBox _box;

        public PokemonTeamCrossover(PokemonBox box)
            : base(2, 2)
        {
            _box = box;
        }

        protected override IList<IChromosome> PerformCross(IList<IChromosome> parents)
        {
            var firstParent = (PokemonTeamChromosome)parents[0];
            var secondParent = (PokemonTeamChromosome)parents[1];

            return new List<IChromosome>
            {
                CreateChild(firstParent, secondParent),
                CreateChild(firstParent, secondParent),
            };
        }

        private IChromosome CreateChild(PokemonTeamChromosome firstParent, PokemonTeamChromosome secondParent)
        {
            var child = (PokemonTeamChromosome)firstParent.CreateNew();
            var used = new HashSet<SmartPokemon>(ReferenceEqualityComparer.Instance);

            // locked slots are fixed by the chromosome itself; count them as already used so the
            // pool never places the same Pokemon in another slot as well
            for (int i = 0; i < child.Length; i++)
            {
                if (child.IsGeneLocked(i) && child.GetGene(i).Value is SmartPokemon locked)
                    used.Add(locked);
            }

            // shuffled pool of both parents' distinct members
            List<SmartPokemon> pool = new(firstParent.Length + secondParent.Length);
            var seen = new HashSet<SmartPokemon>(ReferenceEqualityComparer.Instance);
            for (int i = 0; i < firstParent.Length; i++)
            {
                if (firstParent.GetGene(i).Value is SmartPokemon a && seen.Add(a))
                    pool.Add(a);
                if (secondParent.GetGene(i).Value is SmartPokemon b && seen.Add(b))
                    pool.Add(b);
            }
            Shuffle(pool);

            int poolIndex = 0;
            for (int i = 0; i < child.Length; i++)
            {
                if (child.IsGeneLocked(i))
                    continue;

                SmartPokemon? pick = null;
                while (poolIndex < pool.Count)
                {
                    SmartPokemon candidate = pool[poolIndex++];
                    if (!used.Contains(candidate))
                    {
                        pick = candidate;
                        break;
                    }
                }

                // parents overlapped (or collided with locked members) too much to fill the
                // team from their pool alone - top up from the box
                pick ??= BoxSampler.GetRandomPokemonExcluding(_box, used) ?? _box.GetRandomPokemon();

                child.ReplaceGene(i, new Gene(pick));
                used.Add(pick);
            }

            return child;
        }

        private static void Shuffle(List<SmartPokemon> pool)
        {
            var random = RandomizationProvider.Current;
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = random.GetInt(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
        }
    }
}
