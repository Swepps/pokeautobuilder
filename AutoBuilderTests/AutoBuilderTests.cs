using AutoBuilder;
using Moq.Protected;
using Moq;
using PokeApiNet;
using PokemonDataModel;
using System.Net;
using Xunit;

namespace PokeAutobuilderTests
{
    public class AutoBuilderTests
    {
        private PokeApiService? pokeApiService;

        public AutoBuilderTests()
        {
            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                });
            pokeApiService = new PokeApiService(new HttpClient(mockMessageHandler.Object));
        }

        [Fact]
        public async Task NormalGeneration()
        {
            PokemonStorage storage = new();
            storage.Pokemon.Add((await pokeApiService!.GetPokemonAsync("pikachu"))!);
            storage.Pokemon.Add((await pokeApiService!.GetPokemonAsync("gyarados"))!);
            storage.Pokemon.Add((await pokeApiService!.GetPokemonAsync("swampert"))!);
            storage.Pokemon.Add((await pokeApiService!.GetPokemonAsync("salamence"))!);
            storage.Pokemon.Add((await pokeApiService!.GetPokemonAsync("golem"))!);
            storage.Pokemon.Add((await pokeApiService!.GetPokemonAsync("skarmory"))!);
            storage.Pokemon.Add((await pokeApiService!.GetPokemonAsync("kyogre"))!);

            Assert.Equal(7, storage.Pokemon.Count);

            PokemonTeamGeneticAlgorithm GA = new();
            AutoBuilderWeightings weightings = new();

            GA.Initialize(250, storage, new PokemonTeam(), weightings);
            GA.Run();
        }
    }
}