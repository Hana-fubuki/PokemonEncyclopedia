using PokemonEncyclopedia.Application.Features.GetPokemonByName;
using PokemonEncyclopedia.Application.Features.GetMoveByName;
using PokemonEncyclopedia.Application.Features.GetEvolutionChainBySpeciesName;
using PokemonEncyclopedia.Application.Features.GetPokemonByGeneration;

namespace PokemonEncyclopedia.Tests.Validators;

public class GetPokemonByNameValidatorTests
{
    private readonly GetPokemonByNameValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldSucceedWithValidName()
    {
        // Arrange
        var query = new GetPokemonByNameQuery("pikachu");

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ShouldFailWithEmptyName()
    {
        // Arrange
        var query = new GetPokemonByNameQuery("");

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_ShouldFailWithNameExceedingMaxLength()
    {
        // Arrange
        var longName = new string('a', 51);
        var query = new GetPokemonByNameQuery(longName);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_ShouldSucceedWithMaxLengthName()
    {
        // Arrange
        var maxLengthName = new string('a', 50);
        var query = new GetPokemonByNameQuery(maxLengthName);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ShouldFailWithWhitespaceOnlyName()
    {
        // Arrange
        var query = new GetPokemonByNameQuery("   ");

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}

public class GetMoveByNameValidatorTests
{
    private readonly GetMoveByNameValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldSucceedWithValidName()
    {
        // Arrange
        var query = new GetMoveByNameQuery("tackle");

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ShouldFailWithEmptyName()
    {
        // Arrange
        var query = new GetMoveByNameQuery("");

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ShouldFailWithNameExceedingMaxLength()
    {
        // Arrange
        var longName = new string('a', 51);
        var query = new GetMoveByNameQuery(longName);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}

public class GetEvolutionChainBySpeciesNameValidatorTests
{
    private readonly GetEvolutionChainBySpeciesNameValidator _validator = new();

    [Fact]
    public async Task Validate_ShouldSucceedWithValidName()
    {
        // Arrange
        var query = new GetEvolutionChainBySpeciesNameQuery("bulbasaur");

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ShouldFailWithEmptyName()
    {
        // Arrange
        var query = new GetEvolutionChainBySpeciesNameQuery("");

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ShouldFailWithNameExceedingMaxLength()
    {
        // Arrange
        var longName = new string('a', 51);
        var query = new GetEvolutionChainBySpeciesNameQuery(longName);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}

public class GetPokemonByGenerationValidatorTests
{
    private readonly GetPokemonByGenerationValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(9)]
    public async Task Validate_ShouldSucceedWithValidGeneration(int generation)
    {
        // Arrange
        var query = new GetPokemonByGenerationQuery(generation);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-1)]
    public async Task Validate_ShouldFailWithInvalidGeneration(int generation)
    {
        // Arrange
        var query = new GetPokemonByGenerationQuery(generation);

        // Act
        var result = await _validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
    }
}
