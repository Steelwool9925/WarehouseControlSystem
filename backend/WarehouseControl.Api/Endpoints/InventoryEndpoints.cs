using WarehouseControl.Api.State;

namespace WarehouseControl.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/inventory", (WarehouseState state) => Results.Ok(state.Inventory.Values));
        return app;
    }
}
