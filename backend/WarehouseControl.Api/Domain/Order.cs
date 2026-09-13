namespace WarehouseControl.Api.Domain;

/// <summary>
/// A customer order, exploded into one <see cref="PickTask"/> per line item SKU.
/// </summary>
public sealed class Order
{
    public required Guid Id { get; init; }
    public string? CustomerReference { get; init; }
    public required List<string> LineItemSkus { get; init; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? FulfilledAt { get; set; }
}
