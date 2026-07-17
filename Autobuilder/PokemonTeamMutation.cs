using GeneticSharp;
using PokemonDataModel;

namespace Autobuilder
{
    // Replaces each non-locked team slot, with the given probability, with a random box member
    // that isn't already on the team. This is the operator that injects brand-new Pokemon into
    // the gene pool every generation - without it, a Pokemon absent from the initial population
    // could never appear in any team. Avoiding members already on the team means a mutation
    // never produces a duplicate-carrying team that PokemonTeamFitness would score 0.
    public class PokemonTeamMutation : MutationBase
    {
        private readonly PokemonBox _box;

        public PokemonTeamMutation(PokemonBox box)
        {
            _box = box;
        }

        protected override void PerformMutate(IChromosome chromosome, float probability)
        {
            var teamChromosome = (PokemonTeamChromosome)chromosome;
            var random = RandomizationProvider.Current;

            var used = new HashSet<SmartPokemon>(ReferenceEqualityComparer.Instance);
            for (int i = 0; i < teamChromosome.Length; i++)
            {
                if (teamChromosome.GetGene(i).Value is SmartPokemon member)
                    used.Add(member);
            }

            for (int i = 0; i < teamChromosome.Length; i++)
            {
                if (teamChromosome.IsGeneLocked(i))
                    continue;

                if (random.GetDouble() > probability)
                    continue;

                SmartPokemon? replacement = BoxSampler.GetRandomPokemonExcluding(_box, used);
                if (replacement is null)
                    return; // box has no unused members left - nothing can mutate

                if (teamChromosome.GetGene(i).Value is SmartPokemon current)
                    used.Remove(current);
                used.Add(replacement);

                teamChromosome.ReplaceGene(i, new Gene(replacement));
            }
        }
    }
}
