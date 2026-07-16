using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokemonDataModel
{
    public interface ILazyPokemonList
    {
        string Name
        {
            get;
            set;
        }
        Task<IEnumerable<IPokemonSearchable>> GetListAsync();
        bool IsEmpty();
    }

    public static class LazyPokemonListExtensions
    {
        // case-insensitive filter by name, shared by every page's PokemonSearchBox.SearchFunc
        public static async Task<IEnumerable<IPokemonSearchable>> SearchAsync(this ILazyPokemonList? source, string? searchString)
        {
            if (source is null)
                return Enumerable.Empty<IPokemonSearchable>();

            IEnumerable<IPokemonSearchable> list = await source.GetListAsync();
            if (String.IsNullOrEmpty(searchString))
                return list;

            return list.Where(p => p.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase));
        }
    }
}
