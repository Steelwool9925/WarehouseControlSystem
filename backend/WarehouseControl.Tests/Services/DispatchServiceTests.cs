using WarehouseControl.Api.Domain;
using WarehouseControl.Api.Services;
using WarehouseControl.Api.State;

namespace WarehouseControl.Tests.Services;

public class DispatchServiceTests
{
    private static (WarehouseState State, DispatchService Dispatch) NewSeededSystem()
    {
        var state = new WarehouseState();
        state.Seed();
        return (state, new DispatchService(state));
    }

    [Fact]
    public void CreateOrder_RejectsUnknownSku_WithNoSideEffects()
    {
        var (state, dispatch) = NewSeededSystem();

        var result = dispatch.CreateOrder("customer-1", ["SKU-001", "NOT-A-REAL-SKU"]);

        Assert.False(result.IsSuccess);
        Assert.Contains("NOT-A-REAL-SKU", result.Error);
        Assert.Empty(state.Orders);
        Assert.Empty(state.Tasks);
    }

    [Fact]
    public void CreateOrder_RejectsEmptySkuList()
    {
        var (_, dispatch) = NewSeededSystem();

        var result = dispatch.CreateOrder("customer-1", []);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void CreateOrder_RejectsNullSkuList_WithNoSideEffects()
    {
        // Reflects a request body that omits "skus" entirely, or sends it as JSON null —
        // System.Text.Json binds that to a null list, not an empty one.
        var (state, dispatch) = NewSeededSystem();

        var result = dispatch.CreateOrder("customer-1", null);

        Assert.False(result.IsSuccess);
        Assert.Empty(state.Orders);
        Assert.Empty(state.Tasks);
    }

    [Fact]
    public void CreateOrder_RejectsNullOrBlankSkuElement()
    {
        var (_, dispatch) = NewSeededSystem();

        var result = dispatch.CreateOrder("customer-1", ["SKU-001", null!, "SKU-002"]);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void CreateOrder_ValidOrder_CreatesOneTaskPerLineItem()
    {
        var (state, dispatch) = NewSeededSystem();

        var result = dispatch.CreateOrder("customer-1", ["SKU-001", "SKU-002", "SKU-003"]);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, state.Tasks.Count);
        Assert.All(state.Tasks.Values, t => Assert.Equal(result.Order!.Id, t.OrderId));
        Assert.All(state.Tasks.Values, t => Assert.Equal(PickTaskStatus.Pending, t.Status));
    }

    [Fact]
    public void RunDispatchCycle_AssignsNearestIdleRobot()
    {
        var state = new WarehouseState();
        var near = MakeRobot(new GridPosition(1, 1));
        var far = MakeRobot(new GridPosition(9, 9));
        state.Robots[near.Id] = near;
        state.Robots[far.Id] = far;
        var task = MakeTask(state, new GridPosition(2, 1));
        var dispatch = new DispatchService(state);

        var assigned = dispatch.RunDispatchCycle();

        Assert.Equal(1, assigned);
        Assert.Equal(near.Id, task.AssignedRobotId);
        Assert.Equal(RobotStatus.MovingToPick, near.Status);
        Assert.Equal(RobotStatus.Idle, far.Status);
    }

    [Fact]
    public void RunDispatchCycle_ExcludesRobotsBelowBatteryThreshold()
    {
        var state = new WarehouseState();
        var lowBattery = MakeRobot(new GridPosition(1, 1));
        lowBattery.BatteryPercent = DispatchService.LowBatteryThreshold - 1;
        var healthy = MakeRobot(new GridPosition(9, 9));
        state.Robots[lowBattery.Id] = lowBattery;
        state.Robots[healthy.Id] = healthy;
        var task = MakeTask(state, new GridPosition(2, 1));
        var dispatch = new DispatchService(state);

        dispatch.RunDispatchCycle();

        Assert.Equal(healthy.Id, task.AssignedRobotId);
    }

    [Fact]
    public void RunDispatchCycle_BreaksTiesDeterministicallyByRobotId()
    {
        var state = new WarehouseState();
        var a = MakeRobot(new GridPosition(0, 0));
        var b = MakeRobot(new GridPosition(0, 0));
        state.Robots[a.Id] = a;
        state.Robots[b.Id] = b;
        var task = MakeTask(state, new GridPosition(3, 3));
        var dispatch = new DispatchService(state);
        var expectedWinner = a.Id.CompareTo(b.Id) < 0 ? a.Id : b.Id;

        dispatch.RunDispatchCycle();

        Assert.Equal(expectedWinner, task.AssignedRobotId);
    }

    [Fact]
    public void RunDispatchCycle_NeverDoubleAssignsARobotInOneCycle()
    {
        var state = new WarehouseState();
        var robot = MakeRobot(new GridPosition(0, 0));
        state.Robots[robot.Id] = robot;
        var task1 = MakeTask(state, new GridPosition(1, 0));
        var task2 = MakeTask(state, new GridPosition(0, 1));
        var dispatch = new DispatchService(state);

        var assigned = dispatch.RunDispatchCycle();

        Assert.Equal(1, assigned);
        var assignedCount = 0;
        if (task1.Status == PickTaskStatus.Assigned) assignedCount++;
        if (task2.Status == PickTaskStatus.Assigned) assignedCount++;
        Assert.Equal(1, assignedCount);
    }

    [Fact]
    public void RunDispatchCycle_OverLargeDataset_AllocatesWithinBudget()
    {
        // Warm up the JIT on a small, separate dataset first.
        var (warmState, warmDispatch) = BuildBulkDataset(robotCount: 5, taskCount: 5);
        warmDispatch.RunDispatchCycle();
        _ = warmState;

        var (state, dispatch) = BuildBulkDataset(robotCount: 20, taskCount: 100);

        var before = GC.GetAllocatedBytesForCurrentThread();
        dispatch.RunDispatchCycle();
        var after = GC.GetAllocatedBytesForCurrentThread();

        // A `.Where(...).ToList()`-based implementation allocates a delegate, a Where
        // iterator, and a buffer on top of the same-sized result list; this foreach-based
        // implementation allocates only the result List<PickTask> and the claimed-robots
        // HashSet<Guid> — both proportional to N, but without the extra LINQ layers. Measured
        // at ~29KB for this exact 100-task/20-robot input; 45KB gives headroom for minor JIT/GC
        // variance while still failing hard if a LINQ-chain-heavy rewrite roughly doubles it.
        Assert.True(
            after - before < 45_000,
            $"RunDispatchCycle allocated {after - before} bytes for 100 tasks/20 robots, expected < 45,000.");
    }

    private static (WarehouseState State, DispatchService Dispatch) BuildBulkDataset(
        int robotCount, int taskCount)
    {
        var state = new WarehouseState();
        for (var i = 0; i < robotCount; i++)
        {
            var robot = MakeRobot(new GridPosition(i % WarehouseState.GridSize, 0));
            state.Robots[robot.Id] = robot;
        }

        for (var i = 0; i < taskCount; i++)
        {
            MakeTask(state, new GridPosition(i % WarehouseState.GridSize, i % WarehouseState.GridSize));
        }

        return (state, new DispatchService(state));
    }

    private static Robot MakeRobot(GridPosition position) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Robot",
        HomeStation = position,
        Position = position,
    };

    private static PickTask MakeTask(WarehouseState state, GridPosition target)
    {
        var task = new PickTask
        {
            Id = Guid.NewGuid(),
            OrderId = Guid.NewGuid(),
            Sku = "TEST-SKU",
            TargetLocation = target,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        state.Tasks[task.Id] = task;
        return task;
    }
}
