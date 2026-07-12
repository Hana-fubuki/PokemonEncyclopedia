using PokemonEncyclopedia.Web;

namespace PokemonEncyclopedia.Tests.Unit;

public class PokemonFilterStateTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var state = new PokemonFilterState();

        // Assert
        state.SearchText.Should().Be(string.Empty);
        state.IncludeLegendary.Should().BeTrue();
        state.IncludeMythical.Should().BeTrue();
        state.SelectedGenerations.Should().HaveCount(9);
        state.SelectedTypes.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldInitializeAllGenerations()
    {
        // Act
        var state = new PokemonFilterState();

        // Assert
        state.SelectedGenerations.Should().Contain(Enumerable.Range(1, 9));
    }

    [Fact]
    public void SearchText_ShouldUpdateWhenChanged()
    {
        // Arrange
        var state = new PokemonFilterState();
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
        var state = new PokemonFilterState();
        var changedCount = 0;
        state.Changed += () => changedCount++;

        // Act
        state.SearchText = "pikachu";
        var countAfterFirst = changedCount;
        state.SearchText = "pikachu";

        // Assert
        changedCount.Should().Be(countAfterFirst);
    }

    [Fact]
    public void SearchText_ShouldNormalizeNullToEmpty()
    {
        // Arrange
        var state = new PokemonFilterState();

        // Act
        state.SearchText = null!;

        // Assert
        state.SearchText.Should().Be(string.Empty);
    }

    [Fact]
    public void IncludeLegendary_ShouldToggleValue()
    {
        // Arrange
        var state = new PokemonFilterState();
        var changedInvoked = false;
        state.Changed += () => changedInvoked = true;

        // Act
        state.IncludeLegendary = false;

        // Assert
        state.IncludeLegendary.Should().BeFalse();
        changedInvoked.Should().BeTrue();
    }

    [Fact]
    public void IncludeLegendary_ShouldNotRaiseChangedWhenSameValue()
    {
        // Arrange
        var state = new PokemonFilterState();
        var changedCount = 0;
        state.Changed += () => changedCount++;

        // Act
        state.IncludeLegendary = false;
        var countAfterFirst = changedCount;
        state.IncludeLegendary = false;

        // Assert
        changedCount.Should().Be(countAfterFirst);
    }

    [Fact]
    public void IncludeMythical_ShouldToggleValue()
    {
        // Arrange
        var state = new PokemonFilterState();
        var changedInvoked = false;
        state.Changed += () => changedInvoked = true;

        // Act
        state.IncludeMythical = false;

        // Assert
        state.IncludeMythical.Should().BeFalse();
        changedInvoked.Should().BeTrue();
    }

    [Fact]
    public void ToggleGeneration_ShouldAddGenerationWhenNotPresent()
    {
        // Arrange
        var state = new PokemonFilterState();
        state.SelectedGenerations.Clear();
        var changedInvoked = false;
        state.Changed += () => changedInvoked = true;

        // Act
        state.ToggleGeneration(5);

        // Assert
        state.SelectedGenerations.Should().Contain(5);
        changedInvoked.Should().BeTrue();
    }

    [Fact]
    public void ToggleGeneration_ShouldRemoveGenerationWhenPresent()
    {
        // Arrange
        var state = new PokemonFilterState();
        var changedInvoked = false;
        state.Changed += () => changedInvoked = true;

        // Act
        state.ToggleGeneration(5);

        // Assert
        state.SelectedGenerations.Should().NotContain(5);
        changedInvoked.Should().BeTrue();
    }

    [Fact]
    public void ToggleType_ShouldAddTypeWhenNotPresent()
    {
        // Arrange
        var state = new PokemonFilterState();
        var changedInvoked = false;
        state.Changed += () => changedInvoked = true;

        // Act
        state.ToggleType("fire");

        // Assert
        state.SelectedTypes.Should().Contain("fire");
        changedInvoked.Should().BeTrue();
    }

    [Fact]
    public void ToggleType_ShouldRemoveTypeWhenPresent()
    {
        // Arrange
        var state = new PokemonFilterState();
        state.ToggleType("fire");
        var changedCount = 0;
        state.Changed += () => changedCount++;

        // Act
        state.ToggleType("fire");

        // Assert
        state.SelectedTypes.Should().NotContain("fire");
        changedCount.Should().Be(1);
    }

    [Fact]
    public void ToggleType_ShouldBeCaseInsensitive()
    {
        // Arrange
        var state = new PokemonFilterState();
        state.ToggleType("FIRE");

        // Act
        var contains = state.SelectedTypes.Contains("fire");

        // Assert
        contains.Should().BeTrue();
    }

    [Fact]
    public void ClearFilters_ShouldResetAllFilters()
    {
        // Arrange
        var state = new PokemonFilterState();
        state.SearchText = "pikachu";
        state.IncludeLegendary = false;
        state.IncludeMythical = false;
        state.SelectedGenerations.Clear();
        state.SelectedTypes.Add("fire");
        var changedInvoked = false;
        state.Changed += () => changedInvoked = true;

        // Act
        state.ClearFilters();

        // Assert
        state.SearchText.Should().Be(string.Empty);
        state.IncludeLegendary.Should().BeTrue();
        state.IncludeMythical.Should().BeTrue();
        state.SelectedGenerations.Should().HaveCount(9);
        state.SelectedTypes.Should().BeEmpty();
        changedInvoked.Should().BeTrue();
    }
}
