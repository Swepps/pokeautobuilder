using PokeApiNet;

namespace blazorWebAssemblyApp.Source
{
    using Type = PokeApiNet.Type;
    internal class PokeApiHandler
    {
        private static PokeApiClient ApiClient = new PokeApiClient();

        public static async Task<List<Type>> GetAllTypesAsync()
        {
            List<Type> pokemonTypes = new List<Type>();

            NamedApiResourceList<Type> allTypesPage = await ApiClient.GetNamedResourcePageAsync<Type>();
            foreach (NamedApiResource<Type> res in allTypesPage.Results)
            {
                Type type = await ApiClient.GetResourceAsync(res);

                // there are some weird types in the API with Ids of over 1000 that aren't real types
                if (type == null || type.Id > 18)
                    continue;

                pokemonTypes.Add(type);
            }

            return pokemonTypes;
        }

        public static async Task<Pokedex?> GetNationalDex()
        {
            return await GetPokedex(1);
        }
        public static async Task<Pokedex?> GetPokedex(int i)
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

        public static async Task<PokemonSpecies?> GetPokemonSpeciesAsync(PokemonEntry entry)
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
        public static async Task<PokemonSpecies?> GetPokemonSpeciesAsync(SmartPokemon pokemon)
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
        public static async Task<PokemonSpecies?> GetPokemonSpeciesAsync(string speciesName)
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

        public static async Task<SmartPokemon?> GetPokemonAsync(string pokemonName)
        {
            pokemonName = pokemonName.ToLower();
            try
            {
                Pokemon pokemon = await ApiClient.GetResourceAsync<Pokemon>(pokemonName);
                SmartPokemon smartPokemon = new SmartPokemon(pokemon);

                return smartPokemon;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }
        public static async Task<SmartPokemon?> GetPokemonAsync(int pokedexId)
        {
            try
            {
                Pokemon pokemon = await ApiClient.GetResourceAsync<Pokemon>(pokedexId);
                SmartPokemon smartPokemon = new SmartPokemon(pokemon);

                return smartPokemon;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public static async Task<Generation?> GetGenerationAsync(PokemonSpecies species)
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

        public static async Task<Multipliers> GetPokemonMultipliersAsync(SmartPokemon pokemon)
        {
            Multipliers multipliers = new Multipliers();

            List<Type> types = await ApiClient.GetResourceAsync(pokemon.Types.Select(type => type.Type));

            foreach (Type type in types)
            {
                // get lists of type name to check effectivenesses
                // don't need full type details, just the names
                TypeRelations tr = type.DamageRelations;
                var noDamageTo = tr.NoDamageTo;
                var noDamageFrom = tr.NoDamageFrom;
                var halfDamageTo = tr.HalfDamageTo;
                var halfDamageFrom = tr.HalfDamageFrom;
                var doubleDamageTo = tr.DoubleDamageTo;
                var doubleDamageFrom = tr.DoubleDamageFrom;

                // immune types
                foreach (var namedType in noDamageTo)
                {
                    //multipliers.coverage[namedType.Name] = 0;
                }
                foreach (var namedType in noDamageFrom)
                {
                    multipliers.defense[namedType.Name] = 0;
                }

                // resistant types
                foreach (var namedType in halfDamageTo)
                {
                    //if (!multipliers.coverage.ContainsKey(namedType.Name))
                    //{
                    //    multipliers.coverage[namedType.Name] = multipliers.coverage[namedType.Name] * 0.5;
                    //}
                    //else
                    //{
                    //    multipliers.coverage[namedType.Name] = 0.5;
                    //}
                }
                foreach (var namedType in halfDamageFrom)
                {
                    if (multipliers.defense.ContainsKey(namedType.Name))
                    {
                        multipliers.defense[namedType.Name] = multipliers.defense[namedType.Name] * 0.5;
                    }
                    else
                    {
                        multipliers.defense[namedType.Name] = 0.5;
                    }
                }

                // super effective types
                foreach (var namedType in doubleDamageTo)
                {
                    //if (multipliers.coverage.ContainsKey(namedType.Name))
                    //{
                    //    multipliers.coverage[namedType.Name] = multipliers.coverage[namedType.Name] * 2.0;
                    //}
                    //else
                    //{
                    //    multipliers.coverage[namedType.Name] = 2.0;
                    //}
                    multipliers.coverage[namedType.Name] = true;
                }
                foreach (var namedType in doubleDamageFrom)
                {
                    if (multipliers.defense.ContainsKey(namedType.Name))
                    {
                        multipliers.defense[namedType.Name] = multipliers.defense[namedType.Name] * 2.0;
                    }
                    else
                    {
                        multipliers.defense[namedType.Name] = 2.0;
                    }
                }
            }

            return multipliers;
        }

        public static async Task<SmartPokemon?> GetFinalEvolution(SmartPokemon pokemon)
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
