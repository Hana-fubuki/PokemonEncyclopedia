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
    private const string AllMovesCacheKey = "pokemon:moves:all:v1";
    private const string AllAbilitiesCacheKey = "pokemon:abilities:all:v1";
    private const string LegendaryPokemonCacheKey = "pokemon:legendary:v1";
    private const string MoveCachePrefix = "pokemon:move:v1:";
    private const string SpeciesCachePrefix = "pokemon:species:v1:";
    private const string EvolutionCachePrefix = "pokemon:evolution:v1:";
    private const int MaxConcurrentRequests = 8;
    private const string AllSpeciesCacheKey = "pokemon:species:all:v1";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<PokemonCatalogService> _logger;
    private readonly PokeApiClient _pokeApiClient;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _warmupGate = new(1, 1);
    private IReadOnlyList<Pokemon>? _allPokemon;
    private IReadOnlyList<PokemonSpecies>? _allSpecies;
    private IReadOnlyDictionary<string, Pokemon>? _pokemonByName;
    private string? _allPokemonJson;
    private string? _allSpeciesJson;
    private IReadOnlyList<Move>? _allMoves;
    private string? _allMovesJson;
    private IReadOnlyList<Ability>? _allAbilities;
    private string? _allAbilitiesJson;
    private IReadOnlySet<string>? _legendaryPokemonNames;

    public PokemonCatalogService(
        PokeApiClient pokeApiClient,
        HttpClient httpClient,
        IDistributedCache distributedCache,
        ILogger<PokemonCatalogService> logger)
    {
        _pokeApiClient = pokeApiClient;
        _httpClient = httpClient;
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

        // Filter to first 1024 canonical forms (no regional variants) with valid data
        _allPokemon = cachedPokemon
            .Where(p => p.Types?.Count > 0)  // Ensure types are not null/empty
            .OrderBy(p => p.Id)
            .Take(1024)
            .ToList()
            .AsReadOnly();
        _pokemonByName = _allPokemon.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        return _allPokemon;
    }

    public async Task<IReadOnlyList<PokemonSpecies>> GetAllPokemonSpeciesAsync(CancellationToken cancellationToken)
    {
        // Use CancellationToken.None for cache operations
        var cachedJson = await _distributedCache.GetStringAsync(AllSpeciesCacheKey, CancellationToken.None).ConfigureAwait(false);
        if (cachedJson is not null)
        {
            var cachedSpecies = JsonSerializer.Deserialize<List<PokemonSpecies>>(cachedJson, SerializerOptions);
            if (cachedSpecies is not null)
            {
                _allSpecies = cachedSpecies;
                return _allSpecies;
            }
        }

        // If not cached, fetch all Pokemon and get their species data
        var allPokemon = await GetAllPokemonAsync(cancellationToken).ConfigureAwait(false);
        var speciesList = new List<PokemonSpecies>();

        using var throttler = new SemaphoreSlim(MaxConcurrentRequests);
        foreach (var pokemon in allPokemon)
        {
            await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var species = await _pokeApiClient.GetResourceAsync<PokemonSpecies>(pokemon.Species.Name, cancellationToken)
                    .ConfigureAwait(false);
                if (species is not null)
                    speciesList.Add(species);
            }
            finally
            {
                throttler.Release();
            }
        }

        _allSpecies = speciesList.AsReadOnly();
        _allSpeciesJson = JsonSerializer.Serialize(_allSpecies, SerializerOptions);

        await _distributedCache.SetStringAsync(
            AllSpeciesCacheKey,
            _allSpeciesJson,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = null },
            CancellationToken.None).ConfigureAwait(false);

        return _allSpecies;
    }

    public async Task<IReadOnlyList<Pokemon>> GetAllBasePokemonAsync(CancellationToken cancellationToken)
    {
        var allPokemon = await GetAllPokemonAsync(cancellationToken).ConfigureAwait(false);
        // Return only base forms (no variants/regional forms with dashes in names)
        return allPokemon.Where(p => !p.Name.Contains('-')).ToArray();
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

    public async Task<Move?> GetMoveByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
            return null;

        var cacheKey = $"{MoveCachePrefix}{normalizedName.ToLowerInvariant()}";
        return await GetCachedResourceAsync(cacheKey,
            () => _pokeApiClient.GetResourceAsync<Move>(normalizedName, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<Ability?> GetAbilityByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
            return null;

        var cacheKey = $"pokemon:ability:v1:{normalizedName.ToLowerInvariant()}";
        return await GetCachedResourceAsync(cacheKey,
            () => _pokeApiClient.GetResourceAsync<Ability>(normalizedName, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<PokemonSpecies?> GetPokemonSpeciesByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
            return null;

        var cacheKey = $"{SpeciesCachePrefix}{normalizedName.ToLowerInvariant()}";
        return await GetCachedResourceAsync(cacheKey,
            () => _pokeApiClient.GetResourceAsync<PokemonSpecies>(normalizedName, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<EvolutionChain?> GetEvolutionChainByIdAsync(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
            return null;

        var cacheKey = $"{EvolutionCachePrefix}{id}";
        return await GetCachedResourceAsync(cacheKey,
            () => _pokeApiClient.GetResourceAsync<EvolutionChain>(id, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<EvolutionChain?> GetEvolutionChainBySpeciesNameAsync(string speciesName, CancellationToken cancellationToken)
    {
        var pokemon = await GetPokemonByNameAsync(speciesName, cancellationToken).ConfigureAwait(false);
        if (pokemon is null)
            return null;

        var species = await GetPokemonSpeciesByNameAsync(pokemon.Species.Name, cancellationToken).ConfigureAwait(false);
        if (species?.EvolutionChain is null)
            return null;

        var evolutionChainId = GetResourceId(species.EvolutionChain);
        return evolutionChainId is null
            ? null
            : await GetEvolutionChainByIdAsync(evolutionChainId.Value, cancellationToken).ConfigureAwait(false);
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

    public async Task<IReadOnlyList<Move>> GetAllMovesAsync(CancellationToken cancellationToken)
    {
        var cachedJson = await GetAllMovesJsonAsync(forceRefresh: false, cancellationToken).ConfigureAwait(false);
        if (_allMoves is not null) return _allMoves;

        var cachedMoves = JsonSerializer.Deserialize<List<Move>>(cachedJson, SerializerOptions);
        if (cachedMoves is null)
            throw new InvalidOperationException("Unable to deserialize cached moves JSON.");

        _allMoves = cachedMoves;
        return _allMoves;
    }

    public async Task<IReadOnlyList<Ability>> GetAllAbilitiesAsync(CancellationToken cancellationToken)
    {
        var cachedJson = await GetAllAbilitiesJsonAsync(forceRefresh: false, cancellationToken).ConfigureAwait(false);
        if (_allAbilities is not null) return _allAbilities;

        var cachedAbilities = JsonSerializer.Deserialize<List<Ability>>(cachedJson, SerializerOptions);
        if (cachedAbilities is null)
            throw new InvalidOperationException("Unable to deserialize cached abilities JSON.");

        _allAbilities = cachedAbilities;
        return _allAbilities;
    }

    public async Task<IReadOnlySet<string>> GetLegendaryPokemonNamesAsync(CancellationToken cancellationToken)
    {
        if (_legendaryPokemonNames is not null)
            return _legendaryPokemonNames;

        var pokemon = await GetAllPokemonAsync(cancellationToken).ConfigureAwait(false);
        var legendaryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var throttler = new SemaphoreSlim(MaxConcurrentRequests);
        var speciesTasks = pokemon.Select(async p =>
        {
            await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var species = await GetPokemonSpeciesByNameAsync(p.Species.Name, cancellationToken).ConfigureAwait(false);
                return (name: p.Name, isLegendary: species?.IsLegendary ?? false);
            }
            finally
            {
                throttler.Release();
            }
        });

        var results = await Task.WhenAll(speciesTasks).ConfigureAwait(false);
        foreach (var result in results.Where(r => r.isLegendary))
        {
            legendaryNames.Add(result.name);
        }

        _legendaryPokemonNames = legendaryNames;
        _logger.LogInformation("Legendary Pokémon names cached. Count={Count}", legendaryNames.Count);
        return _legendaryPokemonNames;
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

            // Fetch species names directly from pokemon-species endpoint  
            var speciesNames = new List<string>();
            int offset = 0;
            const int limit = 5000;
            const string pokeApiBaseUrl = "https://pokeapi.co/api/v2/";
            
            try
            {
                while (true)
                {
                    var url = $"{pokeApiBaseUrl}pokemon-species?limit={limit}&offset={offset}";
                    _logger.LogDebug("Fetching from {Url}", url);
                    
                    var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead, cancellationToken)
                        .ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    
                    var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    using var jsonDoc = JsonDocument.Parse(jsonContent);
                    var root = jsonDoc.RootElement;
                    
                    if (!root.TryGetProperty("results", out var resultsElement))
                        break;
                    
                    var resultsArray = resultsElement.EnumerateArray().ToList();
                    if (resultsArray.Count == 0)
                        break;

                    foreach (var result in resultsArray)
                    {
                        if (result.TryGetProperty("name", out var nameElement) && nameElement.GetString() is string name)
                        {
                            speciesNames.Add(name);
                        }
                    }
                    
                    _logger.LogInformation("Fetched {Count} species so far (offset: {Offset})", speciesNames.Count, offset);
                    
                    if (resultsArray.Count < limit)
                        break;
                    
                    offset += resultsArray.Count;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pokemon-species from PokeAPI");
                throw;
            }

            _logger.LogInformation("Total species fetched from pokemon-species endpoint: {Count}", speciesNames.Count);

            // Fetch the full Pokemon object for each species in parallel
            using var pokemonThrottler = new SemaphoreSlim(32); // Increased from MaxConcurrentRequests for faster parallel fetching
            var pokemonTasks = speciesNames.Select(async speciesName =>
            {
                await pokemonThrottler.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    return await _pokeApiClient.GetResourceAsync<Pokemon>(speciesName, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch pokemon {SpeciesName}", speciesName);
                    return null;
                }
                finally
                {
                    pokemonThrottler.Release();
                }
            });

            var pokemonArray = await Task.WhenAll(pokemonTasks).ConfigureAwait(false);
            var pokemonList = pokemonArray.Where(p => p is not null).Cast<Pokemon>().ToList();

            _allPokemon = pokemonList.OrderBy(p => p.Id).ToArray();
            _pokemonByName = _allPokemon.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
            _allPokemonJson = JsonSerializer.Serialize(_allPokemon, SerializerOptions);

            await _distributedCache.SetStringAsync(
                AllPokemonCacheKey,
                _allPokemonJson,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = null
                },
                CancellationToken.None).ConfigureAwait(false);

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

    private async Task<string> GetAllMovesJsonAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
       if (!forceRefresh && !string.IsNullOrWhiteSpace(_allMovesJson)) return _allMovesJson;

       await _warmupGate.WaitAsync(cancellationToken).ConfigureAwait(false);
       try
       {
           if (!forceRefresh && !string.IsNullOrWhiteSpace(_allMovesJson)) return _allMovesJson;

           if (!forceRefresh)
           {
               var cachedJson = await _distributedCache.GetStringAsync(AllMovesCacheKey, CancellationToken.None)
                   .ConfigureAwait(false);
               if (!string.IsNullOrWhiteSpace(cachedJson))
               {
                   _allMovesJson = cachedJson;
                   LastWarmupError = null;
                   _logger.LogInformation("Loaded moves catalog from Redis cache");
                   return _allMovesJson;
               }
           }

           _logger.LogInformation(forceRefresh
               ? "Refreshing moves catalog cache"
               : "Warming moves catalog cache");

           var resources = new List<NamedApiResource<Move>>();
           await foreach (var resource in _pokeApiClient.GetAllNamedResourcesAsync<Move>(cancellationToken)
                              .WithCancellation(cancellationToken)
                              .ConfigureAwait(false))
           {
               resources.Add(resource);
           }

           using var throttler = new SemaphoreSlim(MaxConcurrentRequests);
           var moveTasks = resources.Select(async resource =>
           {
               await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
               try
               {
                   return await _pokeApiClient.GetResourceAsync<Move>(resource.Name, cancellationToken)
                       .ConfigureAwait(false);
               }
               finally
               {
                   throttler.Release();
               }
           });

           var moves = await Task.WhenAll(moveTasks).ConfigureAwait(false);
           _allMoves = moves.OrderBy(m => m.Id).ToArray();
           _allMovesJson = JsonSerializer.Serialize(_allMoves, SerializerOptions);

           await _distributedCache.SetStringAsync(
               AllMovesCacheKey,
               _allMovesJson,
               new DistributedCacheEntryOptions
               {
                   AbsoluteExpirationRelativeToNow = null
               },
               CancellationToken.None).ConfigureAwait(false);

           LastWarmupError = null;

           _logger.LogInformation("Moves catalog cache warmed. Count={Count}", _allMoves.Count);
           return _allMovesJson;
       }
       catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
       {
           throw;
       }
       catch (Exception ex)
       {
           LastWarmupError = ex;
           _logger.LogError(ex, "Failed to warm moves catalog cache");
           throw;
       }
       finally
       {
           _warmupGate.Release();
       }
    }

    private async Task<string> GetAllAbilitiesJsonAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
       if (!forceRefresh && !string.IsNullOrWhiteSpace(_allAbilitiesJson)) return _allAbilitiesJson;

       await _warmupGate.WaitAsync(cancellationToken).ConfigureAwait(false);
       try
       {
           if (!forceRefresh && !string.IsNullOrWhiteSpace(_allAbilitiesJson)) return _allAbilitiesJson;

           if (!forceRefresh)
           {
               var cachedJson = await _distributedCache.GetStringAsync(AllAbilitiesCacheKey, CancellationToken.None)
                   .ConfigureAwait(false);
               if (!string.IsNullOrWhiteSpace(cachedJson))
               {
                   _allAbilitiesJson = cachedJson;
                   LastWarmupError = null;
                   _logger.LogInformation("Loaded abilities catalog from Redis cache");
                   return _allAbilitiesJson;
               }
           }

           _logger.LogInformation(forceRefresh
               ? "Refreshing abilities catalog cache"
               : "Warming abilities catalog cache");

           var resources = new List<NamedApiResource<Ability>>();
           await foreach (var resource in _pokeApiClient.GetAllNamedResourcesAsync<Ability>(cancellationToken)
                              .WithCancellation(cancellationToken)
                              .ConfigureAwait(false))
           {
               resources.Add(resource);
           }

           using var throttler = new SemaphoreSlim(MaxConcurrentRequests);
           var abilityTasks = resources.Select(async resource =>
           {
               await throttler.WaitAsync(cancellationToken).ConfigureAwait(false);
               try
               {
                   return await _pokeApiClient.GetResourceAsync<Ability>(resource.Name, cancellationToken)
                       .ConfigureAwait(false);
               }
               finally
               {
                   throttler.Release();
               }
           });

           var abilities = await Task.WhenAll(abilityTasks).ConfigureAwait(false);
           _allAbilities = abilities.OrderBy(a => a.Id).ToArray();
           _allAbilitiesJson = JsonSerializer.Serialize(_allAbilities, SerializerOptions);

           await _distributedCache.SetStringAsync(
               AllAbilitiesCacheKey,
               _allAbilitiesJson,
               new DistributedCacheEntryOptions
               {
                   AbsoluteExpirationRelativeToNow = null
               },
               CancellationToken.None).ConfigureAwait(false);

           LastWarmupError = null;

           _logger.LogInformation("Abilities catalog cache warmed. Count={Count}", _allAbilities.Count);
           return _allAbilitiesJson;
       }
       catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
       {
           throw;
       }
       catch (Exception ex)
       {
           LastWarmupError = ex;
           _logger.LogError(ex, "Failed to warm abilities catalog cache");
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

    private async Task<T?> GetCachedResourceAsync<T>(string cacheKey, Func<Task<T>> factory, CancellationToken cancellationToken)
    {
        // Use CancellationToken.None for cache operations to prevent cancellation from interrupting cache lookups
        var cachedJson = await _distributedCache.GetStringAsync(cacheKey, CancellationToken.None).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(cachedJson))
        {
            var cachedResource = JsonSerializer.Deserialize<T>(cachedJson, SerializerOptions);
            if (cachedResource is not null)
                return cachedResource;
        }

        var resource = await factory().ConfigureAwait(false);
        if (resource is null)
            return default;

        var json = JsonSerializer.Serialize(resource, SerializerOptions);
        await _distributedCache.SetStringAsync(
            cacheKey,
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = null
            },
            cancellationToken).ConfigureAwait(false);

        return resource;
    }

    private static int? GetResourceId(object resource)
    {
        if (resource.GetType().GetProperty("Url")?.GetValue(resource) is string url)
        {
            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
            {
                var lastSegment = uri.IsAbsoluteUri
                    ? uri.Segments.LastOrDefault()?.Trim('/')
                    : url.TrimEnd('/').Split('/').LastOrDefault();

                if (int.TryParse(lastSegment, out var id))
                    return id;
            }
        }

        return resource.GetType().GetProperty("Id")?.GetValue(resource) is int fallbackId
            ? fallbackId
            : null;
    }
}
