namespace PokemonEncyclopedia.Application.DTOs;

/// <summary>
/// Lightweight DTO for displaying Pokemon in a list view.
/// Contains only essential information to minimize response payload.
/// </summary>
public sealed record PokemonListItemDto
{
    /// <summary>
    /// Internal id of the Pokemon species
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// The name of the Pokemon (e.g., "bulbasaur", "charmander")
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// URL to the official artwork image of this Pokemon
    /// </summary>
    public string ImageUrl { get; init; } = string.Empty;

    /// <summary>
    /// Whether this Pokemon is legendary
    /// </summary>
    public bool IsLegendary { get; init; }

    /// <summary>
    /// Whether this Pokemon is mythical
    /// </summary>
    public bool IsMythical { get; init; }
}
