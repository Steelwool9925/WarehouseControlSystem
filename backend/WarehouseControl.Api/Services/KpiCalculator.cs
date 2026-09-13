using WarehouseControl.Api.Domain;
using WarehouseControl.Api.State;

namespace WarehouseControl.Api.Services;

public sealed record KpiResponse(
    int ActiveRobots,
    int IdleRobots,
    int ChargingRobots,
    int PendingOrders,
    int InProgressOrders,
    int FulfilledOrders,
    double? AveragePickTimeSeconds);

/// <summary>
/// Aggregates the KPIs shown on the control-room dashboard. One foreach pass per collection —
/// deliberately not a chain of separate LINQ <c>.Count(predicate)</c> calls, each of which would
/// re-enumerate the same (potentially large) collection from scratch.
/// </summary>
public static class KpiCalculator
{
    public static KpiResponse Compute(WarehouseState state)
    {
        var activeRobots = 0;
        var idleRobots = 0;
        var chargingRobots = 0;
        foreach (var robot in state.Robots.Values)
        {
            switch (robot.Status)
            {
                case RobotStatus.Idle:
                    idleRobots++;
                    break;
                case RobotStatus.Charging:
                    chargingRobots++;
                    break;
                default: // MovingToPick or ReturningToStation
                    activeRobots++;
                    break;
            }
        }

        var pendingOrders = 0;
        var inProgressOrders = 0;
        var fulfilledOrders = 0;
        foreach (var order in state.Orders.Values)
        {
            switch (order.Status)
            {
                case OrderStatus.Pending:
                    pendingOrders++;
                    break;
                case OrderStatus.InProgress:
                    inProgressOrders++;
                    break;
                case OrderStatus.Fulfilled:
                    fulfilledOrders++;
                    break;
            }
        }

        var totalPickSeconds = 0.0;
        var completedTaskCount = 0;
        foreach (var task in state.Tasks.Values)
        {
            if (task.Status == PickTaskStatus.Completed &&
                task.AssignedAt is { } assignedAt &&
                task.CompletedAt is { } completedAt)
            {
                totalPickSeconds += (completedAt - assignedAt).TotalSeconds;
                completedTaskCount++;
            }
        }

        double? averagePickTimeSeconds = completedTaskCount > 0
            ? totalPickSeconds / completedTaskCount
            : null;

        return new KpiResponse(
            activeRobots,
            idleRobots,
            chargingRobots,
            pendingOrders,
            inProgressOrders,
            fulfilledOrders,
            averagePickTimeSeconds);
    }
}
