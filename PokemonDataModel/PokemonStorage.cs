using System.Text.Json.Serialization;

namespace PokemonDataModel
{
    public class PokemonStorage
    {
        [JsonPropertyName("boxes")]
        public List<PokemonBox> Boxes { get; set; }

        public PokemonStorage()
        {
            Boxes = [];
        }
    }
}
