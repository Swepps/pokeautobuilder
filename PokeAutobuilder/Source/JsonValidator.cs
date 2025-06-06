using PokemonDataModel;
using System.Text.Json;

namespace PokeAutobuilder.Source
{
    public static class JsonValidator
    {
        private static readonly JsonSerializerOptions deserializeOptions = new()
        {
            
        };

        public static bool TryValidatePokemonBoxJson(string json, out PokemonBox? box)
        {
            try
            {
                box = JsonSerializer.Deserialize<PokemonBox>(json, deserializeOptions);

                if (box == null || box.Pokemon == null)
                    return false;

                // Optional: further validation logic, e.g. ensure no nulls in Pokemon list
                if (box.Pokemon.Any(p => p == null))
                    return false;

                return true;
            }
            catch
            {
                box = null;
                return false;
            }
        }
    }
}
