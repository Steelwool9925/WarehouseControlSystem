using WarehouseControl.Api.Domain;

namespace WarehouseControl.Tests.Domain;

public class GridPositionTests
{
    [Theory]
    [InlineData(0, 0, 3, 4, 7)]
    [InlineData(5, 5, 5, 5, 0)]
    [InlineData(-2, -3, 2, 3, 10)]
    public void ManhattanDistanceTo_ComputesGridDistance(int x1, int y1, int x2, int y2, int expected)
    {
        var a = new GridPosition(x1, y1);
        var b = new GridPosition(x2, y2);

        Assert.Equal(expected, a.ManhattanDistanceTo(b));
    }

    [Fact]
    public void StepToward_MovesOneCellAlongLargerDelta()
    {
        var start = new GridPosition(0, 0);
        var target = new GridPosition(3, 1);

        var next = start.StepToward(target);

        Assert.Equal(new GridPosition(1, 0), next);
    }

    [Fact]
    public void StepToward_BreaksTiesTowardX()
    {
        var start = new GridPosition(0, 0);
        var target = new GridPosition(2, 2);

        var next = start.StepToward(target);

        Assert.Equal(new GridPosition(1, 0), next);
    }

    [Fact]
    public void StepToward_ReachesTargetOverRepeatedSteps()
    {
        var position = new GridPosition(0, 0);
        var target = new GridPosition(4, -3);

        for (var i = 0; i < 20 && position != target; i++)
        {
            position = position.StepToward(target);
        }

        Assert.Equal(target, position);
    }

    [Fact]
    public void StepToward_ReturnsSamePositionOnceAtTarget()
    {
        var position = new GridPosition(5, 5);

        var next = position.StepToward(position);

        Assert.Equal(position, next);
    }

    [Fact]
    public void HotPathMethods_AllocateNoHeapMemory()
    {
        var a = new GridPosition(0, 0);
        var b = new GridPosition(37, -19);

        // Warm up the JIT before measuring.
        for (var i = 0; i < 1000; i++)
        {
            _ = a.ManhattanDistanceTo(b);
            _ = a.StepToward(b);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 10_000; i++)
        {
            _ = a.ManhattanDistanceTo(b);
            _ = a.StepToward(b);
        }

        var after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, after - before);
    }
}
