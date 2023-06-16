using MudBlazor;

namespace blazorWebAssemblyApp.Source
{
    using Type = PokeApiNet.Type;
    public class Globals
	{
        public static MudThemeProvider? MudThemeProvider;
        public static string Language = "en";
		public static List<Type> AllTypes = new List<Type>();
		public static SmartPokedex? NationalDex;
		public static PokemonTeam PokemonTeam = new PokemonTeam();
		public static PokemonStorage PokemonStorage = new PokemonStorage();
	}
}
