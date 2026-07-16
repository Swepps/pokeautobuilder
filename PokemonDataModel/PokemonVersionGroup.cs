namespace PokemonDataModel
{
    // one entry per mainline game (or pair/group of games) exposed as a searchable
    // Pokédex-by-version-group location, e.g. in MainLayout's SearchLocations setup
    public record PokemonVersionGroup(string DisplayName, string ApiName, int Id)
    {
        public string ApiUrl => $"https://pokeapi.co/api/v2/version-group/{Id}/";

        public static readonly IReadOnlyList<PokemonVersionGroup> All =
        [
            new("Red & Blue", "red-blue", 1),
            new("Yellow", "yellow", 2),
            new("Gold & Silver", "gold-silver", 3),
            new("Crystal", "crystal", 4),
            new("Ruby & Sapphire", "ruby-sapphire", 5),
            new("Emerald", "emerald", 6),
            new("FireRed & LeafGreen", "firered-leafgreen", 7),
            new("Diamond & Pearl", "diamond-pearl", 8),
            new("Platinum", "platinum", 9),
            new("HeartGold & SoulSilver", "heartgold-soulsilver", 10),
            new("Black & White", "black-white", 11),
            new("Colosseum", "colosseum", 12),
            new("Black 2 & White 2", "black-2-white-2", 14),
            new("X & Y", "x-y", 15),
            new("Omega Ruby & Alpha Sapphire", "omega-ruby-alpha-sapphire", 16),
            new("Sun & Moon", "sun-moon", 17),
            new("Ultra Sun & Ultra Moon", "ultra-sun-ultra-moon", 18),
            new("Let's Go, Pikachu! & Let's Go, Eevee!", "lets-go-pikachu-lets-go-eevee", 19),
            new("Sword & Shield", "sword-shield", 20),
            new("The Isle of Armor", "the-isle-of-armor", 21),
            new("The Crown Tundra", "the-crown-tundra", 22),
            new("Brilliant Diamond and Shining Pearl", "brilliant-diamond-and-shining-pearl", 23),
            new("Legends: Arceus", "legends-arceus", 24),
            new("Scarlet & Violet", "scarlet-violet", 25),
            new("The Teal Mask", "the-teal-mask", 26),
            new("The Indigo Disk", "the-indigo-disk", 27),
            new("Legends: Z-A", "legends-za", 30),
        ];
    }
}
