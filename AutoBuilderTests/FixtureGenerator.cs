using System.Text.Json;
using PokemonDataModel;
using Xunit;

namespace PokeAutobuilderTests
{
    // Regenerates AutoBuilderTests/Fixtures/sample-box.json - a real Pokemon box for manual/browser
    // testing, so testing a change doesn't require clicking through the search-box + dialog flow
    // for each Pokemon. The output matches the exact shape the app's own storage page "Upload a
    // Pokemon box" button expects (a single PokemonBox, see JsonValidator.TryValidatePokemonBoxJson
    // and PokemonStoragePage.OnClickDownloadBoxAsync), so it can be imported through the UI as-is.
    //
    // Only needs re-running if the fixture goes stale (e.g. SmartPokemon's shape changes) - run with:
    //   dotnet test --filter "FullyQualifiedName~FixtureGenerator"
    public class FixtureGenerator
    {
        [Fact]
        public async Task GenerateSampleBox()
        {
            PokeApiService apiService = new(new HttpClient());
            await TestFixtures.EnsureRealTypesLoadedAsync(apiService);
            string[] names = ["pikachu", "charizard", "blastoise", "venusaur", "snorlax", "gengar"];

            List<SmartPokemon> pokemon = [];
            foreach (string name in names)
            {
                SmartPokemon? mon = await apiService.GetPokemonAsync(name);
                Assert.NotNull(mon);
                pokemon.Add(mon);
            }

            PokemonBox box = new("Sample Box", pokemon);
            string json = JsonSerializer.Serialize(box);

            string fixturePath = Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "Fixtures",
                "sample-box.json"
            );
            Directory.CreateDirectory(Path.GetDirectoryName(fixturePath)!);
            await File.WriteAllTextAsync(fixturePath, json);
        }
    }
}
