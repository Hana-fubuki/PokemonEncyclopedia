namespace PokemonEncyclopedia.Tests.Integration;

[Collection(TestExecutionSettings.AppHostIntegrationCollectionName)]
[Trait("Category", "Integration")]
[Trait("Category", "RequiresAppHost")]
public class ApiEndpointsTests(AspireAppHostFixture fixture)
{
    private readonly AspireAppHostFixture _fixture = fixture;

    [Fact]
    public async Task GetAllPokemonEndpoint_ShouldReturn()
    {
        using var requestCts = new CancellationTokenSource(TestExecutionSettings.HttpRequestTimeout);
        await _fixture.EnsureApiHealthyAsync(requestCts.Token);

        var response = await _fixture.CreateApiClient().GetAsync("/api/PokeApi/details", requestCts.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPokemonByNameEndpoint_ShouldReturn()
    {
        using var requestCts = new CancellationTokenSource(TestExecutionSettings.HttpRequestTimeout);
        await _fixture.EnsureApiHealthyAsync(requestCts.Token);

        var response = await _fixture.CreateApiClient().GetAsync("/api/PokeApi/pokemon/pikachu", requestCts.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAbilitiesEndpoint_ShouldReturn()
    {
        using var requestCts = new CancellationTokenSource(TestExecutionSettings.HttpRequestTimeout);
        await _fixture.EnsureApiHealthyAsync(requestCts.Token);

        var response = await _fixture.CreateApiClient().GetAsync("/api/PokeApi/abilities", requestCts.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLegendaryPokemonEndpoint_ShouldReturn()
    {
        using var requestCts = new CancellationTokenSource(TestExecutionSettings.HttpRequestTimeout);
        await _fixture.EnsureApiHealthyAsync(requestCts.Token);

        var response = await _fixture.CreateApiClient().GetAsync("/api/PokeApi/legendary", requestCts.Token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
