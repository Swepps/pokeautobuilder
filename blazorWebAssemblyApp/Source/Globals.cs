namespace blazorWebAssemblyApp.Source
{
	using Type = PokeApiNet.Type;
	public class Globals
	{
		public static string Language = "en";
		public static List<Type> AllTypes = new List<Type>();
		public static SmartPokedex Pokedex = new SmartPokedex();
	}
}
