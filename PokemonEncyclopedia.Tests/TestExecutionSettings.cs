namespace PokemonEncyclopedia.Tests;

public static class TestExecutionSettings
{
    public const string AppHostIntegrationCollectionName = "Aspire AppHost integration tests";
    public const string ExternalIntegrationCollectionName = "External integration tests";

    public static readonly TimeSpan IntegrationTimeout =
        IsCiEnvironment ? TimeSpan.FromMinutes(2) : TimeSpan.FromSeconds(30);
    public static readonly TimeSpan IntegrationStartupTimeout =
        TimeSpan.FromMinutes(10);
    public static readonly TimeSpan HttpRequestTimeout =
        IsCiEnvironment ? TimeSpan.FromSeconds(45) : TimeSpan.FromSeconds(20);

    private static bool IsCiEnvironment =>
        string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);
}

[CollectionDefinition(TestExecutionSettings.AppHostIntegrationCollectionName, DisableParallelization = true)]
public sealed class AppHostIntegrationCollection : ICollectionFixture<PokemonEncyclopedia.Tests.Integration.AspireAppHostFixture>;

[CollectionDefinition(TestExecutionSettings.ExternalIntegrationCollectionName, DisableParallelization = true)]
public sealed class ExternalIntegrationCollection;
