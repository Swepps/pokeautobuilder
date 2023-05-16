using PokeApiNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace autoteambuilder
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

        public static async Task<SmartPokemon?> GetPokemonAsync(string pokemonName)
        {
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
                    multipliers.attack[namedType.Name] = 0;
                }
                foreach (var namedType in noDamageFrom)
                {
                    multipliers.defense[namedType.Name] = 0;
                }

                // resistant types
                foreach (var namedType in halfDamageTo)
                {
                    if (multipliers.attack.ContainsKey(namedType.Name))
                    {
                        multipliers.attack[namedType.Name] = multipliers.attack[namedType.Name] * 0.5;
                    }
                    else
                    {
                        multipliers.attack[namedType.Name] = 0.5;
                    }
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
                    if (multipliers.attack.ContainsKey(namedType.Name))
                    {
                        multipliers.attack[namedType.Name] = multipliers.attack[namedType.Name] * 2.0;
                    }
                    else
                    {
                        multipliers.attack[namedType.Name] = 2.0;
                    }
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
    }
}
