namespace PokemonEncyclopedia.Tests.Integration;

[Collection(TestExecutionSettings.AppHostIntegrationCollectionName)]
[Trait("Category", "Integration")]
public class IntegrationWebTests(AspireAppHostFixture fixture)
{
    private readonly AspireAppHostFixture _fixture = fixture;

    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        using var requestCts = new CancellationTokenSource(TestExecutionSettings.HttpRequestTimeout);
        await _fixture.EnsureWebHealthyAsync(requestCts.Token);

        var response = await _fixture.CreateWebClient().GetAsync("/", requestCts.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPokemonDetailPageReturnsOkStatusCode()
    {
        using var requestCts = new CancellationTokenSource(TestExecutionSettings.HttpRequestTimeout);
        await _fixture.EnsureWebHealthyAsync(requestCts.Token);

        var response = await _fixture.CreateWebClient().GetAsync("/pokemon/pikachu", requestCts.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiServiceIsHealthy()
    {
        using var requestCts = new CancellationTokenSource(TestExecutionSettings.HttpRequestTimeout);
        await _fixture.EnsureApiHealthyAsync(requestCts.Token);
    }

    [Fact]
    public async Task ApiServiceHealthCheckEndpointResponds()
    {
        using var requestCts = new CancellationTokenSource(TestExecutionSettings.HttpRequestTimeout);
        await _fixture.EnsureApiHealthyAsync(requestCts.Token);

        var response = await _fixture.CreateApiClient().GetAsync("/health", requestCts.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
