using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using PokeApiNet;

namespace PokemonEncyclopedia.Web;

public sealed class PokemonApiClient(HttpClient httpClient, IMemoryCache cache)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private const string AllPokemonCacheKey = "pokemon:all";

    public async Task<IReadOnlyList<Pokemon>> GetAllPokemonAsync(CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(AllPokemonCacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var pokemon = await httpClient.GetFromJsonAsync<Pokemon[]>("/api/PokeApi", cancellationToken)
                .ConfigureAwait(false);
            return (IReadOnlyList<Pokemon>)(pokemon ?? Array.Empty<Pokemon>());
        }).ConfigureAwait(false) ?? Array.Empty<Pokemon>();
    }

    public async Task<Pokemon?> GetPokemonAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedName)) return null;

        var cacheKey = $"pokemon:{normalizedName}";
        if (cache.TryGetValue(cacheKey, out Pokemon? cachedPokemon))
            return cachedPokemon;

        var allPokemon = await GetAllPokemonAsync(cancellationToken).ConfigureAwait(false);
        var fromList = allPokemon.FirstOrDefault(p => string.Equals(p.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
        if (fromList is not null)
        {
            cache.Set(cacheKey, fromList, CacheDuration);
            return fromList;
        }

        var pokemon = await httpClient.GetFromJsonAsync<Pokemon>($"/api/PokeApi/pokemon/{Uri.EscapeDataString(normalizedName)}", cancellationToken)
            .ConfigureAwait(false);
        if (pokemon is not null)
            cache.Set(cacheKey, pokemon, CacheDuration);

        return pokemon;
    }
}
