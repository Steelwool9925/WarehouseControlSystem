using WarehouseControl.Api.Services;
using WarehouseControl.Api.State;

namespace WarehouseControl.Api.Endpoints;

public sealed record CreateOrderRequest(string? CustomerReference, List<string>? Skus);

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders", (WarehouseState state) => Results.Ok(state.Orders.Values));

        app.MapGet("/api/orders/{id:guid}", (Guid id, WarehouseState state) =>
            state.Orders.TryGetValue(id, out var order)
                ? Results.Ok(order)
                : Results.NotFound());

        app.MapPost("/api/orders", (CreateOrderRequest request, DispatchService dispatch) =>
        {
            var result = dispatch.CreateOrder(request.CustomerReference, request.Skus);
            return result.IsSuccess
                ? Results.Created($"/api/orders/{result.Order!.Id}", result.Order)
                : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
