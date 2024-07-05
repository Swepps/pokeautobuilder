using PokeApiNet;

namespace PokemonDataModel
{
    public interface IPokemonSearchable
    {
        IEnumerable<NamedApiResource<Pokemon>> GetAllVarieties();
        Task<IEnumerable<NamedApiResource<Pokemon>>> GetAllVarietiesAsync();
    }
}
