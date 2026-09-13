using WarehouseControl.Api.Services;

namespace WarehouseControl.Api.Endpoints;

public static class DispatchEndpoints
{
    public static IEndpointRouteBuilder MapDispatchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/dispatch/run", (DispatchService dispatch) =>
        {
            var assignedCount = dispatch.RunDispatchCycle();
            return Results.Ok(new { assignedCount });
        });

        return app;
    }
}
