using PokeApiNet;
using PokemonDataModel;
using Xunit;

namespace PokeAutobuilderTests
{
    // Tests for PokemonArtwork's fallback-chain resolution and group-level ("whole box/team renders
    // at one consistent art era") logic. All offline - sprite URLs are hand-built via
    // TestFixtures.MakeSprites rather than fetched.
    public class PokemonArtworkTests
    {
        private static SmartPokemon MakeWithSprites(string name, PokemonSprites sprites, int id = 0) =>
            TestFixtures.MakeScoringPokemon(name, id: id, sprites: sprites);

        private static PokemonSprites FullSprites() =>
            new()
            {
                FrontDefault = "root",
                Other = new PokemonSprites.OtherSprites
                {
                    OfficialArtwork = new PokemonSprites.OtherSprites.OfficialArtworkSprites
                    {
                        FrontDefault = "official",
                    },
                    Home = new PokemonSprites.OtherSprites.HomeSprites { FrontDefault = "home" },
                    Showdown = new PokemonSprites.OtherSprites.ShowdownSprites { FrontDefault = "showdown" },
                },
                Versions = new PokemonSprites.VersionSprites
                {
                    GenerationI = new PokemonSprites.VersionSprites.GenerationISprites
                    {
                        Yellow = new PokemonSprites.VersionSprites.GenerationISprites.YellowSprites
                        {
                            FrontDefault = "gen1-opaque",
                            FrontTransparent = "gen1",
                        },
                    },
                    GenerationV = new PokemonSprites.VersionSprites.GenerationVSprites
                    {
                        BlackWhite = new PokemonSprites.VersionSprites.GenerationVSprites.BlackWhiteSprites
                        {
                            FrontDefault = "gen5",
                            Animated = new PokemonSprites
                                .VersionSprites
                                .GenerationVSprites
                                .BlackWhiteSprites
                                .AnimatedSprites
                            {
                                FrontDefault = "gen5-animated",
                            },
                        },
                    },
                    GenerationVIII = new PokemonSprites.VersionSprites.GenerationVIIISprites
                    {
                        Icons = new PokemonSprites.VersionSprites.GenerationVIIISprites.IconsSprites
                        {
                            FrontDefault = "gen8-icon",
                        },
                    },
                },
            };

        [Fact]
        public void GetCandidates_RewritesGitHubRawUrlsToJsDelivr()
        {
            SmartPokemon p = MakeWithSprites(
                "real-url",
                new PokemonSprites
                {
                    Other = new PokemonSprites.OtherSprites
                    {
                        OfficialArtwork = new PokemonSprites.OtherSprites.OfficialArtworkSprites
                        {
                            FrontDefault =
                                "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/other/official-artwork/25.png",
                        },
                    },
                }
            );

            var candidates = PokemonArtwork.GetCandidates(p, null, LargeArtworkStyle.OfficialArtwork);

            Assert.Equal(
                "https://cdn.jsdelivr.net/gh/PokeAPI/sprites@master/sprites/pokemon/other/official-artwork/25.png",
                candidates[0]
            );
        }

        [Fact]
        public void GetCandidates_LeavesNonGitHubUrlsUnchanged()
        {
            SmartPokemon p = MakeWithSprites(
                "other-host",
                new PokemonSprites
                {
                    Other = new PokemonSprites.OtherSprites
                    {
                        OfficialArtwork = new PokemonSprites.OtherSprites.OfficialArtworkSprites
                        {
                            FrontDefault = "https://example.com/some-other-sprite-host/25.png",
                        },
                    },
                }
            );

            var candidates = PokemonArtwork.GetCandidates(p, null, LargeArtworkStyle.OfficialArtwork);

            Assert.Equal("https://example.com/some-other-sprite-host/25.png", candidates[0]);
        }

        [Fact]
        public void OfficialArtwork_PrefersOfficialArtworkUrl()
        {
            SmartPokemon p = MakeWithSprites("full", FullSprites());

            var candidates = PokemonArtwork.GetCandidates(p, null, LargeArtworkStyle.OfficialArtwork);

            Assert.Equal("official", candidates[0]);
        }

        [Fact]
        public void OfficialArtwork_FallsBackToHomeThenRoot_WhenMissing()
        {
            PokemonSprites sprites = new() { FrontDefault = "root" };
            SmartPokemon p = MakeWithSprites("bare", sprites);

            var candidates = PokemonArtwork.GetCandidates(p, null, LargeArtworkStyle.OfficialArtwork);

            Assert.Equal(["root"], candidates);
        }

        [Fact]
        public void GenerationMatched_UsesYellowForGen1()
        {
            SmartPokemon p = MakeWithSprites("full", FullSprites());

            var candidates = PokemonArtwork.GetCandidates(p, 1, LargeArtworkStyle.GenerationMatched);

            Assert.Equal("gen1", candidates[0]);
        }

        [Fact]
        public void GenerationMatched_UsesBlackWhiteForGen5AndUp()
        {
            SmartPokemon p = MakeWithSprites("full", FullSprites());

            Assert.Equal("gen5", PokemonArtwork.GetCandidates(p, 5, LargeArtworkStyle.GenerationMatched)[0]);
            Assert.Equal("gen5", PokemonArtwork.GetCandidates(p, 8, LargeArtworkStyle.GenerationMatched)[0]);
        }

        [Fact]
        public void GenerationMatched_UsesBlackWhiteForUnrestricted()
        {
            SmartPokemon p = MakeWithSprites("full", FullSprites());

            Assert.Equal("gen5", PokemonArtwork.GetCandidates(p, null, LargeArtworkStyle.GenerationMatched)[0]);
        }

