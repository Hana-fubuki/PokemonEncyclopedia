using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.Infrastructure.Services;
using PokemonEncyclopedia.Tests.Common;

namespace PokemonEncyclopedia.Tests.Unit;

public class InfrastructureCatalogRefreshTests
{
    [Fact]
    public async Task GetAllPokemonAsync_FetchesFromApiAndCachesWhenCacheEmpty()
    {
        var handler = new RoutingHttpMessageHandler(uri =>
        {
            var path = uri.AbsolutePath;
            if (path.Contains("pokemon-species"))
                return Ok(
                    """{"count":1,"next":null,"previous":null,"results":[{"name":"bulbasaur","url":"https://pokeapi.co/api/v2/pokemon-species/1/"}]}""");
            if (path.Contains("/pokemon/bulbasaur"))
                return Ok("""{"id":1,"name":"bulbasaur"}""");
            return null;
        });

        var service = CreateService(handler);

        var result = await service.Catalog.GetAllPokemonAsync(CancellationToken.None);

        result.Should().ContainSingle(p => p.Name == "bulbasaur");
        service.Cache.Get("pokemon:all:v1").Should().NotBeNull();
        service.Catalog.LastWarmupError.Should().BeNull();
    }

    [Fact]
    public async Task GetAllPokemonAsync_SetsLastWarmupErrorWhenApiFails()
    {
        var handler = new RoutingHttpMessageHandler(uri =>
            uri.AbsolutePath.Contains("pokemon-species")
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : null);

        var service = CreateService(handler);

        var act = () => service.Catalog.GetAllPokemonAsync(CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        service.Catalog.LastWarmupError.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllMovesAsync_FetchesFromApiAndCachesWhenCacheEmpty()
    {
        var handler = new RoutingHttpMessageHandler(uri =>
        {
            var path = uri.AbsolutePath;
            if (path.Contains("/move/pound"))
                return Ok("""{"id":1,"name":"pound"}""");
            if (path.EndsWith("/move"))
                return Ok(
                    """{"count":1,"next":null,"previous":null,"results":[{"name":"pound","url":"https://pokeapi.co/api/v2/move/1/"}]}""");
            return null;
        });

        var service = CreateService(handler);

        var result = await service.Catalog.GetAllMovesAsync(CancellationToken.None);

        result.Should().ContainSingle(m => m.Name == "pound");
        service.Cache.Get("pokemon:moves:all:v1").Should().NotBeNull();
        service.Catalog.LastWarmupError.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAbilitiesAsync_FetchesFromApiAndCachesWhenCacheEmpty()
    {
        var handler = new RoutingHttpMessageHandler(uri =>
        {
            var path = uri.AbsolutePath;
            if (path.Contains("/ability/overgrow"))
                return Ok("""{"id":65,"name":"overgrow"}""");
            if (path.EndsWith("/ability"))
                return Ok(
                    """{"count":1,"next":null,"previous":null,"results":[{"name":"overgrow","url":"https://pokeapi.co/api/v2/ability/65/"}]}""");
            return null;
        });

        var service = CreateService(handler);

        var result = await service.Catalog.GetAllAbilitiesAsync(CancellationToken.None);

        result.Should().ContainSingle(a => a.Name == "overgrow");
        service.Cache.Get("pokemon:abilities:all:v1").Should().NotBeNull();
        service.Catalog.LastWarmupError.Should().BeNull();
    }

    [Fact]
    public async Task GetMoveByNameAsync_FetchesFromApiAndCachesWhenCacheMissing()
    {
        var handler = new RoutingHttpMessageHandler(uri =>
            uri.AbsolutePath.Contains("/move/pound") ? Ok("""{"id":1,"name":"pound"}""") : null);

        var service = CreateService(handler);

        var result = await service.Catalog.GetMoveByNameAsync("Pound", CancellationToken.None);

        result!.Name.Should().Be("pound");
        service.Cache.Get("pokemon:move:v1:pound").Should().NotBeNull();
    }

    [Fact]
    public async Task GetAbilityByNameAsync_FetchesFromApiAndCachesWhenCacheMissing()
    {
        var handler = new RoutingHttpMessageHandler(uri =>
            uri.AbsolutePath.Contains("/ability/overgrow") ? Ok("""{"id":65,"name":"overgrow"}""") : null);

        var service = CreateService(handler);

        var result = await service.Catalog.GetAbilityByNameAsync("Overgrow", CancellationToken.None);

        result!.Name.Should().Be("overgrow");
        service.Cache.Get("pokemon:ability:v1:overgrow").Should().NotBeNull();
    }

    [Fact]
    public async Task GetPokemonSpeciesByNameAsync_FetchesFromApiAndCachesWhenCacheMissing()
    {
        var handler = new RoutingHttpMessageHandler(uri =>
            uri.AbsolutePath.Contains("/pokemon-species/bulbasaur") ? Ok("""{"id":1,"name":"bulbasaur"}""") : null);

        var service = CreateService(handler);

        var result = await service.Catalog.GetPokemonSpeciesByNameAsync("Bulbasaur", CancellationToken.None);

        result!.Name.Should().Be("bulbasaur");
        service.Cache.Get("pokemon:species:v1:bulbasaur").Should().NotBeNull();
    }

    [Fact]
    public async Task GetEvolutionChainByIdAsync_FetchesFromApiAndCachesWhenCacheMissing()
    {
        var handler = new RoutingHttpMessageHandler(uri =>
            uri.AbsolutePath.Contains("/evolution-chain/1") ? Ok("""{"id":1}""") : null);

        var service = CreateService(handler);

        var result = await service.Catalog.GetEvolutionChainByIdAsync(1, CancellationToken.None);

        result!.Id.Should().Be(1);
        service.Cache.Get("pokemon:evolution:v1:1").Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllPokemonSpeciesAsync_FetchesSpeciesForEachPokemonWhenCacheMissing()
    {
        var handler = new RoutingHttpMessageHandler(uri =>
            uri.AbsolutePath.Contains("/pokemon-species/bulbasaur") ? Ok("""{"id":1,"name":"bulbasaur"}""") : null);

        var service = CreateService(handler);
        await service.Cache.SetStringAsync("pokemon:all:v1",
            """[{"id":1,"name":"bulbasaur","species":{"name":"bulbasaur","url":"https://pokeapi.co/api/v2/pokemon-species/1/"},"types":[{"type":{"name":"grass"}}]}]""");

        var result = await service.Catalog.GetAllPokemonSpeciesAsync(CancellationToken.None);

        result.Should().ContainSingle(s => s.Name == "bulbasaur");
        service.Cache.Get("pokemon:species:all:v1").Should().NotBeNull();
    }

    private static HttpResponseMessage Ok(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
    }

    private static (PokemonCatalogService Catalog, FakeDistributedCache Cache) CreateService(
        HttpMessageHandler handler)
    {
        var cache = new FakeDistributedCache();
        var httpClient = new HttpClient(handler)
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

    private sealed class RoutingHttpMessageHandler(Func<Uri, HttpResponseMessage?> resolver) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = resolver(request.RequestUri!);
            if (response is null)
                throw new InvalidOperationException($"Unexpected request: {request.RequestUri}");

            return Task.FromResult(response);
        }
    }
}