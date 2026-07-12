using PokemonEncyclopedia.Application.Features.GetPokemonByName;
using PokemonEncyclopedia.Application.Features.GetMoveByName;
using PokemonEncyclopedia.Application.Features.GetEvolutionChainBySpeciesName;

namespace PokemonEncyclopedia.Tests.Unit;

public class QueryRecordTests
{
    [Fact]
    public void GetPokemonByNameQuery_ShouldBeRecord()
    {
        // Arrange
        var query1 = new GetPokemonByNameQuery("pikachu");
        var query2 = new GetPokemonByNameQuery("pikachu");

        // Act & Assert
        query1.Should().Be(query2);
    }

    [Fact]
    public void GetPokemonByNameQuery_ShouldHaveName()
    {
        // Arrange
        var query = new GetPokemonByNameQuery("pikachu");

        // Act & Assert
        query.Name.Should().Be("pikachu");
    }

    [Fact]
    public void GetMoveByNameQuery_ShouldBeRecord()
    {
        // Arrange
        var query1 = new GetMoveByNameQuery("tackle");
        var query2 = new GetMoveByNameQuery("tackle");

        // Act & Assert
        query1.Should().Be(query2);
    }

    [Fact]
    public void GetMoveByNameQuery_ShouldHaveName()
    {
        // Arrange
        var query = new GetMoveByNameQuery("tackle");

        // Act & Assert
        query.Name.Should().Be("tackle");
    }

    [Fact]
    public void GetEvolutionChainBySpeciesNameQuery_ShouldBeRecord()
    {
        // Arrange
        var query1 = new GetEvolutionChainBySpeciesNameQuery("bulbasaur");
        var query2 = new GetEvolutionChainBySpeciesNameQuery("bulbasaur");

        // Act & Assert
        query1.Should().Be(query2);
    }

    [Fact]
    public void GetEvolutionChainBySpeciesNameQuery_ShouldHaveSpeciesName()
    {
        // Arrange
        var query = new GetEvolutionChainBySpeciesNameQuery("bulbasaur");

        // Act & Assert
        query.SpeciesName.Should().Be("bulbasaur");
    }
}
