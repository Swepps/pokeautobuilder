namespace PokemonDataModel
{
    using Type = PokeApiNet.Type;

    public class DataModelCache
    {
        // still need this for easy access in some classes... I hate it
        public static List<Type> LoadedTypes = [];
    }
}
