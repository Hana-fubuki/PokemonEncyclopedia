using Microsoft.Extensions.Caching.Memory;
using PokemonEncyclopedia.Web;

namespace PokemonEncyclopedia.Tests.Integration;

[Collection(TestExecutionSettings.IntegrationCollectionName)]
[Trait("Category", "Integration")]
public class PokemonVarietiesIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "RequiresNetwork")]
    public async Task GetPokemonVarietiesAsync_WithBulbasaur_ReturnsVarieties()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pokeapi.co/api/v2/")
        };
        var client = new PokemonApiClient(httpClient, cache);

        // Act
        var varieties = await client.GetPokemonVarietiesAsync("bulbasaur");

        // Assert
        varieties.Should().NotBeEmpty("Bulbasaur should have varieties");
        varieties.Should().AllSatisfy(v =>
        {
            v.Should().NotBeNull();
            v.Name.Should().NotBeNullOrWhiteSpace();
            v.Id.Should().BeGreaterThan(0);
        });
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "RequiresNetwork")]
    public async Task GetPokemonVarietiesAsync_WithEevee_ReturnsMultipleVarieties()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pokeapi.co/api/v2/")
        };
        var client = new PokemonApiClient(httpClient, cache);

        // Act
        var varieties = await client.GetPokemonVarietiesAsync("eevee");

        // Assert
        varieties.Should().NotBeEmpty("Eevee should have varieties");
        varieties.Count.Should().BeGreaterThan(1, "Eevee should have multiple varieties/forms");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPokemonVarietiesAsync_WithNonexistentSpecies_ReturnsEmpty()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pokeapi.co/api/v2/")
        };
        var client = new PokemonApiClient(httpClient, cache);

        // Act
        var varieties = await client.GetPokemonVarietiesAsync("nonexistentpokemon12345");

        // Assert
        varieties.Should().BeEmpty("Nonexistent species should return empty list");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "RequiresNetwork")]
    public async Task GetPokemonVarietiesAsync_VarietiesContainValidTypes()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pokeapi.co/api/v2/")
        };
        var client = new PokemonApiClient(httpClient, cache);

        // Act
        var varieties = await client.GetPokemonVarietiesAsync("bulbasaur");

        // Assert
        varieties.Should().AllSatisfy(v =>
        {
            v.Types.Should().NotBeEmpty("Each variety should have at least one type");
            v.Types.Should().AllSatisfy(t =>
            {
                t.Type.Should().NotBeNull();
                t.Type.Name.Should().NotBeNullOrWhiteSpace();
            });
        });
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "RequiresNetwork")]
    public async Task GetPokemonVarietiesAsync_VarietiesContainSprites()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pokeapi.co/api/v2/")
        };
        var client = new PokemonApiClient(httpClient, cache);

        // Act
        var varieties = await client.GetPokemonVarietiesAsync("bulbasaur");

        // Assert
        varieties.Should().NotBeEmpty();
        varieties.Should().AllSatisfy(v => { v.Sprites.Should().NotBeNull("Each variety should have sprites"); });
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "RequiresNetwork")]
    public async Task GetPokemonVarietiesAsync_NormalizesProperly()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pokeapi.co/api/v2/")
        };
        var client = new PokemonApiClient(httpClient, cache);

        // Act
        var variety1 = await client.GetPokemonVarietiesAsync("bulbasaur");
        var variety2 = await client.GetPokemonVarietiesAsync("BULBASAUR");
        var variety3 = await client.GetPokemonVarietiesAsync("  bulbasaur  ");

        // Assert
        variety1.Should().Equal(variety2, "Case should be normalized");
        variety1.Should().Equal(variety3, "Whitespace should be trimmed");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "RequiresNetwork")]
    public async Task GetPokemonVarietiesAsync_CachesResults()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pokeapi.co/api/v2/")
        };
        var client = new PokemonApiClient(httpClient, cache);

        // Act - First call
        var varieties1 = await client.GetPokemonVarietiesAsync("pikachu");

        // Act - Second call (should be cached)
        var varieties2 = await client.GetPokemonVarietiesAsync("pikachu");

        // Assert
        varieties1.Should().NotBeEmpty();
        varieties2.Should().NotBeEmpty();
        varieties1.Should().Equal(varieties2, "Cached results should be identical");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "RequiresNetwork")]
    public async Task GetPokemonVarietiesAsync_WithCancellation_ThrowsException()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pokeapi.co/api/v2/")
        };
        var client = new PokemonApiClient(httpClient, cache);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await client.GetPokemonVarietiesAsync("bulbasaur", cts.Token)
        );
    }

    [Theory]
    [InlineData("squirtle")]
    [InlineData("charmander")]
    [InlineData("pikachu")]
    [Trait("Category", "Integration")]
    [Trait("Category", "RequiresNetwork")]
    public async Task GetPokemonVarietiesAsync_WithValidSpecies_ReturnsNonNull(string speciesName)
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pokeapi.co/api/v2/")
        };
        var client = new PokemonApiClient(httpClient, cache);

        // Act
        var varieties = await client.GetPokemonVarietiesAsync(speciesName);

        // Assert
        varieties.Should().NotBeNull("GetPokemonVarietiesAsync should never return null");
    }
}