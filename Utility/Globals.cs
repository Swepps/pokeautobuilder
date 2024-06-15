namespace Utility
{
    public class Globals
	{
        public static readonly Dictionary<double, bool> Versions = new Dictionary<double, bool>()
        {
            { 0.1, true },
            { 0.2, false },
            { 1.0, true },
            { 1.1, true },
            { 1.2, true },
        };
        public static readonly double Version = 1.2;
        public static readonly string Language = "en";

        // ah f*ck it... I'm just gonna hard code the type names in instead
        public static readonly List<string> AllTypes = new List<string>
        {
            "normal",
            "fire",
            "water",
            "electric",
            "grass",
            "ice",
            "fighting",
            "poison",
            "ground",
            "flying",
            "psychic",
            "bug",
            "rock",
            "ghost",
            "dragon",
            "dark",
            "steel",
            "fairy"
        };
    }
}
