using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using PokemonEncyclopedia.Web;

namespace PokemonEncyclopedia.Tests.Unit;

public class PokemonApiClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private PokemonApiClient CreateClient()
    {
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };
        return new PokemonApiClient(httpClient, _cache);
    }

    [Fact]
    public async Task GetAllPokemonAsync_ShouldReturnEmptyListWhenResponseIsNull()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/details")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("null")
            });

        var client = CreateClient();

        // Act
        var result = await client.GetAllPokemonAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPokemonAsync_ShouldReturnNullWhenNameIsEmpty()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var result = await client.GetPokemonAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPokemonAsync_ShouldReturnNullWhenNameIsWhitespace()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var result = await client.GetPokemonAsync("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMoveAsync_ShouldReturnNullWhenNameIsEmpty()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var result = await client.GetMoveAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSpeciesAsync_ShouldReturnNullWhenNameIsEmpty()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var result = await client.GetSpeciesAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEvolutionChainAsync_ShouldReturnNullWhenChainIdIsZeroOrNegative()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var result1 = await client.GetEvolutionChainAsync(0);
        var result2 = await client.GetEvolutionChainAsync(-1);

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
    }

    [Fact]
    public async Task GetLegendaryPokemonNamesAsync_ShouldReturnEmptySetWhenResponseIsNull()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/legendary")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("null")
            });

        var client = CreateClient();

        // Act
        var result = await client.GetLegendaryPokemonNamesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAbilitiesAsync_ShouldReturnEmptyListWhenResponseIsNull()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/abilities")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent("null")
            });

        var client = CreateClient();

        // Act
        var result = await client.GetAllAbilitiesAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAbilityAsync_ShouldReturnNullWhenNameIsEmpty()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var result = await client.GetAbilityAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAbilityAsync_ShouldReturnNullWhenNameIsWhitespace()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var result = await client.GetAbilityAsync("   ");

        // Assert
        result.Should().BeNull();
    }
}
