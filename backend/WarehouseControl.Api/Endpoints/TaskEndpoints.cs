using WarehouseControl.Api.State;

namespace WarehouseControl.Api.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/tasks", (WarehouseState state) => Results.Ok(state.Tasks.Values));
        return app;
    }
}
