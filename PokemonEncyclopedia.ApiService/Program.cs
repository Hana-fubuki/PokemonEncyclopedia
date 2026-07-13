using PokemonEncyclopedia.ApiService;

var builder = WebApplication.CreateBuilder(args);
var isIntegrationTestMode = ApiServiceStartup.IsIntegrationTestMode(builder.Configuration);

ApiServiceStartup.ConfigureServices(builder, isIntegrationTestMode);

var app = builder.Build();
ApiServiceStartup.ConfigurePipeline(app, isIntegrationTestMode);
app.Run();