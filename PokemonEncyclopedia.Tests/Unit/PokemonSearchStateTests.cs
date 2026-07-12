using PokemonEncyclopedia.Web;

namespace PokemonEncyclopedia.Tests.Unit;

public class PokemonSearchStateTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmpty()
    {
        // Act
        var state = new PokemonSearchState();

        // Assert
        state.SearchText.Should().Be(string.Empty);
    }

    [Fact]
    public void SearchText_ShouldUpdateWhenChanged()
    {
        // Arrange
        var state = new PokemonSearchState();
        var changedInvoked = false;
        state.Changed += () => changedInvoked = true;

        // Act
        state.SearchText = "pikachu";

        // Assert
        state.SearchText.Should().Be("pikachu");
        changedInvoked.Should().BeTrue();
    }

    [Fact]
    public void SearchText_ShouldNotRaiseChangedWhenSameValue()
    {
        // Arrange
        var state = new PokemonSearchState();
        var changedCount = 0;
        state.Changed += () => changedCount++;

        // Act
        state.SearchText = "charizard";
        var countAfterFirst = changedCount;
        state.SearchText = "charizard";

        // Assert
        changedCount.Should().Be(countAfterFirst);
    }

    [Fact]
    public void SearchText_ShouldNormalizeNullToEmpty()
    {
        // Arrange
        var state = new PokemonSearchState();

        // Act
        state.SearchText = null!;

        // Assert
        state.SearchText.Should().Be(string.Empty);
    }

    [Fact]
    public void SearchText_ShouldHandleWhitespace()
    {
        // Arrange
        var state = new PokemonSearchState();
        var changedCount = 0;
        state.Changed += () => changedCount++;

        // Act
        state.SearchText = "bulbasaur";
        state.SearchText = "  ";

        // Assert
        state.SearchText.Should().Be("  ");
        changedCount.Should().Be(2);
    }

    [Fact]
    public void Changed_EventShouldBeInvocableMultipleTimes()
    {
        // Arrange
        var state = new PokemonSearchState();
        var callCount = 0;
        state.Changed += () => callCount++;

        // Act
        state.SearchText = "squirtle";
        state.SearchText = "wartortle";
        state.SearchText = "blastoise";

        // Assert
        callCount.Should().Be(3);
    }

    [Fact]
    public void Changed_EventShouldSupportMultipleSubscribers()
    {
        // Arrange
        var state = new PokemonSearchState();
        var subscriber1Called = false;
        var subscriber2Called = false;
        state.Changed += () => subscriber1Called = true;
        state.Changed += () => subscriber2Called = true;

        // Act
        state.SearchText = "onix";

        // Assert
        subscriber1Called.Should().BeTrue();
        subscriber2Called.Should().BeTrue();
    }
}
