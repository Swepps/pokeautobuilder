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
    }
}
