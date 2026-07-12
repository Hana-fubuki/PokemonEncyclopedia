using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using PokemonEncyclopedia.Web;

class VarietiesDebugTest
{
    static async Task Main()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("https://pokeapi.co/api/v2/") };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var client = new PokemonApiClient(httpClient, cache);

        Console.WriteLine("=== Testing GetPokemonVarietiesAsync ===\n");

        // Test 1: Venusaur (should have 3 varieties)
        Console.WriteLine("Test 1: venusaur (should have 3 varieties)");
        try
        {
            var venusaurVarieties = await client.GetPokemonVarietiesAsync("venusaur");
            Console.WriteLine($"  Result: {venusaurVarieties.Count} varieties found");
            foreach (var v in venusaurVarieties)
            {
                Console.WriteLine($"    - {v.Name} (ID: {v.Id})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
            Console.WriteLine($"  Stack: {ex.StackTrace}");
        }

        Console.WriteLine("\nTest 2: bulbasaur (should have 1 variety)");
        try
        {
            var bulbasaurVarieties = await client.GetPokemonVarietiesAsync("bulbasaur");
            Console.WriteLine($"  Result: {bulbasaurVarieties.Count} varieties found");
            foreach (var v in bulbasaurVarieties)
            {
                Console.WriteLine($"    - {v.Name} (ID: {v.Id})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
        }

        Console.WriteLine("\nTest 3: Get Pokemon for venusaur-mega");
        try
        {
            var pokemon = await client.GetPokemonAsync("venusaur-mega");
            if (pokemon != null)
            {
                Console.WriteLine($"  Pokemon Name: {pokemon.Name}");
                Console.WriteLine($"  Species Name: {pokemon.Species?.Name}");
            }
            else
            {
                Console.WriteLine("  Pokemon not found!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR: {ex.Message}");
        }

        Console.WriteLine("\n=== Test Complete ===");
    }
}
