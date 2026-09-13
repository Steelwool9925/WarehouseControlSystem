using WarehouseControl.Api.Domain;
using WarehouseControl.Api.State;

namespace WarehouseControl.Api.Services;

/// <summary>
/// Drives the whole simulation: dispatches pending work and advances every in-flight robot one
/// grid cell, once per second.
/// </summary>
public sealed class FleetSimulationService(WarehouseState state, DispatchService dispatchService)
    : BackgroundService
{
    private const double BatteryDrainPerTick = 2;
    private const double BatteryChargePerTick = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            Tick();
        }
    }

    /// <summary>
    /// One simulation step: dispatch pending tasks, advance every in-flight robot one cell,
    /// and roll any order to <see cref="OrderStatus.Fulfilled"/> once all its tasks complete.
    /// Public and separate from <see cref="ExecuteAsync"/> so tests can call it directly without
    /// a real timer.
    /// </summary>
    public void Tick()
    {
        dispatchService.RunDispatchCycle();

        foreach (var robot in state.Robots.Values)
        {
            switch (robot.Status)
            {
                case RobotStatus.MovingToPick:
                    StepTowardTask(robot);
                    break;
                case RobotStatus.ReturningToStation:
                case RobotStatus.Charging:
                    StepTowardHome(robot);
                    break;
            }
        }

        FulfillCompletedOrders();
    }

    private void StepTowardTask(Robot robot)
    {
        if (robot.CurrentTaskId is not { } taskId || !state.Tasks.TryGetValue(taskId, out var task))
        {
            return; // defensive: a MovingToPick robot should always have a live task
        }

        if (robot.Position != task.TargetLocation)
        {
            robot.Position = robot.Position.StepToward(task.TargetLocation);
            robot.BatteryPercent = Math.Max(0, robot.BatteryPercent - BatteryDrainPerTick);
        }

        if (robot.Position != task.TargetLocation)
        {
            return; // still traveling
        }

        task.Status = PickTaskStatus.Completed;
        task.CompletedAt = DateTimeOffset.UtcNow;

        if (state.Inventory.TryGetValue(task.Sku, out var inventoryLocation))
        {
            inventoryLocation.Quantity = Math.Max(0, inventoryLocation.Quantity - 1);
        }

        robot.CurrentTaskId = null;
        robot.Status = robot.BatteryPercent < DispatchService.LowBatteryThreshold
            ? RobotStatus.Charging
            : RobotStatus.ReturningToStation;
    }

    private void StepTowardHome(Robot robot)
    {
        if (robot.Position != robot.HomeStation)
        {
            robot.Position = robot.Position.StepToward(robot.HomeStation);
            robot.BatteryPercent = Math.Max(0, robot.BatteryPercent - BatteryDrainPerTick);
        }

        if (robot.Position != robot.HomeStation)
        {
            return; // still traveling
        }

        // Parked at home this tick (whether it arrived just now or was already there).
        if (robot.Status == RobotStatus.Charging)
        {
            robot.BatteryPercent = Math.Min(100, robot.BatteryPercent + BatteryChargePerTick);
            if (robot.BatteryPercent >= 100)
            {
                robot.Status = RobotStatus.Idle;
            }
        }
        else
        {
            robot.Status = RobotStatus.Idle;
        }
    }

    private void FulfillCompletedOrders()
    {
        foreach (var order in state.Orders.Values)
        {
            if (order.Status == OrderStatus.Fulfilled)
            {
                continue;
            }

            var hasAnyTask = false;
            var allCompleted = true;

            foreach (var task in state.Tasks.Values)
            {
                if (task.OrderId != order.Id)
                {
                    continue;
                }

                hasAnyTask = true;
                if (task.Status != PickTaskStatus.Completed)
                {
                    allCompleted = false;
                    break;
                }
            }

            if (hasAnyTask && allCompleted)
            {
                order.Status = OrderStatus.Fulfilled;
                order.FulfilledAt = DateTimeOffset.UtcNow;
            }
        }
    }
}
