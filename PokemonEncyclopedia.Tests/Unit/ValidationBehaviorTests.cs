using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using PokemonEncyclopedia.Application.Features.GetPokemonByName;

namespace PokemonEncyclopedia.Tests.Unit;

public class ValidationBehaviorTests
{
    [Fact]
    public void Validators_ShouldBeRegisteredInDependencyInjection()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddValidatorsFromAssembly(typeof(PokemonEncyclopedia.Application.Services.IPokemonCatalogService).Assembly);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var validator = serviceProvider.GetRequiredService<IValidator<GetPokemonByNameQuery>>();

        // Assert
        validator.Should().NotBeNull();
        validator.Should().BeOfType<GetPokemonByNameValidator>();
    }

    [Fact]
    public async Task Validator_ShouldValidateSuccessfully_WhenQueryIsValid()
    {
        // Arrange
        var validator = new GetPokemonByNameValidator();
        var query = new GetPokemonByNameQuery("pikachu");

        // Act
        var result = await validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validator_ShouldFail_WhenNameIsEmpty()
    {
        // Arrange
        var validator = new GetPokemonByNameValidator();
        var query = new GetPokemonByNameQuery("");

        // Act
        var result = await validator.ValidateAsync(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}
