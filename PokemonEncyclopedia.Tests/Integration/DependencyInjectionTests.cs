using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;

namespace PokemonEncyclopedia.Tests.Integration;

public class DependencyInjectionTests
{
    [Fact]
    public void ServiceCollection_ShouldSupportAddValidators()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddValidatorsFromAssemblyContaining(typeof(PokemonEncyclopedia.Application.Services.IPokemonCatalogService));

        // Assert
        services.Should().NotBeEmpty();
    }

    [Fact]
    public void ServiceProvider_ShouldBeBuiltSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddValidatorsFromAssemblyContaining(typeof(PokemonEncyclopedia.Application.Services.IPokemonCatalogService));

        // Act
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.Should().NotBeNull();
    }
}
