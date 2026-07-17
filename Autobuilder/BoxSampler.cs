using PokemonDataModel;

namespace Autobuilder
{
    // Draws random Pokemon from a box while avoiding ones already picked. The GA operators use
    // this to build/repair teams without introducing duplicate members, since a team containing
    // the same box entry twice is scored 0 by PokemonTeamFitness and would be a wasted individual.
    internal static class BoxSampler
    {
        // Returns a random box member not already in `used`, or null if every member is used.
        // `used` compares by reference to match PokemonTeam.ContainsDuplicates semantics - two
        // distinct box entries of the same species are legitimately different team members.
        public static SmartPokemon? GetRandomPokemonExcluding(PokemonBox box, ISet<SmartPokemon> used)
        {
            int count = box.Pokemon.Count;
            if (count == 0 || used.Count >= count)
                return null;

            // rejection sampling is cheap when the box is much bigger than a team...
            for (int attempt = 0; attempt < 8; attempt++)
            {
                SmartPokemon candidate = box.GetRandomPokemon();
                if (!used.Contains(candidate))
                    return candidate;
            }

            // ...but fall back to a scan from a random offset so small boxes always terminate
            int start = Random.Shared.Next(count);
            for (int i = 0; i < count; i++)
            {
                SmartPokemon candidate = box.Pokemon[(start + i) % count];
                if (!used.Contains(candidate))
                    return candidate;
            }

            return null;
        }
    }
}
