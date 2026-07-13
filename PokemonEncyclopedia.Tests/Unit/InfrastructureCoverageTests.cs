using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.Application.Services;
using PokemonEncyclopedia.Infrastructure.DependencyInjection;
using PokemonEncyclopedia.Infrastructure.Services;
using PokemonEncyclopedia.Tests.Common;
using PokeType = PokeApiNet.Type;

namespace PokemonEncyclopedia.Tests.Unit;

public class InfrastructureCoverageTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AddInfrastructureServices_RegistersCatalogService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddInfrastructureServices();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IPokemonCatalogService>().Should().NotBeNull();
        provider.GetRequiredService<PokemonCacheRefreshJob>().Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllPokemonAsync_UsesCachedJsonAndBuildsLookup()
    {
        var service = CreateService();
        await service.Cache.SetStringAsync("pokemon:all:v1", JsonSerializer.Serialize(new[]
        {
            CreatePokemon(2, "charmeleon"),
            CreatePokemon(1, "bulbasaur"),
            CreatePokemon(3, "missingno", includeTypes: false)
        }, JsonOptions));

        var result = await service.Catalog.GetAllPokemonAsync(CancellationToken.None);

        result.Select(p => p.Name).Should().Equal("bulbasaur", "charmeleon");
        (await service.Catalog.GetPokemonByNameAsync("BULBASAUR", CancellationToken.None))!.Name.Should()
            .Be("bulbasaur");
        (await service.Catalog.GetPokemonByNameAsync("   ", CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task GetAllBasePokemonAsync_FiltersVariantNames()
    {
        var service = CreateService();
        await service.Cache.SetStringAsync("pokemon:all:v1", JsonSerializer.Serialize(new[]
        {
            CreatePokemon(1, "bulbasaur"),
            CreatePokemon(2, "pikachu-rockstar"),
            CreatePokemon(3, "ivysaur")
        }, JsonOptions));

        var result = await service.Catalog.GetAllBasePokemonAsync(CancellationToken.None);

        result.Select(p => p.Name).Should().Equal("bulbasaur", "ivysaur");
    }

    [Fact]
    public async Task GetAllPokemonSpeciesAsync_UsesCachedSpeciesJson()
    {
        var service = CreateService();
        var species = new[]
        {
            new PokemonSpecies { Id = 1, Name = "bulbasaur" }
        };
        await service.Cache.SetStringAsync("pokemon:species:all:v1", JsonSerializer.Serialize(species, JsonOptions));

        var result = await service.Catalog.GetAllPokemonSpeciesAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("bulbasaur");
    }

    [Fact]
    public async Task GetAllMovesAsync_UsesCachedJson()
    {
        var service = CreateService();
        var moves = new[] { new Move { Id = 1, Name = "pound" } };
        await service.Cache.SetStringAsync("pokemon:moves:all:v1", JsonSerializer.Serialize(moves, JsonOptions));

        var result = await service.Catalog.GetAllMovesAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("pound");
    }

    [Fact]
    public async Task GetAllAbilitiesAsync_UsesCachedJson()
    {
        var service = CreateService();
        var abilities = new[] { new Ability { Id = 65, Name = "overgrow" } };
        await service.Cache.SetStringAsync("pokemon:abilities:all:v1",
            JsonSerializer.Serialize(abilities, JsonOptions));

        var result = await service.Catalog.GetAllAbilitiesAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("overgrow");
    }

    [Fact]
    public async Task GetMoveAndAbilityAndSpeciesByName_ReturnCachedValues()
    {
        var service = CreateService();
        await service.Cache.SetStringAsync("pokemon:move:v1:tackle",
            JsonSerializer.Serialize(new Move { Id = 1, Name = "tackle" }, JsonOptions));
        await service.Cache.SetStringAsync("pokemon:ability:v1:overgrow",
            JsonSerializer.Serialize(new Ability { Id = 65, Name = "overgrow" }, JsonOptions));
        await service.Cache.SetStringAsync("pokemon:species:v1:bulbasaur",
            JsonSerializer.Serialize(new PokemonSpecies { Id = 1, Name = "bulbasaur" }, JsonOptions));

        (await service.Catalog.GetMoveByNameAsync("  tackle  ", CancellationToken.None))!.Name.Should().Be("tackle");
        (await service.Catalog.GetAbilityByNameAsync("OVERGROW", CancellationToken.None))!.Name.Should().Be("overgrow");
        (await service.Catalog.GetPokemonSpeciesByNameAsync("bulbasaur", CancellationToken.None))!.Name.Should()
            .Be("bulbasaur");
    }

    [Fact]
    public async Task GetEvolutionAndLegendaryData_ReturnsCachedValues()
    {
        var service = CreateService();
        await service.Cache.SetStringAsync("pokemon:evolution:v1:1",
            JsonSerializer.Serialize(new EvolutionChain { Id = 1 }, JsonOptions));
        await service.Cache.SetStringAsync("pokemon:species:v1:bulbasaur", JsonSerializer.Serialize(new PokemonSpecies
        {
            Id = 1,
            Name = "bulbasaur",
            IsLegendary = true,
            EvolutionChain = new ApiResource<EvolutionChain> { Url = "https://pokeapi.co/api/v2/evolution-chain/1/" }
        }, JsonOptions));
        await service.Cache.SetStringAsync("pokemon:all:v1", JsonSerializer.Serialize(new[]
        {
            CreatePokemon(1, "bulbasaur")
        }, JsonOptions));

        var chain = await service.Catalog.GetEvolutionChainByIdAsync(1, CancellationToken.None);
        chain.Should().NotBeNull();
        chain!.Id.Should().Be(1);

        var bySpecies = await service.Catalog.GetEvolutionChainBySpeciesNameAsync("bulbasaur", CancellationToken.None);
        bySpecies.Should().NotBeNull();
        bySpecies!.Id.Should().Be(1);

        var legendary = await service.Catalog.GetLegendaryPokemonNamesAsync(CancellationToken.None);
        legendary.Should().Contain("bulbasaur");
    }

    [Fact]
    public async Task GetPokemonSpeciesByGenerationAsync_FiltersExpectedGeneration()
    {
        var service = CreateService();
        await service.Cache.SetStringAsync("pokemon:all:v1", "[]");
        await service.Cache.SetStringAsync("pokemon:all:v1", JsonSerializer.Serialize(new[]
        {
            new
            {
                id = 1,
                species = new { name = "bulbasaur", url = "https://pokeapi.co/api/v2/pokemon-species/1/" }
            },
            new
            {
                id = 155,
                species = new { name = "cyndaquil", url = "https://pokeapi.co/api/v2/pokemon-species/155/" }
            },
            new
            {
                id = 999,
                species = new { name = "testmon", url = "https://pokeapi.co/api/v2/pokemon-species/999/" }
            }
        }, JsonOptions));

        var result = await service.Catalog.GetPokemonSpeciesByGenerationAsync(2, CancellationToken.None);

        result.Should().ContainSingle(x => x.Name == "cyndaquil");
    }

    [Fact]
    public async Task GetPokemonSpeciesByGenerationAsync_ThrowsForInvalidGeneration()
    {
        var service = CreateService();
        await service.Cache.SetStringAsync("pokemon:all:v1", "[]");

        var act = () => service.Catalog.GetPokemonSpeciesByGenerationAsync(0, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetAllPokemonAsync_RefreshesFromApiWhenCacheMissing()
    {
        var service = CreateService(new StaticHttpMessageHandler(
            ("https://pokeapi.co/api/v2/pokemon-species?limit=5000&offset=0", """{"results":[]}""")));

        var result = await service.Catalog.GetAllPokemonAsync(CancellationToken.None);

        result.Should().BeEmpty();
        service.Cache.Get("pokemon:all:v1").Should().NotBeNull();
    }

    [Fact]
    public void GetResourceId_CoversUrlAndIdFallback()
    {
        InvokeGetResourceId(new { Url = "https://pokeapi.co/api/v2/evolution-chain/77/" }).Should().Be(77);
        InvokeGetResourceId(new { Url = "/api/v2/evolution-chain/88/" }).Should().Be(88);
        InvokeGetResourceId(new { Id = 42 }).Should().Be(42);
    }

    [Theory]
    [InlineData(1, 1, 151)]
    [InlineData(9, 906, 1025)]
    public void GetGenerationDexRange_CoversBoundaryGenerations(int generation, int minId, int maxId)
    {
        var result = InvokeGenerationDexRange(generation);
        result.Should().Be((minId, maxId));
    }

    [Fact]
    public async Task RefreshAllPokemonAsync_WarmsCacheWhenMissing()
    {
        var service = CreateService(new StaticHttpMessageHandler(
            ("https://pokeapi.co/api/v2/pokemon-species?limit=5000&offset=0", """{"results":[]}""")));

        await service.Catalog.RefreshAllPokemonAsync(CancellationToken.None);

        service.Cache.Get("pokemon:all:v1").Should().NotBeNull();
    }

    private static (PokemonCatalogService Catalog, FakeDistributedCache Cache) CreateService(
        HttpMessageHandler? handler = null)
    {
        var cache = new FakeDistributedCache();
        var httpClient = new HttpClient(handler ?? new ThrowingHttpMessageHandler())
        {
            BaseAddress = new Uri("https://pokeapi.co/api/v2/")
        };
        var pokeApiClient = new PokeApiClient(httpClient);
        var catalog = new PokemonCatalogService(
            pokeApiClient,
            httpClient,
            cache,
            Mock.Of<ILogger<PokemonCatalogService>>());

        return (catalog, cache);
    }

    private static (int MinId, int MaxId) InvokeGenerationDexRange(int generation)
    {
        var method =
            typeof(PokemonCatalogService).GetMethod("GetGenerationDexRange",
                BindingFlags.NonPublic | BindingFlags.Static);
        return ((int MinId, int MaxId))method!.Invoke(null, [generation])!;
    }

    private static int? InvokeGetResourceId(object resource)
    {
        var method =
            typeof(PokemonCatalogService).GetMethod("GetResourceId", BindingFlags.NonPublic | BindingFlags.Static);
        return (int?)method!.Invoke(null, [resource]);
    }

    private static Pokemon CreatePokemon(int id, string name, bool includeTypes = true)
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
            Types = includeTypes
                ? [new PokemonType { Type = new NamedApiResource<PokeType> { Name = "grass" } }]
                : []
        };
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException($"Unexpected request: {request.RequestUri}");
        }
    }

    private sealed class StaticHttpMessageHandler(params (string Url, string Content)[] responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var match = responses.FirstOrDefault(r =>
                string.Equals(r.Url, request.RequestUri?.ToString(), StringComparison.OrdinalIgnoreCase));
            if (match == default)
                throw new InvalidOperationException($"Unexpected request: {request.RequestUri}");

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(match.Content)
            });
        }
    }
}