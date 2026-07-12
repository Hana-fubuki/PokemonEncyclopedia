using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace PokemonEncyclopedia.ServiceDefaults;

public static class Telemetry
{
    public const string ActivitySourceName = "PokemonEncyclopedia";
    public const string MeterName = "PokemonEncyclopedia";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> ApiRequests = Meter.CreateCounter<long>(
        "pokemon_api_requests_total",
        description: "Total API requests processed by the Pokémon Encyclopedia API.");

    public static readonly Histogram<double> ApiRequestDuration = Meter.CreateHistogram<double>(
        "pokemon_api_request_duration_ms",
        unit: "ms",
        description: "Duration of Pokémon Encyclopedia API requests.");

    public static readonly Histogram<double> ClientRequestDuration = Meter.CreateHistogram<double>(
        "pokemon_client_request_duration_ms",
        unit: "ms",
        description: "Duration of Pokémon Encyclopedia client requests.");
}
