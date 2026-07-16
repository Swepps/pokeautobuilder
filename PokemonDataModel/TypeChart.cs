namespace PokemonDataModel
{
    using Type = PokeApiNet.Type;

    // Holds the loaded Pokemon types (with their damage relations) for resolving a Pokemon's type
    // names into full Type objects. An instance class (registered in DI, populated by
    // ProfileService at startup) rather than a global static, so tests can use isolated charts and
    // future features can build alternative charts (e.g. per-generation type effectiveness).
    public class TypeChart
    {
        private readonly Dictionary<string, Type> _typesByName = new();

        public bool IsEmpty => _typesByName.Count == 0;

        public void Populate(IEnumerable<Type> types)
        {
            _typesByName.Clear();
            foreach (Type type in types)
            {
                _typesByName[type.Name] = type;
            }
        }

        public void Add(Type type)
        {
            _typesByName[type.Name] = type;
        }

        public Type Resolve(string typeName)
        {
            if (_typesByName.TryGetValue(typeName, out Type? type))
            {
                return type;
            }

            throw new InvalidOperationException(
                $"Type '{typeName}' is not present in the type chart"
                    + (IsEmpty ? " (the chart has not been populated)" : "")
            );
        }
    }
}
