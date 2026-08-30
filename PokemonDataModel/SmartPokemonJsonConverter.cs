using System.Text.Json;
using System.Text.Json.Serialization;

namespace PokemonDataModel
{
    // Deserializes SmartPokemon and immediately resolves its types/multipliers against the given
    // (base/Unrestricted) TypeChart, so a deserialized SmartPokemon is always fully initialized -
    // callers never see a half-constructed instance. Registered into Blazored local/session
    // storage's JsonSerializerOptions in Program.cs, which covers every persistence path (boxes,
    // team storage, session team, box upload). This converter only has access to the single base
    // chart injected in Program.cs, not to whichever box/team ruleset the deserialized Pokemon will
    // end up under - that ruleset's cache entry is warmed separately once the Pokemon is attached
    // to its box/team (see PokemonBox.EnsureInitialized / ProfileService).
    public class SmartPokemonJsonConverter : JsonConverter<SmartPokemon>
    {
        private readonly TypeChart _typeChart;

        public SmartPokemonJsonConverter(TypeChart typeChart)
        {
            _typeChart = typeChart;
        }

        // Read/Write is called once per pokemon and copying JsonSerializerOptions is expensive
        // (it rebuilds serializer metadata caches), so cache the stripped copy per source options
        private readonly Dictionary<JsonSerializerOptions, JsonSerializerOptions> _optionsCache =
            new();

        // the [JsonConstructor] path must run without this converter or Read/Write would recurse
        private JsonSerializerOptions WithoutThisConverter(JsonSerializerOptions options)
        {
            if (_optionsCache.TryGetValue(options, out JsonSerializerOptions? cached))
            {
                return cached;
            }

            JsonSerializerOptions copy = new(options);
            for (int i = copy.Converters.Count - 1; i >= 0; i--)
            {
                if (copy.Converters[i] is SmartPokemonJsonConverter)
                {
                    copy.Converters.RemoveAt(i);
                }
            }
            _optionsCache[options] = copy;
            return copy;
        }

        public override SmartPokemon? Read(
            ref Utf8JsonReader reader,
            System.Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            SmartPokemon? pokemon = JsonSerializer.Deserialize<SmartPokemon>(
                ref reader,
                WithoutThisConverter(options)
            );
            pokemon?.InitializeTypes(_typeChart, BoxRules.Unrestricted());
            return pokemon;
        }

        public override void Write(
            Utf8JsonWriter writer,
            SmartPokemon value,
            JsonSerializerOptions options
        )
        {
            JsonSerializer.Serialize(writer, value, WithoutThisConverter(options));
        }
    }
}
