using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WarehouseControl.Api.Domain;
using WarehouseControl.Api.Endpoints;

namespace WarehouseControl.Tests.Endpoints;

public class EndpointsTests
{
    private static readonly JsonSerializerOptions CaseInsensitive =
        new() { PropertyNameCaseInsensitive = true };

    // Each test gets its own factory (and so its own WarehouseState singleton instance) — the
    // in-memory state would otherwise leak mutations (created orders, dispatched robots) between
    // tests sharing one factory, per the plan's own contingency note.
    private static WebApplicationFactory<Program> NewFactory() => new();

    [Fact]
    public async Task GetRobots_ReturnsSeededFleetWithStringStatus()
    {
        await using var factory = NewFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/robots");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        var robots = doc.RootElement;
        Assert.Equal(4, robots.GetArrayLength());
        Assert.Equal("Idle", robots[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetInventory_ReturnsSeededCatalogue()
    {
        await using var factory = NewFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/inventory");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(8, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetTasks_InitiallyEmpty()
    {
        await using var factory = NewFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/tasks");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task PostOrders_HappyPath_Returns201WithLocationAndOrder()
    {
        await using var factory = NewFactory();
        var client = factory.CreateClient();
        var request = new CreateOrderRequest("cust-1", ["SKU-001", "SKU-002"]);

        var response = await client.PostAsJsonAsync("/api/orders", request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = body.GetProperty("id").GetGuid();
        Assert.Equal($"/api/orders/{id}", response.Headers.Location!.OriginalString);
        Assert.Equal("Pending", body.GetProperty("status").GetString());
        Assert.Equal(2, body.GetProperty("lineItemSkus").GetArrayLength());
    }

    [Fact]
    public async Task PostOrders_MissingSkusField_Returns400_NotA500()
    {
        await using var factory = NewFactory();
        var client = factory.CreateClient();

        // A body that omits "skus" entirely — System.Text.Json binds this to a null list.
        var response = await client.PostAsync(
            "/api/orders",
            JsonContent.Create(new { customerReference = "cust-1" }));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(body.GetProperty("error").GetString()));
    }

    [Fact]
    public async Task PostOrders_UnknownSku_Returns400WithError()
    {
        await using var factory = NewFactory();
        var client = factory.CreateClient();
        var request = new CreateOrderRequest("cust-1", ["NOT-A-REAL-SKU"]);

        var response = await client.PostAsJsonAsync("/api/orders", request);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("NOT-A-REAL-SKU", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task GetOrderById_Existing_Returns200WithMatchingOrder()
    {
        await using var factory = NewFactory();
        var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync(
            "/api/orders", new CreateOrderRequest("cust-1", ["SKU-001"]));
        var createdBody = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = createdBody.GetProperty("id").GetGuid();

        var response = await client.GetAsync($"/api/orders/{id}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(id, body.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task GetOrderById_Missing_Returns404()
    {
        await using var factory = NewFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostDispatchRun_AfterOrderCreated_AssignsAtLeastOneTask()
    {
        await using var factory = NewFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/orders", new CreateOrderRequest("cust-1", ["SKU-001"]));

        var response = await client.PostAsync("/api/dispatch/run", content: null);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.GetProperty("assignedCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task GetKpis_ShapeIsSane_AndAveragePickTimeIsNullWithNoCompletions()
    {
        await using var factory = NewFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/kpis");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var totalRobots = body.GetProperty("activeRobots").GetInt32() +
            body.GetProperty("idleRobots").GetInt32() +
            body.GetProperty("chargingRobots").GetInt32();
        Assert.Equal(4, totalRobots);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("averagePickTimeSeconds").ValueKind);
    }
}
