using PokeApiNet;

namespace PokemonDataModel
{
    public interface IPokemonSearchable
    {
        string Name
        {
            get;
            set;
        }
        IEnumerable<NamedApiResource<Pokemon>> GetAllVarieties();
        Task<IEnumerable<NamedApiResource<Pokemon>>> GetAllVarietiesAsync();
    }
}
