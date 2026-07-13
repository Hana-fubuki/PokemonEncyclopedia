using System.Text.Json;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using PokeApiNet;
using PokemonEncyclopedia.Web;
using PokemonEncyclopedia.Web.Components;
using PokemonEncyclopedia.Web.Components.Layout;
using PokemonEncyclopedia.Web.Components.Pages;
using PokeType = PokeApiNet.Type;

namespace PokemonEncyclopedia.Tests.Unit;

public class WebCoverageTests
{
    [Fact]
    public async Task PokemonThemeState_InitializesAndTogglesWithJsInterop()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Setup<string>("themeInterop.getTheme").SetResult("dark");
        ctx.JSInterop.SetupVoid("themeInterop.setTheme", "light");

        var state = new PokemonThemeState();
        await state.InitializeAsync(ctx.JSInterop.JSRuntime);

        state.IsDarkMode.Should().BeTrue();

        await state.Toggle();

        state.IsDarkMode.Should().BeFalse();
    }

    [Fact]
    public void ThemeToggle_RendersAndTogglesIcon()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new PokemonThemeState());

        var cut = ctx.Render<ThemeToggle>();

        cut.Markup.Should().Contain("☀️");
        cut.Find("button").Click();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("🌙"));
    }

    [Fact]
    public void Counter_IncrementsOnClick()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<Counter>();

        cut.Markup.Should().Contain("Current count: 0");
        cut.Find("button").Click();
        cut.Markup.Should().Contain("Current count: 1");
    }

    [Fact]
    public void NavMenu_RendersPokedexLink()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<NavMenu>();

        cut.Markup.Should().Contain("Pokédex");
    }

    [Fact]
    public void MainLayout_UsesThemeFromJsInterop()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Setup<string>("themeInterop.getTheme").SetResult("dark");
        ctx.Services.AddSingleton(new PokemonThemeState());

        var cut = ctx.Render<MainLayout>();

        cut.Markup.Should().Contain("theme-dark");
        cut.Markup.Should().Contain("Poképedia");
    }

    [Fact]
    public async Task Home_RendersPokemonCardsAndFiltersSearch()
    {
        using var ctx = new BunitContext();
        var handler = new RouteHttpMessageHandler(
            ("http://localhost/api/PokeApi/details", JsonSerializer.Serialize(new[]
            {
                CreatePokemon(1, "bulbasaur"),
                CreatePokemon(25, "pikachu")
            })),
            ("http://localhost/api/PokeApi/legendary", """["mewtwo"]"""),
            ("http://localhost/api/PokeApi/species/bulbasaur", JsonSerializer.Serialize(CreateSpecies("bulbasaur"))),
            ("http://localhost/api/PokeApi/species/pikachu", JsonSerializer.Serialize(CreateSpecies("pikachu")))
        );
        ctx.Services.AddSingleton(CreatePokemonApiClient(handler));
        ctx.Services.AddSingleton(new PokemonFilterState());

        var cut = ctx.Render<Home>();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("bulbasaur"));
        cut.Markup.Should().Contain("pikachu");

        cut.Find("input.search-input").Input("pika");
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("pikachu"));
        cut.Markup.Should().NotContain("bulbasaur");
    }

    [Fact]
    public void PokemonEvolutionNode_RendersChildrenAndNavigates()
    {
        using var ctx = new BunitContext();
        var nav = ctx.Services.GetRequiredService<NavigationManager>() as BunitNavigationManager;
        var spriteMap = new Dictionary<string, string?>
        {
            ["bulbasaur"] = "sprite.png",
            ["ivysaur"] = null
        };

        var chain = new ChainLink
        {
            Species = new NamedApiResource<PokemonSpecies> { Name = "bulbasaur" },
            IsBaby = true,
            EvolutionDetails =
            [
                new EvolutionDetail
                {
                    MinLevel = 16,
                    Trigger = new NamedApiResource<EvolutionTrigger> { Name = "level-up" }
                }
            ],
            EvolvesTo =
            [
                new ChainLink
                {
                    Species = new NamedApiResource<PokemonSpecies> { Name = "ivysaur" },
                    EvolutionDetails = [],
                    EvolvesTo = []
                }
            ]
        };

        var cut = ctx.Render<PokemonEvolutionNode>(parameters => parameters
            .Add(p => p.Node, chain)
            .Add(p => p.SpriteMap, spriteMap));

        cut.Markup.Should().Contain("Baby Form");
        cut.Markup.Should().Contain("Lv. 16");
        cut.Markup.Should().Contain("ivysaur");

        cut.Find(".evolution-card").Click();
        nav!.Uri.Should().EndWith("/pokemon/bulbasaur");
    }

    [Fact]
    public async Task PokemonDetail_RendersInteractiveDataAndNavigates()
    {
        using var ctx = new BunitContext();
        var client = CreatePokemonApiClient(new RouteHttpMessageHandler(
            ("http://localhost/api/PokeApi/pokemon/bulbasaur", JsonSerializer.Serialize(CreateDetailPokemon(
                1,
                "bulbasaur",
                "https://img.example/bulbasaur.png",
                "bulbasaur",
                [CreatePokemonType("grass"), CreatePokemonType("poison")],
                [
                    CreatePokemonAbility("chlorophyll", true, null),
                    CreatePokemonAbility("overgrow", false, "Boosts grass-type moves.")
                ],
                [
                    CreatePokemonStat("hp", 0),
                    CreatePokemonStat("attack", 49),
                    CreatePokemonStat("defense", 49),
                    CreatePokemonStat("special-attack", 65),
                    CreatePokemonStat("special-defense", 65),
                    CreatePokemonStat("speed", 300),
                    CreatePokemonStat("mystery", 12)
                ],
                [
                    CreatePokemonMove("tackle", [("level-up", 1), ("machine", 1)]),
                    CreatePokemonMove("growl", [("tutor", 0)])
                ]))),
            ("http://localhost/api/PokeApi/species/bulbasaur", JsonSerializer.Serialize(CreateDetailSpecies(
                "bulbasaur",
                "A strange seed\nwith a sweet scent\f.",
                "https://pokeapi.co/api/v2/evolution-chain/1/",
                [
                    CreateSpeciesVariety(1, "bulbasaur"),
                    CreateSpeciesVariety(2, "ivysaur")
                ]))),
            ("http://localhost/api/PokeApi/details", JsonSerializer.Serialize(new[]
            {
                CreateDetailPokemon(1, "bulbasaur", "https://img.example/bulbasaur.png", "bulbasaur"),
                CreateDetailPokemon(99, "mysterymon", null, null)
            })),
            ("http://localhost/api/PokeApi/legendary", """[]"""),
            ("http://localhost/api/PokeApi/evolution-chain/1", JsonSerializer.Serialize(CreateEvolutionChain())),
            ("http://localhost/api/PokeApi/ability/chlorophyll",
                JsonSerializer.Serialize(CreateAbility("chlorophyll", null, null))),
            ("http://localhost/api/PokeApi/ability/overgrow",
                JsonSerializer.Serialize(CreateAbility("overgrow", "Boosts grass-type moves.", "generation-iii"))),
            ("http://localhost/api/PokeApi/move/tackle",
                JsonSerializer.Serialize(CreateMove("tackle", "normal", "physical", 40, 100, 35,
                    "A physical attack."))),
            ("http://localhost/api/PokeApi/move/growl",
                JsonSerializer.Serialize(CreateMove("growl", null, null, null, null, 40, null))),
            ("https://pokeapi.co/api/v2/pokemon/1/",
                JsonSerializer.Serialize(CreateDetailPokemon(1, "bulbasaur", "https://img.example/bulbasaur.png",
                    "bulbasaur"))),
            ("https://pokeapi.co/api/v2/pokemon/2/",
                JsonSerializer.Serialize(
                    CreateDetailPokemon(2, "ivysaur", "https://img.example/ivysaur.png", "ivysaur")))
        ));
        ctx.Services.AddSingleton(client);

        var nav = ctx.Services.GetRequiredService<NavigationManager>() as BunitNavigationManager;
        var cut = ctx.Render<PokemonDetail>(parameters => parameters.Add(p => p.Name, "bulbasaur"));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Trade for Ditto"));
        cut.Markup.Should().Contain("https://img.example/bulbasaur.png");
        cut.Markup.Should().Contain("A strange seed");
        cut.Markup.Should().Contain("sweet scent");
        cut.Markup.Should().Contain("Baby Form");
        cut.Markup.Should().Contain("Lv. 16");
        cut.Markup.Should().Contain("Trade for Ditto");
        cut.Markup.Should().Contain("Unknown requirement");
        cut.Markup.Should().Contain("Hidden");
        cut.Markup.Should().Contain("tackle");
        cut.Markup.Should().Contain("growl");
        cut.Markup.Should().Contain("Level Up");
        cut.Markup.Should().Contain("Tutor");

        cut.Find(".back-link").Click();
        nav!.Uri.Should().EndWith("/");

        cut.FindAll(".ability-pill-button")[1].Click();
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Boosts grass-type moves."));

        cut.FindAll(".variety-card")[1].Click();
        nav.Uri.Should().EndWith("/pokemon/ivysaur");
    }

    [Fact]
    public async Task PokemonDetail_RendersFallbackStates()
    {
        using var ctx = new BunitContext();
        var handler = new RouteHttpMessageHandler(
            ("http://localhost/api/PokeApi/pokemon/missingno", JsonSerializer.Serialize(CreateDetailPokemon(
                0,
                "missingno",
                null,
                "missingno",
                [],
                [],
                [
                    CreatePokemonStat("hp", 255)
                ],
                []))),
            ("http://localhost/api/PokeApi/species/missingno", JsonSerializer.Serialize(CreateDetailSpecies(
                "missingno",
                null,
                null,
                []))),
            ("http://localhost/api/PokeApi/details", "[]"),
            ("http://localhost/api/PokeApi/legendary", """[]"""),
            ("http://localhost/api/PokeApi/ability/limber",
                JsonSerializer.Serialize(CreateAbility("limber", null, null)))
        );
        ctx.Services.AddSingleton(CreatePokemonApiClient(handler));

        var cut = ctx.Render<PokemonDetail>(parameters => parameters.Add(p => p.Name, "missingno"));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("missingno"));
        cut.Markup.Should().Contain("Loading sprite...");
        cut.Markup.Should().Contain("No Pokédex flavor text available.");
        cut.Markup.Should().Contain("No move data found.");
        cut.Markup.Should().Contain("No varieties available.");
        cut.Markup.Should().Contain("Loading evolution data...");
    }

    [Fact]
    public void Home_TogglesAllFilters()
    {
        using var ctx = new BunitContext();
        var filterState = new PokemonFilterState();
        ctx.Services.AddSingleton(filterState);

        filterState.ToggleGeneration(1);
        filterState.ToggleType("grass");
        filterState.IncludeLegendary = false;
        filterState.IncludeMythical = false;

        filterState.SelectedGenerations.Should().NotContain(1);
        filterState.SelectedTypes.Should().Contain("grass");
        filterState.IncludeLegendary.Should().BeFalse();
        filterState.IncludeMythical.Should().BeFalse();

        filterState.ClearFilters();
        filterState.SelectedGenerations.Should().Contain(1);
        filterState.SelectedTypes.Should().BeEmpty();
        filterState.IncludeLegendary.Should().BeTrue();
        filterState.IncludeMythical.Should().BeTrue();
    }

    private static PokemonApiClient CreatePokemonApiClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
        return new PokemonApiClient(httpClient, new MemoryCache(new MemoryCacheOptions()));
    }

    private static Pokemon CreatePokemon(int id, string name)
    {
        return new Pokemon
        {
            Id = id,
            Name = name,
            Species = new NamedApiResource<PokemonSpecies>
            {
                Name = name,
                Url = $"https://pokeapi.co/api/v2/pokemon-species/{id}/"
            },
            Types =
            [
                new PokemonType
                {
                    Type = new NamedApiResource<PokeType> { Name = "grass" }
                }
            ],
            Abilities =
            [
                new PokemonAbility
                {
                    Ability = new NamedApiResource<Ability> { Name = "overgrow" },
                    IsHidden = false
                }
            ],
            Stats =
            [
                new PokemonStat
                {
                    Stat = new NamedApiResource<Stat> { Name = "speed" },
                    BaseStat = 45
                }
            ],
            Moves =
            [
                new PokemonMove
                {
                    Move = new NamedApiResource<Move> { Name = "tackle" },
                    VersionGroupDetails =
                    [
                        new PokemonMoveVersion
                        {
                            LevelLearnedAt = 1,
                            MoveLearnMethod = new NamedApiResource<MoveLearnMethod> { Name = "level-up" }
                        }
                    ]
                }
            ]
        };
    }

    private static Pokemon CreateDetailPokemon(
        int id,
        string name,
        string? spriteUrl,
        string? speciesName,
        IReadOnlyList<PokemonType>? types = null,
        IReadOnlyList<PokemonAbility>? abilities = null,
        IReadOnlyList<PokemonStat>? stats = null,
        IReadOnlyList<PokemonMove>? moves = null)
    {
        NamedApiResource<PokemonSpecies>? species = null;
        if (speciesName is not null)
            species = new NamedApiResource<PokemonSpecies>
            {
                Name = speciesName,
                Url = $"https://pokeapi.co/api/v2/pokemon-species/{id}/"
            };

        return new Pokemon
        {
            Id = id,
            Name = name,
            BaseExperience = 64,
            Height = 7,
            Weight = 69,
            Sprites = new PokemonSprites
            {
                FrontDefault = spriteUrl!
            },
            Species = species!,
            Types = types?.ToList() ?? [],
            Abilities = abilities?.ToList() ?? [],
            Stats = stats?.ToList() ?? [],
            Moves = moves?.ToList() ?? [],
            Forms = [],
            HeldItems = []
        };
    }

    private static PokemonSpecies CreateDetailSpecies(
        string name,
        string? flavorText,
        string? evolutionChainUrl,
        IReadOnlyList<PokemonSpeciesVariety> varieties)
    {
        ApiResource<EvolutionChain>? evolutionChain = null;
        if (evolutionChainUrl is not null)
            evolutionChain = new ApiResource<EvolutionChain>
            {
                Url = evolutionChainUrl
            };

        return new PokemonSpecies
        {
            Id = 1,
            Name = name,
            Generation = new NamedApiResource<Generation> { Name = "generation-i" },
            Habitat = new NamedApiResource<PokemonHabitat> { Name = "forest" },
            GrowthRate = new NamedApiResource<GrowthRate> { Name = "medium-slow" },
            Color = new NamedApiResource<PokemonColor> { Name = "green" },
            CaptureRate = 45,
            BaseHappiness = 50,
            HatchCounter = 20,
            GenderRate = 8,
            FormsSwitchable = true,
            IsLegendary = false,
            IsMythical = false,
            IsBaby = false,
            FlavorTextEntries = flavorText is null
                ? []
                :
                [
                    new PokemonSpeciesFlavorTexts
                    {
                        Language = new NamedApiResource<Language> { Name = "en" },
                        FlavorText = flavorText
                    }
                ],
            EggGroups =
            [
                new NamedApiResource<EggGroup> { Name = "monster" }
            ],
            EvolutionChain = evolutionChain!,
            Varieties = varieties.ToList()
        };
    }

    private static EvolutionChain CreateEvolutionChain()
    {
        return new EvolutionChain
        {
            Id = 1,
            Chain = new ChainLink
            {
                Species = new NamedApiResource<PokemonSpecies> { Name = "bulbasaur" },
                IsBaby = true,
                EvolutionDetails =
                [
                    new EvolutionDetail
                    {
                        Trigger = new NamedApiResource<EvolutionTrigger> { Name = "level-up" },
                        MinLevel = 16
                    },
                    new EvolutionDetail
                    {
                        Trigger = new NamedApiResource<EvolutionTrigger> { Name = "trade" },
                        Item = new NamedApiResource<Item> { Name = "leaf-stone" },
                        HeldItem = new NamedApiResource<Item> { Name = "soothe-bell" },
                        KnownMove = new NamedApiResource<Move> { Name = "double-team" },
                        KnownMoveType = new NamedApiResource<PokeType> { Name = "grass" },
                        TradeSpecies = new NamedApiResource<PokemonSpecies> { Name = "ditto" },
                        PartySpecies = new NamedApiResource<PokemonSpecies> { Name = "pikachu" },
                        PartyType = new NamedApiResource<PokeType> { Name = "fire" },
                        MinHappiness = 220,
                        MinAffection = 2,
                        MinBeauty = 5,
                        TimeOfDay = "day",
                        NeedsOverworldRain = true,
                        TurnUpsideDown = true,
                        Gender = 2
                    },
                    new EvolutionDetail()
                ],
                EvolvesTo =
                [
                    new ChainLink
                    {
                        Species = new NamedApiResource<PokemonSpecies> { Name = "ivysaur" },
                        EvolutionDetails = [],
                        EvolvesTo = []
                    }
                ]
            }
        };
    }

    private static Ability CreateAbility(string name, string? effect, string? generation)
    {
        NamedApiResource<Generation>? generationResource = null;
        if (generation is not null) generationResource = new NamedApiResource<Generation> { Name = generation };

        return new Ability
        {
            Id = 1,
            Name = name,
            Generation = generationResource!,
            EffectEntries = effect is null
                ? []
                :
                [
                    new VerboseEffect
                    {
                        Language = new NamedApiResource<Language> { Name = "en" },
                        Effect = effect,
                        ShortEffect = effect
                    }
                ]
        };
    }

    private static Move CreateMove(
        string name,
        string? type,
        string? damageClass,
        int? power,
        int? accuracy,
        int? pp,
        string? shortEffect)
    {
        NamedApiResource<PokeType>? typeResource = null;
        if (type is not null) typeResource = new NamedApiResource<PokeType> { Name = type };

        NamedApiResource<MoveDamageClass>? damageClassResource = null;
        if (damageClass is not null) damageClassResource = new NamedApiResource<MoveDamageClass> { Name = damageClass };

        return new Move
        {
            Id = 1,
            Name = name,
            Type = typeResource!,
            DamageClass = damageClassResource!,
            Power = power,
            Accuracy = accuracy,
            Pp = pp,
            EffectEntries = shortEffect is null
                ? []
                :
                [
                    new VerboseEffect
                    {
                        Language = new NamedApiResource<Language> { Name = "en" },
                        Effect = shortEffect,
                        ShortEffect = shortEffect
                    }
                ]
        };
    }

    private static PokemonType CreatePokemonType(string type)
    {
        return new PokemonType
        {
            Type = new NamedApiResource<PokeType> { Name = type }
        };
    }

    private static PokemonAbility CreatePokemonAbility(string name, bool isHidden, string? effect)
    {
        return new PokemonAbility
        {
            Ability = new NamedApiResource<Ability> { Name = name },
            IsHidden = isHidden
        };
    }

    private static PokemonStat CreatePokemonStat(string statName, int value)
    {
        return new PokemonStat
        {
            Stat = new NamedApiResource<Stat> { Name = statName },
            BaseStat = value
        };
    }

    private static PokemonMove CreatePokemonMove(
        string moveName,
        IReadOnlyList<(string method, int level)> versionGroupDetails)
    {
        return new PokemonMove
        {
            Move = new NamedApiResource<Move> { Name = moveName },
            VersionGroupDetails = versionGroupDetails.Select(detail => new PokemonMoveVersion
            {
                LevelLearnedAt = detail.level,
                MoveLearnMethod = new NamedApiResource<MoveLearnMethod> { Name = detail.method }
            }).ToList()
        };
    }

    private static PokemonSpeciesVariety CreateSpeciesVariety(int id, string name)
    {
        return new PokemonSpeciesVariety
        {
            IsDefault = id == 1,
            Pokemon = new NamedApiResource<Pokemon>
            {
                Name = name,
                Url = $"https://pokeapi.co/api/v2/pokemon/{id}/"
            }
        };
    }

    private static PokemonSpecies CreateSpecies(string name)
    {
        return new PokemonSpecies
        {
            Id = 1,
            Name = name,
            Generation = new NamedApiResource<Generation> { Name = "generation-i" },
            Habitat = new NamedApiResource<PokemonHabitat> { Name = "forest" },
            GrowthRate = new NamedApiResource<GrowthRate> { Name = "medium-slow" },
            Color = new NamedApiResource<PokemonColor> { Name = "green" },
            CaptureRate = 45,
            BaseHappiness = 50,
            HatchCounter = 20,
            GenderRate = 8,
            FormsSwitchable = true,
            IsLegendary = false,
            IsMythical = false,
            IsBaby = false,
            FlavorTextEntries =
            [
                new PokemonSpeciesFlavorTexts
                {
                    Language = new NamedApiResource<Language> { Name = "en" },
                    FlavorText = "A friendly Pokémon."
                }
            ],
            EggGroups =
            [
                new NamedApiResource<EggGroup> { Name = "monster" }
            ],
            EvolutionChain = new ApiResource<EvolutionChain>
            {
                Url = "https://pokeapi.co/api/v2/evolution-chain/1/"
            },
            Varieties =
            [
                new PokemonSpeciesVariety
                {
                    IsDefault = true,
                    Pokemon = new NamedApiResource<Pokemon>
                    {
                        Name = name,
                        Url = "https://pokeapi.co/api/v2/pokemon/1/"
                    }
                }
            ]
        };
    }

    private sealed class RouteHttpMessageHandler(params (string Url, string Content)[] responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.ToString();
            var match = responses.FirstOrDefault(r => string.Equals(r.Url, url, StringComparison.OrdinalIgnoreCase));
            if (match == default)
                throw new InvalidOperationException($"Unexpected request: {url}");

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(match.Content)
            });
        }
    }
}