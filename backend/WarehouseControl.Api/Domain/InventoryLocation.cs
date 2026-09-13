namespace WarehouseControl.Api.Domain;

/// <summary>
/// A shelf location holding stock of one SKU. Quantity is mutable — it decrements each time a
/// pick task against this SKU completes.
/// </summary>
public sealed class InventoryLocation
{
    public required string Sku { get; init; }
    public required string Description { get; init; }
    public required GridPosition Position { get; init; }
    public int Quantity { get; set; }
}
