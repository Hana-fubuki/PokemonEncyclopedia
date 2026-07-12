using FluentAssertions;
using Xunit;
using Microsoft.Extensions.Caching.Memory;
using PokemonEncyclopedia.Web;

namespace PokemonEncyclopedia.Tests.Unit;

public class PokemonVarietiesMethodTests
{
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly HttpClient _httpClient;

    public PokemonVarietiesMethodTests()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://pokeapi.co/api/v2/")
        };
    }

    [Fact]
    public void PokemonApiClient_HasGetPokemonVarietiesAsync_Method()
    {
        // Arrange & Act
        var client = new PokemonApiClient(_httpClient, _cache);
        var method = client.GetType().GetMethod("GetPokemonVarietiesAsync");

        // Assert
        method.Should().NotBeNull("GetPokemonVarietiesAsync method should exist");
    }

    [Fact]
    public void GetPokemonVarietiesAsync_MethodSignature_IsCorrect()
    {
        // Arrange & Act
        var client = new PokemonApiClient(_httpClient, _cache);
        var method = client.GetType().GetMethod("GetPokemonVarietiesAsync");

        // Assert
        method.Should().NotBeNull();
        method.ReturnType.Name.Should().Contain("Task", "Should return Task");
    }

    [Fact]
    public void PokemonApiClient_ExportsGetPokemonVarietiesAsync_Publicly()
    {
        // Arrange
        var client = new PokemonApiClient(_httpClient, _cache);

        // Act
        var hasMethod = client.GetType().GetMethods()
            .Any(m => m.Name == "GetPokemonVarietiesAsync" && m.IsPublic);

        // Assert
        hasMethod.Should().BeTrue("GetPokemonVarietiesAsync should be public");
    }

    [Fact]
    public void GetPokemonVarietiesAsync_AcceptsStringParameter()
    {
        // Arrange
        var client = new PokemonApiClient(_httpClient, _cache);
        var method = client.GetType().GetMethods()
            .FirstOrDefault(m => m.Name == "GetPokemonVarietiesAsync" &&
                m.GetParameters().Any(p => p.ParameterType == typeof(string)));

        // Assert
        method.Should().NotBeNull("GetPokemonVarietiesAsync should accept string parameter");
    }

    [Fact]
    public void GetPokemonVarietiesAsync_AcceptsCancellationToken()
    {
        // Arrange
        var client = new PokemonApiClient(_httpClient, _cache);
        var method = client.GetType().GetMethods()
            .FirstOrDefault(m => m.Name == "GetPokemonVarietiesAsync" &&
                m.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken)));

        // Assert
        method.Should().NotBeNull("GetPokemonVarietiesAsync should accept CancellationToken parameter");
    }
}
