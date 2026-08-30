using PokemonDataModel;
using System.Text.Json;

namespace PokeAutobuilder.Source
{
    public static class JsonValidator
    {
        private static readonly JsonSerializerOptions deserializeOptions = new()
        {
            
        };

        public static bool TryValidatePokemonBoxJson(string json, out PokemonBox? box, out string? warning)
        {
            warning = null;
            try
            {
                box = JsonSerializer.Deserialize<PokemonBox>(json, deserializeOptions);

                if (box == null || box.Pokemon == null)
                    return false;

                // Optional: further validation logic, e.g. ensure no nulls in Pokemon list
                if (box.Pokemon.Any(p => p == null))
                    return false;

                // this running app doesn't understand the newer ruleset fields a future version
                // added. Surface a warning here for immediate feedback - the actual reset happens
                // in ProfileService when the box is loaded for real (this validator's raw JSON
                // never reaches storage as-is; local-storage-helper.js writes the original upload
                // text directly, this `box` is only used to decide whether to accept it)
                if (box.Rules.SchemaVersion > BoxRules.CurrentSchemaVersion)
                {
                    warning =
                        "This box's ruleset was created by a newer version of the app and could not "
                        + "be fully understood, so it has been reset to unrestricted.";
                }

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
