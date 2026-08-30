namespace PokemonDataModel
{
    // Parses PokeAPI's generation resource names ("generation-i".."generation-ix") into the plain
    // integers this app uses everywhere else (BoxRules.Generation, PokemonVersionGroup.Generation) -
    // shared by SmartPokemon's past-types resolution and PokeApiService's evolution-generation check.
    public static class PokemonGenerationNames
    {
        public static int Parse(string generationApiName) =>
            generationApiName switch
            {
                "generation-i" => 1,
                "generation-ii" => 2,
                "generation-iii" => 3,
                "generation-iv" => 4,
                "generation-v" => 5,
                "generation-vi" => 6,
                "generation-vii" => 7,
                "generation-viii" => 8,
                "generation-ix" => 9,
                // an unrecognized/future generation name can never be "the applicable era" for any
                // generation this app knows about - treat it as arbitrarily far in the future rather
                // than guessing
                _ => int.MaxValue,
            };
    }
}
