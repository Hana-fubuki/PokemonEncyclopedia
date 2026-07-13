namespace PokemonEncyclopedia.Tests;

public static class TestExecutionSettings
{
    public const string IntegrationCollectionName = "Aspire integration tests";

    public static readonly TimeSpan IntegrationTimeout =
        IsCiEnvironment ? TimeSpan.FromMinutes(2) : TimeSpan.FromSeconds(30);

    private static bool IsCiEnvironment =>
        string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);
}

[CollectionDefinition(TestExecutionSettings.IntegrationCollectionName, DisableParallelization = true)]
public sealed class IntegrationTestCollection;
