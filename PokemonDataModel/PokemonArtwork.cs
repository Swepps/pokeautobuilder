namespace PokemonDataModel
{
    // Large-card artwork choices - see PokemonArtwork for how each resolves to actual sprite URLs.
    public enum LargeArtworkStyle
    {
        OfficialArtwork,
        ShowdownAnimated,
        PokemonHome,
        GenerationMatched,
        GenerationMatchedAnimated,
    }

    // Mini-card (box/team roster) artwork choices.
    public enum MiniArtworkStyle
    {
        GenerationMatched,
        BoxIcon,
    }

    // Resolves which sprite/artwork URL(s) to show for a Pokemon, for a chosen style and (where the
    // style cares) a ruleset generation. PokeAPI's actual coverage is uneven - a Mega, a very recent
    // Pokemon, or an early-generation sprite for something introduced later can all be missing at
    // the "ideal" level - so every style is a best-to-worst ordered chain of layers rather than a
    // single URL, ending in a layer that's always present (root FrontDefault) so a chain never comes
    // up empty.
    //
    // ResolveGroupLevel additionally picks one shared layer for a whole roster (a box, a team) so
    // members don't render at visibly different art eras just because one of them happens to be
    // missing the top layer - see PokeAutobuilder's card components for how that's wired in.
    public static class PokemonArtwork
    {
        private delegate string? UrlGetter(SmartPokemon pokemon, int? generation);

        private static string? OfficialArtworkUrl(SmartPokemon p, int? generation) =>
            p.Sprites.Other?.OfficialArtwork?.FrontDefault;

        private static string? HomeUrl(SmartPokemon p, int? generation) => p.Sprites.Other?.Home?.FrontDefault;

        private static string? ShowdownUrl(SmartPokemon p, int? generation) =>
            p.Sprites.Other?.Showdown?.FrontDefault;

        private static string? RootFrontDefaultUrl(SmartPokemon p, int? generation) => p.Sprites.FrontDefault;

        // Gen 8 is the newest generation PokeApiNet 4.1.0 maps an icon set for at all (it doesn't map
        // Gen 5's, and PokeAPI has none for Gen 9) - covers every species through Gen 8 plus most alt
        // forms, but nothing from Gen 9 onward.
        private static string? BoxIconUrl(SmartPokemon p, int? generation) =>
            p.Sprites.Versions?.GenerationVIII?.Icons?.FrontDefault;

        // Gen 6 onward renders in 3D and its flat sprite renders look poor blown up, while Gen 5's
        // Black/White set is both the sharpest 2D sprite generation and - per the community sprite
        // repo PokeAPI serves images from - was extended to cover every Pokemon introduced since, not
        // just the ones that actually existed in Gen 5. So it stands in for "generation-matched" for
        // every ruleset Gen 5 and up, and for Unrestricted (nothing specific to match).
        private static string? GenerationMatchedUrl(SmartPokemon p, int? generation) =>
            generation switch
            {
                1 => p.Sprites.Versions?.GenerationI?.Yellow?.FrontTransparent,
                2 => p.Sprites.Versions?.GenerationII?.Crystal?.FrontTransparent,
                3 => p.Sprites.Versions?.GenerationIII?.Emerald?.FrontDefault,
                4 => p.Sprites.Versions?.GenerationIV?.Platinum?.FrontDefault,
                _ => p.Sprites.Versions?.GenerationV?.BlackWhite?.FrontDefault,
            };

        // Only Gen 5's Black/White set has an animated variant, so this layer simply has nothing to
        // offer outside Gen 5+ - the fallback chain moves on to the static generation-matched sprite
        // for those rulesets with no special-casing needed.
        private static string? GenerationMatchedAnimatedUrl(SmartPokemon p, int? generation) =>
            HasAnimatedGenerationMatch(generation)
                ? p.Sprites.Versions?.GenerationV?.BlackWhite?.Animated?.FrontDefault
                : null;

        // Whether GenerationMatchedAnimated has anything to offer at all for this generation - lets
        // a caller (e.g. the Preferences UI) hint that the option quietly renders as the static
        // sprite outside Gen 5+ rather than an animated one.
        public static bool HasAnimatedGenerationMatch(int? generation) => generation is null or >= 5;

        // Ordered best-to-worst layers per style. The last layer of every chain must never return
        // null for a real Pokemon (root FrontDefault is PokeAPI's universal guarantee).
        private static readonly IReadOnlyDictionary<LargeArtworkStyle, IReadOnlyList<UrlGetter>> LargeLayers =
            new Dictionary<LargeArtworkStyle, IReadOnlyList<UrlGetter>>
            {
                [LargeArtworkStyle.OfficialArtwork] = [OfficialArtworkUrl, HomeUrl, RootFrontDefaultUrl],
                [LargeArtworkStyle.ShowdownAnimated] = [ShowdownUrl, OfficialArtworkUrl, RootFrontDefaultUrl],
                [LargeArtworkStyle.PokemonHome] = [HomeUrl, OfficialArtworkUrl, RootFrontDefaultUrl],
                [LargeArtworkStyle.GenerationMatched] = [GenerationMatchedUrl, OfficialArtworkUrl, RootFrontDefaultUrl],
                [LargeArtworkStyle.GenerationMatchedAnimated] =
                    [GenerationMatchedAnimatedUrl, GenerationMatchedUrl, OfficialArtworkUrl, RootFrontDefaultUrl],
            };

        private static readonly IReadOnlyDictionary<MiniArtworkStyle, IReadOnlyList<UrlGetter>> MiniLayers =
            new Dictionary<MiniArtworkStyle, IReadOnlyList<UrlGetter>>
            {
                [MiniArtworkStyle.GenerationMatched] = [GenerationMatchedUrl, OfficialArtworkUrl, RootFrontDefaultUrl],
                [MiniArtworkStyle.BoxIcon] = [BoxIconUrl, GenerationMatchedUrl, OfficialArtworkUrl, RootFrontDefaultUrl],
            };

        // Large-card sprite styles are pixel art blown up to a much bigger card, so they need
        // crisp/pixelated upscaling; official artwork/Home/Showdown are already high-resolution and
        // shouldn't be. Mini-card styles are always small pixel art regardless of which one is picked.
        public static bool IsPixelArt(LargeArtworkStyle style) =>
            style is LargeArtworkStyle.GenerationMatched or LargeArtworkStyle.GenerationMatchedAnimated;

        // The layer index every member of `roster` can satisfy for `style` - the smallest index
        // where every Pokemon has a non-null URL, so a group renders at one consistent visual era
        // instead of some members silently falling back further than others. Falls through to the
        // chain's last (always-present) layer if no earlier one is universal. An empty roster
        // resolves to the first (best) layer, since there's nothing to constrain it down.
        public static int ResolveGroupLevel(
            IEnumerable<SmartPokemon> roster,
            int? generation,
            LargeArtworkStyle style
        ) => ResolveGroupLevel(roster, generation, LargeLayers[style]);

        public static int ResolveGroupLevel(
            IEnumerable<SmartPokemon> roster,
            int? generation,
            MiniArtworkStyle style
        ) => ResolveGroupLevel(roster, generation, MiniLayers[style]);

        private static int ResolveGroupLevel(
            IEnumerable<SmartPokemon> roster,
            int? generation,
            IReadOnlyList<UrlGetter> layers
        )
        {
            List<SmartPokemon> members = roster.ToList();
            for (int i = 0; i < layers.Count - 1; i++)
            {
                if (members.All(p => layers[i](p, generation) is not null))
                    return i;
            }
            return layers.Count - 1;
        }

        // The candidate URLs for one Pokemon, starting at `fromLevel` (typically a roster's resolved
        // group level) and continuing down through the rest of the chain - each card still gets its
        // own fallback beneath the shared level as a safety net for e.g. an image that 404s despite
        // PokeAPI's data claiming it exists.
        public static IReadOnlyList<string> GetCandidates(
            SmartPokemon pokemon,
            int? generation,
            LargeArtworkStyle style,
            int fromLevel = 0
        ) => GetCandidates(pokemon, generation, LargeLayers[style], fromLevel);

        public static IReadOnlyList<string> GetCandidates(
            SmartPokemon pokemon,
            int? generation,
            MiniArtworkStyle style,
            int fromLevel = 0
        ) => GetCandidates(pokemon, generation, MiniLayers[style], fromLevel);

        private static IReadOnlyList<string> GetCandidates(
            SmartPokemon pokemon,
            int? generation,
            IReadOnlyList<UrlGetter> layers,
            int fromLevel
        )
        {
            List<string> urls = [];
            for (int i = fromLevel; i < layers.Count; i++)
            {
                string? url = layers[i](pokemon, generation);
                if (url is not null)
                    urls.Add(url);
            }
            return urls;
        }
    }
}
