using PokemonEncyclopedia.Application.Utilities;

namespace PokemonEncyclopedia.Tests.Unit;

public class NameNormalizationFuzzTests
{
    [Property]
    public bool Normalize_IsIdempotent(string input)
    {
        var normalized = NameNormalization.Normalize(input);
        return NameNormalization.Normalize(normalized) == normalized;
    }

    [Property]
    public bool Normalize_ReturnsTrimmedLowercase_WhenPresent(string input)
    {
        var normalized = NameNormalization.Normalize(input);
        return normalized is null || normalized == input.Trim().ToLowerInvariant();
    }
}