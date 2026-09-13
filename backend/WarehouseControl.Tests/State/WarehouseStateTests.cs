using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using WarehouseControl.Api.Domain;
using WarehouseControl.Api.State;

namespace WarehouseControl.Tests.State;

public class WarehouseStateTests
{
    [Fact]
    public void Seed_PopulatesFourRobotsAndEightInventoryLocations()
    {
        var state = new WarehouseState();

        state.Seed();

        Assert.Equal(4, state.Robots.Count);
        Assert.Equal(8, state.Inventory.Count);
        Assert.All(state.Robots.Values, r => Assert.Equal(r.HomeStation, r.Position));
    }

    [Fact]
    public void Inventory_LookupIsCaseInsensitive()
    {
        var state = new WarehouseState();
        state.Seed();

        Assert.True(state.Inventory.TryGetValue("sku-001", out var location));
        Assert.Equal("SKU-001", location!.Sku);
        Assert.True(state.Inventory.ContainsKey("Sku-001"));
    }

    [Fact]
    public void Inventory_CaseInsensitiveLookup_AllocatesNoHeapMemory()
    {
        var state = new WarehouseState();
        state.Seed();
        var keysToLookUp = new[] { "sku-001", "SKU-002", "Sku-003", "sKu-004" };

        // Warm up the JIT before measuring.
        foreach (var key in keysToLookUp)
        {
            state.Inventory.TryGetValue(key, out _);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 10_000; i++)
        {
            state.Inventory.TryGetValue(keysToLookUp[i % keysToLookUp.Length], out _);
        }

        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, after - before);
    }

    [Fact]
    public void NaiveToLowerInvariantLookup_AllocatesMemory_ForComparison()
    {
        // Demonstrates what the OrdinalIgnoreCase-keyed dictionary avoids: a naive
        // case-insensitive lookup that lowercases the key on every call allocates a new
        // string each time.
        var plainDictionary = new Dictionary<string, int> { ["sku-001"] = 1 };

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 1_000; i++)
        {
            _ = plainDictionary.TryGetValue("SKU-001".ToLowerInvariant(), out _);
        }

        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.True(after - before > 0);
    }

    [Fact]
    public async Task Host_SerializesEnumsAsStrings()
    {
        await using var factory = new WebApplicationFactory<Program>();

        // No endpoint returns a Robot yet (that lands in the rest-endpoints plan) — assert
        // against the app's actual configured JsonSerializerOptions instead, resolved from the
        // running host's DI container, so this test verifies the real configuration rather than
        // a freshly-constructed one that could drift from it.
        var options = factory.Services
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()
            .Value.SerializerOptions;

        var json = JsonSerializer.Serialize(RobotStatus.Idle, options);

        Assert.Equal("\"Idle\"", json);
    }

    [Fact]
    public async Task Host_AllowsCorsFromFrontendDevOrigin()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Options, "/");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(
            "http://localhost:5173",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
