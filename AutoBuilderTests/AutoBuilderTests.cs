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

    public class AutoBuilderTests : IAsyncLifetime
    {
        private readonly TypeChart typeChart = new();
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
            apiService = new PokeApiService(new HttpClient(), typeChart);
            await TestFixtures.EnsureRealTypesLoadedAsync(apiService, typeChart);
        }

        [Fact]
        public async Task BasicGeneration()
        {
            PokemonBox box = new();
            box.Pokemon.Add((await apiService!.GetPokemonAsync("pikachu"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("gyarados"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("swampert"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("salamence"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("golem"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("skarmory"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("kyogre"))!);

            PokemonTeamGeneticAlgorithm GA = new();
            AutoBuilderWeightings weightings = new();

            PokemonTeam BestTeam = new();
            GA.GenerationRan += (g) => 
            {
                if (g.BestChromosome is null)
                    return;

                BestTeam = g.BestChromosome.GetTeam();
            };
            PokemonTeam lockedMembers = new();
            for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
            {
                lockedMembers.Pokemon.Add(null);
            }
            GA.Initialize(50, box, lockedMembers, weightings);
            GA.Run(10);

            // check that the final team has 6 unique members
            Assert.False(BestTeam.ContainsDuplicates());
        }

        [Fact]
        public async Task NormalGeneration()
        {
            PokemonBox box = new();
            // add a selection of "bad" pokemon
            box.Pokemon.Add((await apiService!.GetPokemonAsync("rattata"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("wurmple"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("pidgey"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("zigzagoon"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("magikarp"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("whismur"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("spearow"))!);

            // add 6 "good" pokemon that should theoretically get picked
            SmartPokemon lapras = (await apiService.GetPokemonAsync("lapras"))!;
            SmartPokemon gardevoir = (await apiService.GetPokemonAsync("gardevoir"))!;
            SmartPokemon gengar = (await apiService.GetPokemonAsync("gengar"))!;
            SmartPokemon talonflame = (await apiService.GetPokemonAsync("talonflame"))!;
            SmartPokemon ferrothorn = (await apiService.GetPokemonAsync("ferrothorn"))!;
            SmartPokemon gliscor = (await apiService.GetPokemonAsync("gliscor"))!;
            box.Pokemon.Add(lapras);
            box.Pokemon.Add(gardevoir);
            box.Pokemon.Add(gengar);
            box.Pokemon.Add(talonflame);
            box.Pokemon.Add(ferrothorn);
            box.Pokemon.Add(gliscor);

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
            PokemonTeam lockedMembers = new();
            for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
            {
                lockedMembers.Pokemon.Add(null);
            }
            GA.Initialize(250, box, lockedMembers, weightings);
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

        [Fact]
        public async Task LockedGeneration()
        {
            PokemonBox box = new();
            // add a selection of "bad" pokemon
            SmartPokemon spearow = (await apiService!.GetPokemonAsync("spearow"))!; // used for lock test
            box.Pokemon.Add(spearow);
            box.Pokemon.Add((await apiService.GetPokemonAsync("rattata"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("wurmple"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("pidgey"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("zigzagoon"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("magikarp"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("whismur"))!);

            // add 6 "good" pokemon that should theoretically get picked
            SmartPokemon lapras = (await apiService.GetPokemonAsync("lapras"))!;
            SmartPokemon gardevoir = (await apiService.GetPokemonAsync("gardevoir"))!;
            SmartPokemon gengar = (await apiService.GetPokemonAsync("gengar"))!;
            SmartPokemon talonflame = (await apiService.GetPokemonAsync("talonflame"))!;
            SmartPokemon ferrothorn = (await apiService.GetPokemonAsync("ferrothorn"))!;
            SmartPokemon gliscor = (await apiService.GetPokemonAsync("gliscor"))!;
            box.Pokemon.Add(lapras);
            box.Pokemon.Add(gardevoir);
            box.Pokemon.Add(gengar);
            box.Pokemon.Add(talonflame);
            box.Pokemon.Add(ferrothorn);
            box.Pokemon.Add(gliscor);

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

            // lock some members of the team so they cannot change
            PokemonTeam lockedMembers = new();
            // 2 "modifiable" slots
            for (int i = 0; i < 2; i++)
            {
                lockedMembers.Pokemon.Add(null);
            }
            lockedMembers.Pokemon.Add(spearow);
            lockedMembers.Pokemon.Add(gliscor);

            GA.Initialize(250, box, lockedMembers, weightings);
            GA.Run(50);

            // check that the final team has unique members
            Assert.False(bestTeam.ContainsDuplicates());

            // check that the algorithm hasn't changed the locked members
            Assert.True(bestTeam.Pokemon[2] == spearow);
            Assert.True(bestTeam.Pokemon[3] == gliscor);
        }

        [Fact]
        public async Task OnlyOneMegaEvolved()
        {
            PokemonBox box = new();
            // add a selection of "bad" pokemon
            box.Pokemon.Add((await apiService!.GetPokemonAsync("rattata"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("wurmple"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("pidgey"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("zigzagoon"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("magikarp"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("whismur"))!);
            box.Pokemon.Add((await apiService.GetPokemonAsync("spearow"))!);

            // add 2 mega-evolved pokemon. only one should be added to the team
            SmartPokemon megaGarchomp = (await apiService.GetPokemonAsync("garchomp-mega"))!;
            SmartPokemon megaCharizard = (await apiService.GetPokemonAsync("charizard-mega-y"))!;

            // add 4 other good pokemon to fill the space of a "good" team
            SmartPokemon gengar = (await apiService.GetPokemonAsync("gengar"))!;
            SmartPokemon talonflame = (await apiService.GetPokemonAsync("talonflame"))!;
            SmartPokemon ferrothorn = (await apiService.GetPokemonAsync("ferrothorn"))!;
            SmartPokemon gliscor = (await apiService.GetPokemonAsync("gliscor"))!;

            box.Pokemon.Add(megaGarchomp);
            box.Pokemon.Add(megaCharizard);
            box.Pokemon.Add(gengar);
            box.Pokemon.Add(talonflame);
            box.Pokemon.Add(ferrothorn);
            box.Pokemon.Add(gliscor);

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
            PokemonTeam lockedMembers = new();
            for (int i = 0; i < PokemonTeam.MaxTeamSize; i++)
            {
                lockedMembers.Pokemon.Add(null);
            }
            GA.Initialize(250, box, lockedMembers, weightings);
            GA.Run(50);

            // check that the final team has 6 unique members
            Assert.False(bestTeam.ContainsDuplicates());

            // check that the algorithm has not chosen both mega-evolved pokemon
            Assert.True(bestTeam.CountMegaPokemon() <= 1);
        }
    }
}