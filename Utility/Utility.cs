namespace Utility
{
    public class StringUtils
    {
        public static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("ARGH!");
            return input.First().ToString().ToUpper() + string.Join("", input.Skip(1));
        }

        public static string TypeImgFromName(string typeName)
        {
            switch (typeName)
            {
                case "normal":
                    return "img/Type-Normal.png";
                case "fighting":
                    return "img/Type-Fighting.png";
                case "flying":
                    return "img/Type-Flying.png";
                case "poison":
                    return "img/Type-Poison.png";
                case "ground":
                    return "img/Type-Ground.png";
                case "rock":
                    return "img/Type-Rock.png";
                case "bug":
                    return "img/Type-Bug.png";
                case "ghost":
                    return "img/Type-Ghost.png";
                case "steel":
                    return "img/Type-Steel.png";
                case "fire":
                    return "img/Type-Fire.png";
                case "water":
                    return "img/Type-Water.png";
                case "grass":
                    return "img/Type-Grass.png";
                case "electric":
                    return "img/Type-Electric.png";
                case "psychic":
                    return "img/Type-Psychic.png";
                case "ice":
                    return "img/Type-Ice.png";
                case "dragon":
                    return "img/Type-Dragon.png";
                case "dark":
                    return "img/Type-Dark.png";
                case "fairy":
                    return "img/Type-Fairy.png";
                default:
                    return "";
            }
        }

        public static string DisabledTypeImgFromName(string typeName)
        {
            switch (typeName)
            {
                case "normal":
                    return "img/Type-Normal-Disabled.png";
                case "fighting":
                    return "img/Type-Fighting-Disabled.png";
                case "flying":
                    return "img/Type-Flying-Disabled.png";
                case "poison":
                    return "img/Type-Poison-Disabled.png";
                case "ground":
                    return "img/Type-Ground-Disabled.png";
                case "rock":
                    return "img/Type-Rock-Disabled.png";
                case "bug":
                    return "img/Type-Bug-Disabled.png";
                case "ghost":
                    return "img/Type-Ghost-Disabled.png";
                case "steel":
                    return "img/Type-Steel-Disabled.png";
                case "fire":
                    return "img/Type-Fire-Disabled.png";
                case "water":
                    return "img/Type-Water-Disabled.png";
                case "grass":
                    return "img/Type-Grass-Disabled.png";
                case "electric":
                    return "img/Type-Electric-Disabled.png";
                case "psychic":
                    return "img/Type-Psychic-Disabled.png";
                case "ice":
                    return "img/Type-Ice-Disabled.png";
                case "dragon":
                    return "img/Type-Dragon-Disabled.png";
                case "dark":
                    return "img/Type-Dark-Disabled.png";
                case "fairy":
                    return "img/Type-Fairy-Disabled.png";
                default:
                    return "";
            }
        }
    }
}
