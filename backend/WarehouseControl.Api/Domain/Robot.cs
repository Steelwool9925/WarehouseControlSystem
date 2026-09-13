namespace WarehouseControl.Api.Domain;

/// <summary>
/// A pick robot on the warehouse floor. Mutable — position, status, battery and current task
/// change every simulation tick while the robot is stored in <c>WarehouseState</c>.
/// </summary>
public sealed class Robot
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required GridPosition HomeStation { get; init; }
    public required GridPosition Position { get; set; }
    public RobotStatus Status { get; set; } = RobotStatus.Idle;
    public double BatteryPercent { get; set; } = 100;
    public Guid? CurrentTaskId { get; set; }
}
