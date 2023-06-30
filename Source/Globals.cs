﻿using MudBlazor;

namespace pokeAutoBuilder.Source
{
    using Type = PokeApiNet.Type;

    public class Globals
	{
        public static Dictionary<double, bool> Versions = new Dictionary<double, bool>()
        {
            { 0.1, true }
        };
        public static double Version = 0.1;
        public static string Language = "en";
        public static MudThemeProvider? MudThemeProvider;
        public static SmartPokedex? NationalDex;
		public static PokemonTeam PokemonTeam = new PokemonTeam();
		public static PokemonStorage PokemonStorage = new PokemonStorage();

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
