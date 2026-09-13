using WarehouseControl.Api.State;

namespace WarehouseControl.Api.Endpoints;

public static class RobotEndpoints
{
    public static IEndpointRouteBuilder MapRobotEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/robots", (WarehouseState state) => Results.Ok(state.Robots.Values));
        return app;
    }
}
