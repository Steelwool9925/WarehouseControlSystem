using WarehouseControl.Api.Domain;
using WarehouseControl.Api.Services;
using WarehouseControl.Api.State;

namespace WarehouseControl.Tests.Services;

public class FleetSimulationServiceTests
{
    private const int MaxTicks = 50;

    [Fact]
    public void Tick_FullPickAndReturnCycle_HealthyBattery_CompletesAndReturnsIdle()
    {
        var state = new WarehouseState();
        var robot = new Robot
        {
            Id = Guid.NewGuid(),
            Name = "R1",
            HomeStation = new GridPosition(0, 0),
            Position = new GridPosition(0, 0),
            BatteryPercent = 100,
        };
        state.Robots[robot.Id] = robot;
        state.Inventory["SKU-X"] = new InventoryLocation
        {
            Sku = "SKU-X",
            Description = "Test widget",
            Position = new GridPosition(2, 0),
            Quantity = 5,
        };
        var dispatch = new DispatchService(state);
        var sim = new FleetSimulationService(state, dispatch);

        var orderResult = dispatch.CreateOrder("cust-1", ["SKU-X"]);
        Assert.True(orderResult.IsSuccess);
        var order = orderResult.Order!;
        var task = Assert.Single(state.Tasks.Values);

        // Tick 1: dispatch assigns the robot, then the same tick steps it one cell toward the
        // target — dispatch and movement happen in the same Tick(), not staggered across ticks.
        sim.Tick();
        Assert.Equal(RobotStatus.MovingToPick, robot.Status);
        Assert.Equal(new GridPosition(1, 0), robot.Position);
        Assert.Equal(PickTaskStatus.Assigned, task.Status);

        // Tick 2: robot reaches the target, completes the task, inventory decrements, order
        // fulfills immediately (it doesn't wait for the robot to get home).
        sim.Tick();
        Assert.Equal(PickTaskStatus.Completed, task.Status);
        Assert.Equal(4, state.Inventory["SKU-X"].Quantity);
        Assert.Equal(OrderStatus.Fulfilled, order.Status);
        Assert.NotNull(order.FulfilledAt);
        Assert.Equal(RobotStatus.ReturningToStation, robot.Status);

        // Keep ticking until the robot makes it home and goes idle.
        var ticks = 0;
        while (robot.Status != RobotStatus.Idle && ticks < MaxTicks)
        {
            sim.Tick();
            ticks++;
        }

        Assert.Equal(RobotStatus.Idle, robot.Status);
        Assert.Equal(robot.HomeStation, robot.Position);
    }

    [Fact]
    public void Tick_FullPickAndReturnCycle_LowBatteryAfterPick_ChargesAtHomeThenIdle()
    {
        var state = new WarehouseState();
        var robot = new Robot
        {
            Id = Guid.NewGuid(),
            Name = "R1",
            HomeStation = new GridPosition(0, 0),
            Position = new GridPosition(0, 0),
            // Drains 2%/tick while traveling; 2 ticks to reach the target at (2,0) leaves this
            // robot at 19% on arrival — below the 20% threshold, so it should route home to
            // Charging instead of ReturningToStation.
            BatteryPercent = 23,
        };
        state.Robots[robot.Id] = robot;
        state.Inventory["SKU-X"] = new InventoryLocation
        {
            Sku = "SKU-X",
            Description = "Test widget",
            Position = new GridPosition(2, 0),
            Quantity = 5,
        };
        var dispatch = new DispatchService(state);
        var sim = new FleetSimulationService(state, dispatch);
        dispatch.CreateOrder("cust-1", ["SKU-X"]);
        var task = Assert.Single(state.Tasks.Values);

        sim.Tick(); // assign + first step
        sim.Tick(); // arrives, completes task, battery drops to 19% -> routes to Charging

        Assert.Equal(PickTaskStatus.Completed, task.Status);
        Assert.Equal(RobotStatus.Charging, robot.Status);
        Assert.True(robot.BatteryPercent < DispatchService.LowBatteryThreshold);

        var ticks = 0;
        while (robot.Status != RobotStatus.Idle && ticks < MaxTicks)
        {
            sim.Tick();
            ticks++;
        }

        Assert.Equal(RobotStatus.Idle, robot.Status);
        Assert.Equal(robot.HomeStation, robot.Position);
        // A robot only leaves Charging once it reaches full battery.
        Assert.Equal(100, robot.BatteryPercent);
    }

    [Fact]
    public void Tick_WithNoPendingWork_IsANoOp()
    {
        var state = new WarehouseState();
        state.Seed();
        var dispatch = new DispatchService(state);
        var sim = new FleetSimulationService(state, dispatch);

        sim.Tick();

        Assert.All(state.Robots.Values, r => Assert.Equal(RobotStatus.Idle, r.Status));
        Assert.All(state.Robots.Values, r => Assert.Equal(r.HomeStation, r.Position));
    }
}
