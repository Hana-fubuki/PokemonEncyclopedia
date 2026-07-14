namespace PokemonEncyclopedia.Application.Utilities;

public static class NameNormalization
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim().ToLowerInvariant();
    }
}