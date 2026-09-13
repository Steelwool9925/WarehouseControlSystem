using WarehouseControl.Api.Services;
using WarehouseControl.Api.State;

namespace WarehouseControl.Api.Endpoints;

public static class KpiEndpoints
{
    public static IEndpointRouteBuilder MapKpiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/kpis", (WarehouseState state) => Results.Ok(KpiCalculator.Compute(state)));
        return app;
    }
}
