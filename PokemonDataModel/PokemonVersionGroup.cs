namespace PokemonDataModel
{
    // one entry per mainline game (or pair/group of games) exposed as a searchable
    // Pokédex-by-version-group location, e.g. in MainLayout's SearchLocations setup
    //
    // Generation is the PokeAPI version-group's own `generation` classification (confirmed live via
    // GET /api/v2/version-group/{Id}/), not necessarily the generation of the original game a
    // remake/spin-off is set in - e.g. Brilliant Diamond & Shining Pearl and Legends: Arceus are
    // Gen-4-era content but PokeAPI classifies both as generation-viii.
    public record PokemonVersionGroup(string DisplayName, string ApiName, int Id, int Generation)
    {
        public string ApiUrl => $"https://pokeapi.co/api/v2/version-group/{Id}/";

        public static readonly IReadOnlyList<PokemonVersionGroup> All =
        [
            new("Red & Blue", "red-blue", 1, 1),
            new("Yellow", "yellow", 2, 1),
            new("Gold & Silver", "gold-silver", 3, 2),
            new("Crystal", "crystal", 4, 2),
            new("Ruby & Sapphire", "ruby-sapphire", 5, 3),
            new("Emerald", "emerald", 6, 3),
            new("FireRed & LeafGreen", "firered-leafgreen", 7, 3),
            new("Diamond & Pearl", "diamond-pearl", 8, 4),
            new("Platinum", "platinum", 9, 4),
            new("HeartGold & SoulSilver", "heartgold-soulsilver", 10, 4),
            new("Black & White", "black-white", 11, 5),
            new("Colosseum", "colosseum", 12, 3),
            new("Black 2 & White 2", "black-2-white-2", 14, 5),
            new("X & Y", "x-y", 15, 6),
            new("Omega Ruby & Alpha Sapphire", "omega-ruby-alpha-sapphire", 16, 6),
            new("Sun & Moon", "sun-moon", 17, 7),
            new("Ultra Sun & Ultra Moon", "ultra-sun-ultra-moon", 18, 7),
            new("Let's Go, Pikachu! & Let's Go, Eevee!", "lets-go-pikachu-lets-go-eevee", 19, 7),
            new("Sword & Shield", "sword-shield", 20, 8),
            new("The Isle of Armor", "the-isle-of-armor", 21, 8),
            new("The Crown Tundra", "the-crown-tundra", 22, 8),
            new("Brilliant Diamond and Shining Pearl", "brilliant-diamond-and-shining-pearl", 23, 8),
            new("Legends: Arceus", "legends-arceus", 24, 8),
            new("Scarlet & Violet", "scarlet-violet", 25, 9),
            new("The Teal Mask", "the-teal-mask", 26, 9),
            new("The Indigo Disk", "the-indigo-disk", 27, 9),
            new("Legends: Z-A", "legends-za", 30, 9),
        ];
    }
}
