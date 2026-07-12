using Microsoft.Extensions.Logging;

namespace PokemonEncyclopedia.Tests.Common;

public static class TestHelpers
{
    public static ILoggerFactory CreateTestLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });
    }

    public static string NormalizeName(string name)
    {
        return (name ?? string.Empty).Trim().ToLowerInvariant();
    }

    public static bool IsValidPokemonGeneration(int generation)
    {
        return generation >= 1 && generation <= 9;
    }
}
