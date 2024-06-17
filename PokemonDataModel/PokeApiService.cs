using PokeApiNet;
using Utility;

namespace PokemonDataModel
{
    using Type = PokeApiNet.Type;

    public class PokeApiService
    {
        public static PokeApiService? Instance { get; private set; }

        private readonly PokeApiClient ApiClient;

        public PokeApiService(HttpClient httpClient)
        {
            ApiClient = new PokeApiClient(httpClient);
            Instance = this;
        }

        // -- API access functions --

        // it works but I may as well just hard code it so no longer used...
        public async Task<List<Type>> GetAllTypesAsync()
        {
            List<Type> pokemonTypes = new List<Type>();

            NamedApiResourceList<Type> allTypesPage = await ApiClient.GetNamedResourcePageAsync<Type>();
            foreach (NamedApiResource<Type> res in allTypesPage.Results)
            {
                Type type = await ApiClient.GetResourceAsync(res);

                // there are some weird types in the API with Ids of over 1000 that aren't real types
                if (type == null || !Globals.AllTypes.Contains(type.Name))
                    continue;

                pokemonTypes.Add(type);
            }

            return pokemonTypes;
        }

        // we will cache the national pokedex between sessions for faster loading time
        public async Task<SmartPokedex?> GetNationalDexAsync()
        {
            Pokedex? nationalDex = await GetPokedexAsync(1);

            if (nationalDex is not null)
            {
                SmartPokedex smNatDex = new SmartPokedex(nationalDex);
                return smNatDex;
            }

            return null;
        }
        public async Task<Pokedex?> GetPokedexAsync(int i)
        {
            try
            {
                Pokedex pokedex = await ApiClient.GetResourceAsync<Pokedex>(i);
                return pokedex;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public async Task<PokemonSpecies?> GetPokemonSpeciesAsync(PokemonEntry entry)
        {
            try
            {
                PokemonSpecies species = await ApiClient.GetResourceAsync(entry.PokemonSpecies);
                return species;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        public async Task<PokemonSpecies?> GetPokemonSpeciesAsync(SmartPokemon pokemon)
        {
            try
            {
                PokemonSpecies species = await ApiClient.GetResourceAsync(pokemon.Species);
                return species;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        public async Task<PokemonSpecies?> GetPokemonSpeciesAsync(string speciesName)
        {
            try
            {
                PokemonSpecies species = await ApiClient.GetResourceAsync<PokemonSpecies>(speciesName);
                return species;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public async Task<SmartPokemon?> GetPokemonAsync(string pokemonName)
        {
            pokemonName = pokemonName.ToLower();
            try
            {
                Pokemon pokemon = await ApiClient.GetResourceAsync<Pokemon>(pokemonName);
                SmartPokemon smartPokemon = await SmartPokemon.BuildSmartPokemonAsync(pokemon);

                return smartPokemon;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        public async Task<SmartPokemon?> GetPokemonAsync(int pokedexId)
        {
            try
            {
                Pokemon pokemon = await ApiClient.GetResourceAsync<Pokemon>(pokedexId);
                SmartPokemon smartPokemon = await SmartPokemon.BuildSmartPokemonAsync(pokemon);

                return smartPokemon;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public async Task<Generation?> GetGenerationAsync(PokemonSpecies species)
        {
            try
            {
                Generation generation = await ApiClient.GetResourceAsync(species.Generation);

                return generation;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public async Task<Move?> GetMoveAsync(string moveName)
        {
            try
            {
                Move move = await ApiClient.GetResourceAsync<Move>(moveName);

                return move;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public async Task<List<PokemonMove>> GetPokemonMovesAsync(SmartPokemon pokemon)
        {
            try
            {
                Pokemon p = await ApiClient.GetResourceAsync<Pokemon>(pokemon.Id);
                if (p is null)
                    return [];

                return p.Moves;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return [];
            }
        }

		public async Task<Type?> GetTypeAsync(string typeName)
		{
			try
			{
				Type type = await ApiClient.GetResourceAsync<Type>(typeName);

				return type;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return null;
			}
		}

		public async Task<List<Type>> GetPokemonTypesAsync(Pokemon pokemon)
        {
            List<Type> types = new List<Type>();

            // TODO attempt caching of all types... maybe that will speed up load times?
            foreach (PokemonType t in pokemon.Types)
            {
                types.Add(await ApiClient.GetResourceAsync(t.Type));
            }

            return types;
        }

        public async Task<SmartPokemon?> GetFinalEvolution(SmartPokemon pokemon)
        {
            try
            {
                PokemonSpecies species = await ApiClient.GetResourceAsync(pokemon.Species);
                EvolutionChain chain = await ApiClient.GetResourceAsync(species.EvolutionChain);

                // already fully evolved
                if (chain.Chain.EvolvesTo.Count == 0)
                    return pokemon;

                ChainLink link = chain.Chain.EvolvesTo[0];
                NamedApiResource<PokemonSpecies> linkSpecies = link.Species;
                while (link.EvolvesTo.Count > 0)
                {
                    link = link.EvolvesTo[0];
                    linkSpecies = link.Species;
                }

                // it's already the final evolution
                if (species.Name == linkSpecies.Name)
                    return pokemon;

                return await GetPokemonAsync(linkSpecies.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
    }
}
