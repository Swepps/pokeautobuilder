using Autobuilder;
using Moq.Protected;
using Moq;
using PokeApiNet;
using PokemonDataModel;
using System.Net;
using Xunit;
using Accord.Math;
using Utility;
using Xunit.Abstractions;

namespace PokeAutobuilderTests
{
    using Type = PokeApiNet.Type;

    public class PokeApiServiceTests : IAsyncLifetime
    {
        private readonly TypeChart typeChart = new();
        private PokeApiService? apiService;

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            apiService = new PokeApiService(new HttpClient(), typeChart);
            await TestFixtures.EnsureRealTypesLoadedAsync(apiService, typeChart);
        }

        [Fact]
        public async Task ApiServiceGetPokedex()
        {
            SmartPokedex? nationalDex = await apiService!.GetNationalDexAsync();
            Assert.NotNull(nationalDex);

            Pokedex? hoennDex = await apiService.GetPokedexAsync(4);
            Assert.NotNull(hoennDex);
            Assert.Equal("hoenn", hoennDex.Name);
        }

        [Fact]
        public async Task ApiServiceGetPokemon()
        {
            SmartPokemon? pikachu = await apiService!.GetPokemonAsync("pikachu");
            Assert.NotNull(pikachu);
            Assert.Equal("pikachu", pikachu.Name);

            SmartPokemon? gholdengo = await apiService!.GetPokemonAsync(1000);
            Assert.NotNull(gholdengo);
            Assert.Equal("gholdengo", gholdengo.Name);
        }

        [Fact]
        public async Task ApiServiceGetAllTypes()
        {
            List<Type> types = await apiService!.GetAllTypesAsync();
            Assert.NotEmpty(types);
            Assert.All(types, t => Assert.Contains(t.Name, Globals.AllTypes));
        }

        [Fact]
        public async Task ApiServiceGetPokemonSpeciesByEntry()
        {
            Pokedex? nationalDex = await apiService!.GetPokedexAsync(1);
            Assert.NotNull(nationalDex);
            PokemonSpecies? species = await apiService!.GetPokemonSpeciesAsync(nationalDex.PokemonEntries[24]);
            Assert.NotNull(species);
            Assert.Equal("pikachu", species.Name);
        }

        [Fact]
        public async Task ApiServiceGetPokemonSpeciesBySmartPokemon()
        {
            SmartPokemon? pikachu = await apiService!.GetPokemonAsync("pikachu");
            Assert.NotNull(pikachu);
            PokemonSpecies? species = await apiService!.GetPokemonSpeciesAsync(pikachu!);
            Assert.NotNull(species);
            Assert.Equal("pikachu", species.Name);
        }

        [Fact]
        public async Task ApiServiceGetPokemonSpeciesByName()
        {
            PokemonSpecies? species = await apiService!.GetPokemonSpeciesAsync("pikachu");
            Assert.NotNull(species);
            Assert.Equal("pikachu", species.Name);
        }

        [Fact]
        public async Task ApiServiceGetGeneration()
        {
            PokemonSpecies? species = await apiService!.GetPokemonSpeciesAsync("pikachu");
            Assert.NotNull(species);
            Generation? generation = await apiService!.GetGenerationAsync(species);
            Assert.NotNull(generation);
            Assert.Equal("generation-i", generation.Name);
        }

        [Fact]
        public async Task ApiServiceGetMove()
        {
            Move? move = await apiService!.GetMoveAsync("tackle");
            Assert.NotNull(move);
            Assert.Equal("tackle", move.Name);
        }

        [Fact]
        public async Task ApiServiceGetPokemonMoves()
        {
            SmartPokemon? pikachu = await apiService!.GetPokemonAsync("pikachu");
            Assert.NotNull(pikachu);
            List<PokemonMove> moves = await apiService.GetPokemonMovesAsync(pikachu!);
            Assert.NotEmpty(moves);
            Assert.All(moves, move => Assert.NotNull(move.Move));
        }

        [Fact]
        public async Task ApiServiceGetPokemonMovesForRegionalVariant()
        {
            // marowak-alola should get its own variety's moves (e.g. shadow-bone, which regular
            // Kanto marowak can't learn), not the default marowak variety's moves, while still
            // inheriting moves from its pre-evolution cubone (e.g. bone-club)
            SmartPokemon? marowakAlola = await apiService!.GetPokemonAsync("marowak-alola");
            Assert.NotNull(marowakAlola);
            List<PokemonMove> moves = await apiService.GetPokemonMovesAsync(marowakAlola!);

            Assert.Contains(moves, move => move.Move.Name == "shadow-bone");
            Assert.Contains(moves, move => move.Move.Name == "bone-club");
            Assert.DoesNotContain(moves, move => move.Move.Name == "sing"); // kanto marowak-only move that cubone can't learn
        }

        [Fact]
        public async Task ApiServiceGetType()
        {
            Type? type = await apiService!.GetTypeAsync("electric");
            Assert.NotNull(type);
            Assert.Equal("electric", type.Name);
        }

        [Fact]
        public async Task ApiServiceGetPokemonTypes()
        {
            SmartPokemon? pikachu = await apiService!.GetPokemonAsync("pikachu");
            Assert.NotNull(pikachu);
            List<Type> types = await apiService!.GetPokemonTypesAsync(pikachu);
            Assert.NotEmpty(types);
            Assert.All(types, t => Assert.Equal("electric", t.Name));
        }

        [Fact]
        public async Task ApiServiceGetFinalEvolution()
        {
            SmartPokemon? pikachu = await apiService!.GetPokemonAsync("pikachu");
            Assert.NotNull(pikachu);
            SmartPokemon? finalEvolution = await apiService.GetFinalEvolution(pikachu);
            Assert.NotNull(finalEvolution);
            Assert.NotEqual("pikachu", finalEvolution.Name); // Assuming Pikachu is not the final evolution in the chain
        }
    }
}
