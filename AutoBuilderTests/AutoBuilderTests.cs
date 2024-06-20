using AutoBuilder;
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
        private PokeApiService? apiService;

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            apiService = new PokeApiService(new HttpClient());
            if (DataModelCache.LoadedTypes.Count == 0)
            {
                DataModelCache.LoadedTypes = await apiService.GetAllTypesAsync();
            }
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


    public class AutoBuilderTests : IAsyncLifetime
    {
        private PokeApiService? apiService;
        private readonly ITestOutputHelper output;

        public AutoBuilderTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            apiService = new PokeApiService(new HttpClient());
            if (DataModelCache.LoadedTypes.Count == 0)
            {
                DataModelCache.LoadedTypes = await apiService.GetAllTypesAsync();
            }
        }

        [Fact]
        public async Task BasicGeneration()
        {
            PokemonStorage storage = new();
            storage.Pokemon.Add((await apiService!.GetPokemonAsync("pikachu"))!);
            storage.Pokemon.Add((await apiService.GetPokemonAsync("gyarados"))!);
            storage.Pokemon.Add((await apiService.GetPokemonAsync("swampert"))!);
            storage.Pokemon.Add((await apiService.GetPokemonAsync("salamence"))!);
            storage.Pokemon.Add((await apiService.GetPokemonAsync("golem"))!);
            storage.Pokemon.Add((await apiService.GetPokemonAsync("skarmory"))!);
            storage.Pokemon.Add((await apiService.GetPokemonAsync("kyogre"))!);

            PokemonTeamGeneticAlgorithm GA = new();
            AutoBuilderWeightings weightings = new();

            PokemonTeam BestTeam = new();
            GA.GenerationRan += (g) => 
            {
                if (g.BestChromosome is null)
                    return;

                BestTeam = g.BestChromosome.GetTeam();
            };
            GA.Initialize(50, storage, new PokemonTeam(), weightings);
            GA.Run(10);

            // check that the final team has 6 unique members
            Assert.False(BestTeam.ContainsDuplicates());
        }

        [Fact]
        public async Task NormalGeneration()
        {
            PokemonStorage storage = new();
            // add a selection of "bad" pokemon
            storage.Pokemon.Add((await apiService!.GetPokemonAsync("rattata"))!);
            storage.Pokemon.Add((await apiService.GetPokemonAsync("wurmple"))!);
            storage.Pokemon.Add((await apiService.GetPokemonAsync("pidgey"))!);
            storage.Pokemon.Add((await apiService.GetPokemonAsync("zigzagoon"))!);
            storage.Pokemon.Add((await apiService.GetPokemonAsync("magikarp"))!);
            storage.Pokemon.Add((await apiService.GetPokemonAsync("whismur"))!);
            storage.Pokemon.Add((await apiService.GetPokemonAsync("spearow"))!);

            // add 6 "good" pokemon that should theoretically get picked
            SmartPokemon lapras = (await apiService.GetPokemonAsync("lapras"))!;
            SmartPokemon gardevoir = (await apiService.GetPokemonAsync("gardevoir"))!;
            SmartPokemon gengar = (await apiService.GetPokemonAsync("gengar"))!;
            SmartPokemon talonflame = (await apiService.GetPokemonAsync("talonflame"))!;
            SmartPokemon ferrothorn = (await apiService.GetPokemonAsync("ferrothorn"))!;
            SmartPokemon gliscor = (await apiService.GetPokemonAsync("gliscor"))!;
            storage.Pokemon.Add(lapras);
            storage.Pokemon.Add(gardevoir);
            storage.Pokemon.Add(gengar);
            storage.Pokemon.Add(talonflame);
            storage.Pokemon.Add(ferrothorn);
            storage.Pokemon.Add(gliscor);

            PokemonTeamGeneticAlgorithm GA = new();
            AutoBuilderWeightings weightings = new();

            PokemonTeam bestTeam = new();
            double? bestScore = 0;
            GA.GenerationRan += (g) =>
            {
                if (g.BestChromosome is null)
                    return;

                if (g.BestChromosome.Fitness > bestScore)
                {
                    bestScore = g.BestChromosome.Fitness;
                    bestTeam = g.BestChromosome.GetTeam();
                }

                output.WriteLine("{0,-4}|{1,-9:0.000}|{2,-14:0.000}|{3,-30}"
                    , g.GenerationsNumber
                    , g.BestChromosome.Fitness
                    , bestScore
                    , bestTeam.ToString()
                    );
            };
            output.WriteLine("Gen |G.Fitness|Best Fitness  |Best Team");
            GA.Initialize(250, storage, new PokemonTeam(), weightings);
            GA.Run(50);

            // check that the final team has 6 unique members
            Assert.False(bestTeam.ContainsDuplicates());

            // check that the algorithm has picked out the 6 good pokemon
            Assert.Contains(lapras, bestTeam.Pokemon);
            Assert.Contains(gardevoir, bestTeam.Pokemon);
            Assert.Contains(gengar, bestTeam.Pokemon);
            Assert.Contains(talonflame, bestTeam.Pokemon);
            Assert.Contains(ferrothorn, bestTeam.Pokemon);
            Assert.Contains(gliscor, bestTeam.Pokemon);
        }
    }
}