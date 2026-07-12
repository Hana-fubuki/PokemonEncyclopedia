using System.Net.Http;
using Microsoft.Extensions.Caching.Memory;
using PokemonEncyclopedia.Web;

// Setup
var httpClient = new HttpClient { BaseAddress = new Uri("https://pokeapi.co/api/v2/") };
var cache = new MemoryCache(new MemoryCacheOptions());
var client = new PokemonApiClient(httpClient, cache);

// Test cases
var testSpecies = new[] { "bulbasaur", "venusaur", "pikachu" };

foreach (var species in testSpecies)
{
    try
    {
        var varieties = await client.GetPokemonVarietiesAsync(species);
        Console.WriteLine($"\n✓ {species}:");
        Console.WriteLine($"  Count: {varieties.Count}");
        foreach (var v in varieties)
        {
            Console.WriteLine($"  - {v.Name} (ID: {v.Id})");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n✗ {species}: {ex.Message}");
    }
}

Console.WriteLine("\n✓ Varieties test complete!");
