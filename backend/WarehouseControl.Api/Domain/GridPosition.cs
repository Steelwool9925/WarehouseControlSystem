using System.Runtime.CompilerServices;

namespace WarehouseControl.Api.Domain;

/// <summary>
/// An integer coordinate on the warehouse floor grid.
/// </summary>
public readonly record struct GridPosition(int X, int Y)
{
    /// <summary>
    /// The Manhattan (grid) distance to another position.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ManhattanDistanceTo(GridPosition other)
    {
        // Widen to long before subtracting/Math.Abs: int.MinValue - int.MaxValue (or
        // Math.Abs(int.MinValue) alone) overflows checked int arithmetic. Grid coordinates are
        // always small in practice, but the math itself stays correct at any int input.
        long dx = (long)X - other.X;
        long dy = (long)Y - other.Y;
        return (int)(Math.Abs(dx) + Math.Abs(dy));
    }

    /// <summary>
    /// Moves one grid cell toward <paramref name="target"/>, stepping along the axis with the
    /// larger remaining delta first; ties are broken toward the X axis. Returns the current
    /// position unchanged once <paramref name="target"/> is reached.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GridPosition StepToward(GridPosition target)
    {
        long dx = (long)target.X - X;
        long dy = (long)target.Y - Y;

        if (dx == 0 && dy == 0)
        {
            return this;
        }

        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            return this with { X = X + Math.Sign(dx) };
        }

        return this with { Y = Y + Math.Sign(dy) };
    }
}
