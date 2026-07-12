using Microsoft.Extensions.Logging;
using PokemonEncyclopedia.Application.DTOs;

namespace PokemonEncyclopedia.Tests.Unit;

public class PokemonDtoTests
{
    [Fact]
    public void PokemonListItemDto_ShouldHaveRequiredProperties()
    {
        // Arrange
        var pokemonName = "pikachu";
        var pokemonId = 25;
        var isLegendary = false;
        var isMythical = false;
        var imageUrl = "https://example.com/image.png";

        // Act
        var dto = new PokemonListItemDto
        {
            Name = pokemonName,
            Id = pokemonId,
            IsLegendary = isLegendary,
            IsMythical = isMythical,
            ImageUrl = imageUrl
        };

        // Assert
        dto.Name.Should().Be(pokemonName);
        dto.Id.Should().Be(pokemonId);
        dto.IsLegendary.Should().Be(isLegendary);
        dto.IsMythical.Should().Be(isMythical);
        dto.ImageUrl.Should().Be(imageUrl);
    }

    [Fact]
    public void PokemonListItemDto_ShouldBeRecord()
    {
        // Arrange
        var dto1 = new PokemonListItemDto
        {
            Name = "pikachu",
            Id = 25,
            ImageUrl = "https://example.com/image1.png"
        };

        var dto2 = new PokemonListItemDto
        {
            Name = "pikachu",
            Id = 25,
            ImageUrl = "https://example.com/image1.png"
        };

        // Act & Assert
        dto1.Should().Be(dto2);
    }

    [Fact]
    public void PokemonListItemDto_ShouldHandleNullImageUrl()
    {
        // Arrange & Act
        var dto = new PokemonListItemDto
        {
            ImageUrl = ""
        };

        // Assert
        dto.ImageUrl.Should().Be(string.Empty);
    }
}
