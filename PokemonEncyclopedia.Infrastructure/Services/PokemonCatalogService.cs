using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using PokeApiNet;
using PokemonEncyclopedia.Application.Services;

namespace PokemonEncyclopedia.Infrastructure.Services;

/// <summary>
///     Loads and caches Pokémon catalog data in memory and Redis.
/// </summary>
public class PokemonCatalogService : IPokemonCatalogService
{
    private const string AllPokemonCacheKey = "pokemon:all:v1";
    private const int MaxConcurrentRequests = 8;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<PokemonCatalogService> _logger;
    private readonly PokeApiClient _pokeApiClient;
    private readonly SemaphoreSlim _warmupGate = new(1, 1);
    private IReadOnlyList<Pokemon>? _allPokemon;
    private IReadOnlyDictionary<string, Pokemon>? _pokemonByName;
    private string? _allPokemonJson;

    public PokemonCatalogService(
        PokeApiClient pokeApiClient,
        IDistributedCache distributedCache,
        ILogger<PokemonCatalogService> logger)
    {
        _pokeApiClient = pokeApiClient;
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public bool IsWarm => _allPokemon is not null || _allPokemonJson is not null;

    public Exception? LastWarmupError { get; private set; }

    public async Task<IReadOnlyList<Pokemon>> GetAllPokemonAsync(CancellationToken cancellationToken)
    {
        var cachedJson = await GetAllPokemonJsonAsync(forceRefresh: false, cancellationToken).ConfigureAwait(false);
        if (_allPokemon is not null) return _allPokemon;

        var cachedPokemon = JsonSerializer.Deserialize<List<Pokemon>>(cachedJson, SerializerOptions);
        if (cachedPokemon is null)
            throw new InvalidOperationException("Unable to deserialize cached Pokémon catalog JSON.");

        _allPokemon = cachedPokemon;
        _pokemonByName = _allPokemon.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        return _allPokemon;
    }

    public async Task RefreshAllPokemonAsync(CancellationToken cancellationToken)
    {
        await GetAllPokemonJsonAsync(forceRefresh: true, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Pokemon?> GetPokemonByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
            return null;

        var pokemon = await GetAllPokemonAsync(cancellationToken).ConfigureAwait(false);
        if (_pokemonByName is not null && _pokemonByName.TryGetValue(normalizedName, out var cachedPokemon))
            return cachedPokemon;

        return pokemon.FirstOrDefault(p => string.Equals(p.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<NamedApiResource<PokemonSpecies>>> GetPokemonSpeciesByGenerationAsync(
        int generation,
        CancellationToken cancellationToken)
    {
        var cachedJson = await GetAllPokemonJsonAsync(forceRefresh: false, cancellationToken).ConfigureAwait(false);
        var (minId, maxId) = GetGenerationDexRange(generation);
        var resources = new List<NamedApiResource<PokemonSpecies>>();

        using var document = JsonDocument.Parse(cachedJson);
        foreach (var pokemonElement in document.RootElement.EnumerateArray())
        {
            if (!pokemonElement.TryGetProperty("id", out var idElement) || !idElement.TryGetInt32(out var id))
                continue;
            if (id < minId || id > maxId) continue;
            if (!pokemonElement.TryGetProperty("species", out var speciesElement)) continue;
            if (!speciesElement.TryGetProperty("name", out var nameElement)) continue;
            if (!speciesElement.TryGetProperty("url", out var urlElement)) continue;

            var name = nameElement.GetString();
            var url = urlElement.GetString();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url)) continue;

            resources.Add(new NamedApiResource<PokemonSpecies>
            {
                Name = name,
                Url = url
            });
        }

        return resources;
    }

    private async Task<string> GetAllPokemonJsonAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        if (!forceRefresh && !string.IsNullOrWhiteSpace(_allPokemonJson)) return _allPokemonJson;

        await _warmupGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!forceRefresh && !string.IsNullOrWhiteSpace(_allPokemonJson)) return _allPokemonJson;

            if (!forceRefresh)
            {
                var cachedJson = await _distributedCache.GetStringAsync(AllPokemonCacheKey, cancellationToken)
                    .ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(cachedJson))
                {
                    _allPokemonJson = cachedJson;
                    LastWarmupError = null;
                    _logger.LogInformation("Loaded Pokémon catalog from Redis cache");
                    return _allPokemonJson;
                }
            }

            _logger.LogInformation(forceRefresh
                ? "Refreshing Pokémon catalog cache"
                : "Warming Pokémon catalog cache");

            var resources = new List<NamedApiResource<Pokemon>>();
            await foreach (var resource in _pokeApiClient.GetAllNamedResourcesAsync<Pokemon>(cancellationToken)
                               .WithCancellation(cancellationToken)
                               .ConfigureAwait(false))
            {
                resources.Add(resource);
            }

            using var throttler = new SemaphoreSlim(MaxConcurrentRequests);
            var pokemonTasks = resources.Select(async resource =>
            {
                await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    return await _pokeApiClient.GetResourceAsync<Pokemon>(resource.Name, cancellationToken)
                        .ConfigureAwait(false);
                }
                finally
                {
                    throttler.Release();
                }
            });

            var pokemon = await Task.WhenAll(pokemonTasks).ConfigureAwait(false);
            _allPokemon = pokemon.OrderBy(p => p.Id).ToArray();
            _pokemonByName = _allPokemon.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
            _allPokemonJson = JsonSerializer.Serialize(_allPokemon, SerializerOptions);

            await _distributedCache.SetStringAsync(
                AllPokemonCacheKey,
                _allPokemonJson,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
                },
                cancellationToken).ConfigureAwait(false);

            LastWarmupError = null;

            _logger.LogInformation("Pokémon catalog cache warmed. Count={Count}", _allPokemon.Count);
            return _allPokemonJson;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LastWarmupError = ex;
            _logger.LogError(ex, "Failed to warm Pokémon catalog cache");
            throw;
        }
        finally
        {
            _warmupGate.Release();
        }
    }

    private static (int MinId, int MaxId) GetGenerationDexRange(int generation)
    {
        return generation switch
        {
            1 => (1, 151),
            2 => (152, 251),
            3 => (252, 386),
            4 => (387, 493),
            5 => (494, 649),
            6 => (650, 721),
            7 => (722, 809),
            8 => (810, 905),
            9 => (906, 1025),
            _ => throw new ArgumentOutOfRangeException(nameof(generation), generation, "Generation must be between 1 and 9.")
        };
    }
}
