using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace PokemonEncyclopedia.Tests.Unit;

public class QueryHandlerTests
{
    [Fact]
    public void ServiceCollection_ShouldBeAvailable()
    {
        // Act
        var services = new ServiceCollection();

        // Assert
        services.Should().NotBeNull();
    }

    [Fact]
    public void ServiceProvider_ShouldBeBuilt()
    {
        // Act
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.Should().NotBeNull();
    }
}
