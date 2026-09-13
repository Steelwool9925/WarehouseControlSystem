namespace WarehouseControl.Api.Domain;

/// <summary>
/// One line item of an order: fetch <see cref="Sku"/> from <see cref="TargetLocation"/>.
/// </summary>
public sealed class PickTask
{
    public required Guid Id { get; init; }
    public required Guid OrderId { get; init; }
    public required string Sku { get; init; }
    public required GridPosition TargetLocation { get; init; }
    public PickTaskStatus Status { get; set; } = PickTaskStatus.Pending;
    public Guid? AssignedRobotId { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? AssignedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
