using System.Net.Http.Json;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using PokeApiNet;
using PokemonEncyclopedia.ServiceDefaults;

namespace PokemonEncyclopedia.Web;

public sealed class PokemonApiClient(HttpClient httpClient, IMemoryCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private const string AllPokemonCacheKey = "pokemon:all";
    private const string LegendaryPokemonCacheKey = "pokemon:legendary";
        private const string AllAbilitiesCacheKey = "abilities:all";

    public async Task<IReadOnlyList<Pokemon>> GetAllPokemonAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var pokemon = await cache.GetOrCreateAsync(AllPokemonCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var response = await httpClient.GetFromJsonAsync<Pokemon[]>("/api/PokeApi/details", cancellationToken)
                .ConfigureAwait(false);
            return (IReadOnlyList<Pokemon>)(response ?? Array.Empty<Pokemon>());
        }).ConfigureAwait(false) ?? Array.Empty<Pokemon>();
        sw.Stop();
        Telemetry.ClientRequestDuration.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("client.operation", "all-pokemon"));
        return pokemon;
    }

    public async Task<Pokemon?> GetPokemonAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedName)) return null;

        var cacheKey = $"pokemon:{normalizedName}";
        if (cache.TryGetValue(cacheKey, out Pokemon? cachedPokemon))
            return cachedPokemon;

        var sw = Stopwatch.StartNew();
        var pokemon = await httpClient.GetFromJsonAsync<Pokemon>($"/api/PokeApi/pokemon/{Uri.EscapeDataString(normalizedName)}", cancellationToken)
            .ConfigureAwait(false);
        sw.Stop();
        Telemetry.ClientRequestDuration.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("client.operation", "pokemon"),
            new KeyValuePair<string, object?>("pokemon.name", normalizedName));
        if (pokemon is not null)
        {
            cache.Set(cacheKey, pokemon, CacheDuration);
            return pokemon;
        }

        var allPokemon = await GetAllPokemonAsync(cancellationToken).ConfigureAwait(false);
        var fromList = allPokemon.FirstOrDefault(p => string.Equals(p.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        if (fromList is not null)
            cache.Set(cacheKey, fromList, CacheDuration);

        return fromList;
    }

    public async Task<Move?> GetMoveAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedName)) return null;

        var cacheKey = $"move:{normalizedName}";
        if (cache.TryGetValue(cacheKey, out Move? cachedMove))
            return cachedMove;

        var sw = Stopwatch.StartNew();
        var move = await httpClient.GetFromJsonAsync<Move>($"/api/PokeApi/move/{Uri.EscapeDataString(normalizedName)}", cancellationToken)
            .ConfigureAwait(false);
        sw.Stop();
        Telemetry.ClientRequestDuration.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("client.operation", "move"),
            new KeyValuePair<string, object?>("move.name", normalizedName));
        if (move is not null)
            cache.Set(cacheKey, move, CacheDuration);

        return move;
    }

    public async Task<PokemonSpecies?> GetSpeciesAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedName)) return null;

        var cacheKey = $"species:{normalizedName}";
        if (cache.TryGetValue(cacheKey, out PokemonSpecies? cachedSpecies))
            return cachedSpecies;

        var sw = Stopwatch.StartNew();
        var species = await httpClient.GetFromJsonAsync<PokemonSpecies>($"/api/PokeApi/species/{Uri.EscapeDataString(normalizedName)}", cancellationToken)
            .ConfigureAwait(false);
        sw.Stop();
        Telemetry.ClientRequestDuration.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("client.operation", "species"),
            new KeyValuePair<string, object?>("species.name", normalizedName));
        if (species is not null)
            cache.Set(cacheKey, species, CacheDuration);

        return species;
    }

    public async Task<EvolutionChain?> GetEvolutionChainAsync(int chainId, CancellationToken cancellationToken = default)
    {
        if (chainId <= 0) return null;

        var cacheKey = $"evolution:{chainId}";
        if (cache.TryGetValue(cacheKey, out EvolutionChain? cachedChain))
            return cachedChain;

        var sw = Stopwatch.StartNew();
        var chain = await httpClient.GetFromJsonAsync<EvolutionChain>($"/api/PokeApi/evolution-chain/{chainId}", cancellationToken)
            .ConfigureAwait(false);
        sw.Stop();
        Telemetry.ClientRequestDuration.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("client.operation", "evolution"),
            new KeyValuePair<string, object?>("evolution.chain_id", chainId));
        if (chain is not null)
            cache.Set(cacheKey, chain, CacheDuration);

        return chain;
    }

    public async Task<IReadOnlySet<string>> GetLegendaryPokemonNamesAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var legendaryNames = await cache.GetOrCreateAsync(LegendaryPokemonCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var response = await httpClient.GetFromJsonAsync<HashSet<string>>("/api/PokeApi/legendary", cancellationToken)
                .ConfigureAwait(false);
            return (IReadOnlySet<string>)(response ?? new HashSet<string>());
        }).ConfigureAwait(false) ?? new HashSet<string>();
        sw.Stop();
        Telemetry.ClientRequestDuration.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("client.operation", "legendary-pokemon"));
        return legendaryNames;
    }

    public async Task<IReadOnlyList<Ability>> GetAllAbilitiesAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var abilities = await cache.GetOrCreateAsync(AllAbilitiesCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var response = await httpClient.GetFromJsonAsync<Ability[]>("/api/PokeApi/abilities", cancellationToken)
                .ConfigureAwait(false);
            return (IReadOnlyList<Ability>)(response ?? Array.Empty<Ability>());
        }).ConfigureAwait(false) ?? Array.Empty<Ability>();
        sw.Stop();
        Telemetry.ClientRequestDuration.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("client.operation", "all-abilities"));
        return abilities;
    }

    public async Task<Ability?> GetAbilityAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedName)) return null;

        var cacheKey = $"ability:{normalizedName}";
        if (cache.TryGetValue(cacheKey, out Ability? cachedAbility))
            return cachedAbility;

        var sw = Stopwatch.StartNew();
        var ability = await httpClient.GetFromJsonAsync<Ability>($"/api/PokeApi/ability/{Uri.EscapeDataString(normalizedName)}", cancellationToken)
            .ConfigureAwait(false);
        sw.Stop();
        Telemetry.ClientRequestDuration.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("client.operation", "ability"),
            new KeyValuePair<string, object?>("ability.name", normalizedName));
        if (ability is not null)
            cache.Set(cacheKey, ability, CacheDuration);

        return ability;
    }

    public async Task<IReadOnlyList<Pokemon>> GetPokemonVarietiesAsync(string speciesName, CancellationToken cancellationToken = default)
    {
        var normalizedName = speciesName.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedName)) return Array.Empty<Pokemon>();

        var cacheKey = $"varieties:{normalizedName}";
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<Pokemon>? cachedVarieties))
            return cachedVarieties;

        var sw = Stopwatch.StartNew();
        var varieties = new List<Pokemon>();
        var species = await GetSpeciesAsync(normalizedName, cancellationToken).ConfigureAwait(false);
        
        if (species?.Varieties is not null && species.Varieties.Count > 0)
        {
            foreach (var variety in species.Varieties)
            {
                if (variety.Pokemon?.Name is not null)
                {
                    var pokemon = await GetPokemonAsync(variety.Pokemon.Name, cancellationToken).ConfigureAwait(false);
                    if (pokemon is not null)
                    {
                        varieties.Add(pokemon);
                    }
                }
            }
        }

        sw.Stop();
        var varietiesList = (IReadOnlyList<Pokemon>)varieties.AsReadOnly();
        Telemetry.ClientRequestDuration.Record(sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("client.operation", "varieties"),
            new KeyValuePair<string, object?>("species.name", normalizedName));
        
        if (varieties.Count > 0)
            cache.Set(cacheKey, varietiesList, CacheDuration);

        return varietiesList;
    }
}
