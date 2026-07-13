using PokemonEncyclopedia.Web;
using Microsoft.Extensions.Caching.Memory;

namespace PokemonEncyclopedia.Tests.Unit;

public class EdgeCaseTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  \t\n  ")]
    [InlineData(null)]
    public void PokemonFilterState_SearchText_ShouldHandleWhitespaceVariations(string? input)
    {
        // Arrange
        var state = new PokemonFilterState();

        // Act
        state.SearchText = input ?? string.Empty;

        // Assert
        state.SearchText.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10)]
    [InlineData(100)]
    public void PokemonFilterState_ToggleGeneration_ShouldHandleOutOfRangeValues(int generation)
    {
        // Arrange
        var state = new PokemonFilterState();

        // Act & Assert
        // Should not throw
        state.ToggleGeneration(generation);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Fire")]
    [InlineData("WATER")]
    [InlineData("grass")]
    public void PokemonFilterState_ToggleType_ShouldHandleVariations(string type)
    {
        // Arrange
        var state = new PokemonFilterState();

        // Act & Assert
        // Should not throw
        state.ToggleType(type);
    }

    [Theory]
    [InlineData("PIKACHU")]
    [InlineData("pikachu")]
    [InlineData("PiKaChU")]
    [InlineData("  pikachu  ")]
    public void PokemonSearchState_SearchText_ShouldHandleVariations(string input)
    {
        // Arrange
        var state = new PokemonSearchState();

        // Act
        state.SearchText = input;

        // Assert
        state.SearchText.Should().NotBeNull();
    }
}

public class MemoryCacheTests
{
    [Fact]
    public void MemoryCache_ShouldStoreAndRetrieveValues()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cacheKey = "test-key";
        var cacheValue = "test-value";

        // Act
        cache.Set(cacheKey, cacheValue);
        var result = cache.Get<string>(cacheKey);

        // Assert
        result.Should().Be(cacheValue);
    }

    [Fact]
    public void MemoryCache_ShouldReturnNullForMissingKey()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());

        // Act
        var result = cache.Get<string>("non-existent-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void MemoryCache_ShouldUpdateExistingValue()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cacheKey = "test-key";

        // Act
        cache.Set(cacheKey, "value1");
        var result1 = cache.Get<string>(cacheKey);
        cache.Set(cacheKey, "value2");
        var result2 = cache.Get<string>(cacheKey);

        // Assert
        result1.Should().Be("value1");
        result2.Should().Be("value2");
    }

    [Fact]
    public void MemoryCache_TryGetValue_ShouldReturnFalseForMissingKey()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());

        // Act
        var result = cache.TryGetValue("non-existent-key", out string? value);

        // Assert
        result.Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact]
    public void MemoryCache_TryGetValue_ShouldReturnTrueForExistingKey()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        cache.Set("test-key", "test-value");

        // Act
        var result = cache.TryGetValue("test-key", out string? value);

        // Assert
        result.Should().BeTrue();
        value.Should().Be("test-value");
    }
}
