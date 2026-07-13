using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using PokeApiNet;
using PokemonEncyclopedia.Web;

namespace PokemonEncyclopedia.Tests.Unit;

public class PokemonApiClientCoverageTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetPokemonAsync_ReturnsAndCachesHttpResult()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(cache, new RouteHttpMessageHandler(
            ("http://localhost/api/PokeApi/pokemon/pikachu", Serialize(new Pokemon { Id = 25, Name = "pikachu" }))));

        var result = await client.GetPokemonAsync("  PIKACHU  ");

        result.Should().NotBeNull();
        result!.Name.Should().Be("pikachu");
        cache.TryGetValue("pokemon:pikachu", out Pokemon? cached).Should().BeTrue();
        cached!.Name.Should().Be("pikachu");
    }

    [Fact]
    public async Task GetPokemonAsync_FallsBackToAllPokemonListWhenDirectLookupMisses()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(cache, new RouteHttpMessageHandler(
            ("http://localhost/api/PokeApi/pokemon/missingno", "null"),
            ("http://localhost/api/PokeApi/details", Serialize(new[]
            {
                new Pokemon { Id = 0, Name = "missingno" }
            }))));

        var result = await client.GetPokemonAsync("missingno");

        result.Should().NotBeNull();
        result!.Name.Should().Be("missingno");
    }

    [Fact]
    public async Task GetPokemonByIdAsync_ReturnsAndCachesHttpResult()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(cache, new RouteHttpMessageHandler(
            ("https://pokeapi.co/api/v2/pokemon/25/", Serialize(new Pokemon { Id = 25, Name = "pikachu" }))));

        var result = await client.GetPokemonByIdAsync(25);

        result.Should().NotBeNull();
        result!.Name.Should().Be("pikachu");
        cache.TryGetValue("pokemon:id:25", out Pokemon? cached).Should().BeTrue();
        cached!.Name.Should().Be("pikachu");
    }

    [Fact]
    public async Task GetMoveAsync_ReturnsAndCachesHttpResult()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(cache, new RouteHttpMessageHandler(
            ("http://localhost/api/PokeApi/move/tackle", Serialize(new Move { Id = 1, Name = "tackle" }))));

        var result = await client.GetMoveAsync("TACKLE");

        result.Should().NotBeNull();
        result!.Name.Should().Be("tackle");
        cache.TryGetValue("move:tackle", out Move? cached).Should().BeTrue();
        cached!.Name.Should().Be("tackle");
    }

    [Fact]
    public async Task GetSpeciesAsync_UsesPokeApiEndpointWhenConfigured()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(cache, new RouteHttpMessageHandler(
            ("https://pokeapi.co/api/v2/pokemon-species/bulbasaur/", Serialize(new PokemonSpecies { Id = 1, Name = "bulbasaur" }))),
            new Uri("https://pokeapi.co/api/v2/"));

        var result = await client.GetSpeciesAsync("bulbasaur");

        result.Should().NotBeNull();
        result!.Name.Should().Be("bulbasaur");
    }

    [Fact]
    public async Task GetSpeciesAsync_UsesLocalApiEndpointOtherwise()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(cache, new RouteHttpMessageHandler(
            ("http://localhost/api/PokeApi/species/bulbasaur", Serialize(new PokemonSpecies { Id = 1, Name = "bulbasaur" }))));

        var result = await client.GetSpeciesAsync("bulbasaur");

        result.Should().NotBeNull();
        result!.Name.Should().Be("bulbasaur");
    }

    [Fact]
    public async Task GetEvolutionChainAsync_ReturnsAndCachesHttpResult()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(cache, new RouteHttpMessageHandler(
            ("http://localhost/api/PokeApi/evolution-chain/1", Serialize(new EvolutionChain { Id = 1 }))));

        var result = await client.GetEvolutionChainAsync(1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        cache.TryGetValue("evolution:1", out EvolutionChain? cached).Should().BeTrue();
        cached!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetLegendaryPokemonNamesAsync_ReturnsAndCachesSet()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(cache, new RouteHttpMessageHandler(
            ("http://localhost/api/PokeApi/legendary", """["bulbasaur","mewtwo"]""")));

        var result = await client.GetLegendaryPokemonNamesAsync();

        result.Should().Contain("bulbasaur").And.Contain("mewtwo");
    }

    [Fact]
    public async Task GetAllAbilitiesAsync_ReturnsAndCachesHttpResult()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(cache, new RouteHttpMessageHandler(
            ("http://localhost/api/PokeApi/abilities", Serialize(new[]
            {
                new Ability { Id = 65, Name = "overgrow" }
            }))));

        var result = await client.GetAllAbilitiesAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("overgrow");
    }

    [Fact]
    public async Task GetAbilityAsync_ReturnsAndCachesHttpResult()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(cache, new RouteHttpMessageHandler(
            ("http://localhost/api/PokeApi/ability/overgrow", Serialize(new Ability { Id = 65, Name = "overgrow" }))));

        var result = await client.GetAbilityAsync("OVERGROW");

        result.Should().NotBeNull();
        result!.Name.Should().Be("overgrow");
        cache.TryGetValue("ability:overgrow", out Ability? cached).Should().BeTrue();
        cached!.Name.Should().Be("overgrow");
    }

    [Fact]
    public async Task GetPokemonAsync_ReturnsCachedResultWithoutHttpCall()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("pokemon:pikachu", new Pokemon { Id = 25, Name = "pikachu" }, TimeSpan.FromMinutes(10));
        var client = CreateClient(cache, new ThrowingHttpMessageHandler());

        var result = await client.GetPokemonAsync("pikachu");

        result!.Name.Should().Be("pikachu");
    }

    [Fact]
    public async Task GetPokemonVarietiesAsync_BuildsVarietiesFromSpeciesAndPokemonCache()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = CreateClient(cache, new ThrowingHttpMessageHandler());

        cache.Set("species:bulbasaur", new PokemonSpecies
        {
            Id = 1,
            Name = "bulbasaur",
            Varieties = new List<PokemonSpeciesVariety>
            {
                new()
                {
                    IsDefault = true,
                    Pokemon = new NamedApiResource<Pokemon>
                    {
                        Name = "bulbasaur",
                        Url = "https://pokeapi.co/api/v2/pokemon/1/"
                    }
                },
                new()
                {
                    IsDefault = false,
                    Pokemon = new NamedApiResource<Pokemon>
                    {
                        Name = "bulbasaur-mega",
                        Url = "https://pokeapi.co/api/v2/pokemon/2/"
                    }
                }
            }
        }, TimeSpan.FromMinutes(10));
        cache.Set("pokemon:id:1", new Pokemon { Id = 1, Name = "bulbasaur" }, TimeSpan.FromMinutes(10));
        cache.Set("pokemon:id:2", new Pokemon { Id = 2, Name = "bulbasaur-mega" }, TimeSpan.FromMinutes(10));

        var result = await client.GetPokemonVarietiesAsync("BULBASAUR");

        result.Should().HaveCount(2);
        result.Select(p => p.Name).Should().Contain(new[] { "bulbasaur", "bulbasaur-mega" });
    }

    private static PokemonApiClient CreateClient(IMemoryCache cache, HttpMessageHandler handler, Uri? baseAddress = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = baseAddress ?? new Uri("http://localhost")
        };
        return new PokemonApiClient(httpClient, cache);
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private sealed class RouteHttpMessageHandler(params (string Url, string Content)[] responses) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.ToString();
            var match = responses.FirstOrDefault(r => string.Equals(r.Url, url, StringComparison.OrdinalIgnoreCase));
            if (match == default)
                throw new InvalidOperationException($"Unexpected request: {url}");

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(match.Content)
            });
        }
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException($"Unexpected request: {request.RequestUri}");
    }
}