        [Fact]
        public void GenerationMatched_FallsBackToOfficialArtwork_WhenGenerationSpriteMissing()
        {
            // no Versions at all - e.g. a Pokemon whose sprites just don't cover Gen 1
            SmartPokemon p = MakeWithSprites(
                "no-gen1",
                new PokemonSprites
                {
                    FrontDefault = "root",
                    Other = new PokemonSprites.OtherSprites
                    {
                        OfficialArtwork = new PokemonSprites.OtherSprites.OfficialArtworkSprites
                        {
                            FrontDefault = "official",
                        },
                    },
                }
            );

            var candidates = PokemonArtwork.GetCandidates(p, 1, LargeArtworkStyle.GenerationMatched);

            Assert.Equal(["official", "root"], candidates);
        }

        [Fact]
        public void GenerationMatchedAnimated_FallsBackToStaticSprite_ForGen1Through4()
        {
            SmartPokemon p = MakeWithSprites("full", FullSprites());

            // Gen 1 has no animated Black/White sprite at all - the animated layer has nothing to
            // offer, so the chain moves straight to the (Gen 1-matched) static sprite
            var candidates = PokemonArtwork.GetCandidates(
                p,
                1,
                LargeArtworkStyle.GenerationMatchedAnimated
            );

            Assert.Equal(["gen1", "official", "root"], candidates);
        }

        [Fact]
        public void GenerationMatchedAnimated_UsesAnimatedSprite_ForGen5AndUp()
        {
            SmartPokemon p = MakeWithSprites("full", FullSprites());

            var candidates = PokemonArtwork.GetCandidates(
                p,
                5,
                LargeArtworkStyle.GenerationMatchedAnimated
            );

            Assert.Equal("gen5-animated", candidates[0]);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(4, false)]
        [InlineData(5, true)]
        [InlineData(9, true)]
        public void HasAnimatedGenerationMatch_MatchesGen5PlusAndUnrestricted(int? generation, bool expected)
        {
            Assert.Equal(expected, PokemonArtwork.HasAnimatedGenerationMatch(generation));
        }

        [Fact]
        public void BoxIcon_FallsBackToGenerationMatchedThenOfficialArtwork()
        {
            SmartPokemon withIcon = MakeWithSprites("has-icon", FullSprites());
            SmartPokemon withoutIcon = MakeWithSprites(
                "no-icon",
                new PokemonSprites
                {
                    FrontDefault = "root",
                    Other = new PokemonSprites.OtherSprites
                    {
                        OfficialArtwork = new PokemonSprites.OtherSprites.OfficialArtworkSprites
                        {
                            FrontDefault = "official",
                        },
                    },
                    Versions = new PokemonSprites.VersionSprites
                    {
                        GenerationV = new PokemonSprites.VersionSprites.GenerationVSprites
                        {
                            BlackWhite = new PokemonSprites.VersionSprites.GenerationVSprites.BlackWhiteSprites
                            {
                                FrontDefault = "gen5",
                            },
                        },
                    },
                }
            );

            Assert.Equal(
                "gen8-icon",
                PokemonArtwork.GetCandidates(withIcon, null, MiniArtworkStyle.BoxIcon)[0]
            );
            Assert.Equal(
                "gen5",
                PokemonArtwork.GetCandidates(withoutIcon, null, MiniArtworkStyle.BoxIcon)[0]
            );
        }

        [Fact]
        public void ResolveGroupLevel_PicksTheMostDegradedLevelAcrossTheRoster()
        {
            SmartPokemon hasAnimated = MakeWithSprites("has-animated", FullSprites());
            SmartPokemon staticOnly = MakeWithSprites(
                "static-only",
                new PokemonSprites
                {
                    FrontDefault = "root",
                    Other = new PokemonSprites.OtherSprites
                    {
                        OfficialArtwork = new PokemonSprites.OtherSprites.OfficialArtworkSprites
                        {
                            FrontDefault = "official",
                        },
                    },
                    Versions = new PokemonSprites.VersionSprites
                    {
                        GenerationV = new PokemonSprites.VersionSprites.GenerationVSprites
                        {
                            BlackWhite = new PokemonSprites.VersionSprites.GenerationVSprites.BlackWhiteSprites
                            {
                                FrontDefault = "gen5-static-only",
                            },
                        },
                    },
                }
            );

            int soloLevel = PokemonArtwork.ResolveGroupLevel(
                [hasAnimated],
                5,
                LargeArtworkStyle.GenerationMatchedAnimated
            );
            int groupLevel = PokemonArtwork.ResolveGroupLevel(
                [hasAnimated, staticOnly],
                5,
                LargeArtworkStyle.GenerationMatchedAnimated
            );

            // alone, hasAnimated would render animated (level 0) - but paired with a Pokemon that
            // only has the static sprite, the whole group downgrades to level 1 so neither renders
            // at a visibly different era from the other
            Assert.Equal(0, soloLevel);
            Assert.Equal(1, groupLevel);

            IReadOnlyList<string> hasAnimatedAtGroupLevel = PokemonArtwork.GetCandidates(
                hasAnimated,
                5,
                LargeArtworkStyle.GenerationMatchedAnimated,
                groupLevel
            );
            Assert.Equal("gen5", hasAnimatedAtGroupLevel[0]);
        }

        [Fact]
        public void ResolveGroupLevel_EmptyRoster_ResolvesToTheBestLevel()
        {
            int level = PokemonArtwork.ResolveGroupLevel([], 5, LargeArtworkStyle.GenerationMatchedAnimated);

            Assert.Equal(0, level);
        }
    }
}
