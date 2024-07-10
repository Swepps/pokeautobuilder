namespace Utility
{
    public class Globals
	{
        public static readonly List<double> Versions =
        [
            0.1,
            0.2,
            1.0,
            1.1,
            1.2,
            1.3,
            1.4,
        ];
        public static readonly double Version = Versions.Last();
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
