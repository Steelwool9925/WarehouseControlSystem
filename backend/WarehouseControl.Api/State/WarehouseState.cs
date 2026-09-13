using System.Collections.Concurrent;
using WarehouseControl.Api.Domain;

namespace WarehouseControl.Api.State;

/// <summary>
/// The whole app's in-memory data store: no database, per the design spec. Registered as a
/// singleton; every field is a thread-safe collection since it's read and mutated from HTTP
/// request handlers and the background simulation tick concurrently.
/// </summary>
public sealed class WarehouseState
{
    public const int GridSize = 10;

    public ConcurrentDictionary<Guid, Robot> Robots { get; } = new();

    // Keyed with OrdinalIgnoreCase so SKU lookups are case-insensitive without allocating a
    // ToLowerInvariant() copy of the key on every call.
    public ConcurrentDictionary<string, InventoryLocation> Inventory { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public ConcurrentDictionary<Guid, PickTask> Tasks { get; } = new();

    public ConcurrentDictionary<Guid, Order> Orders { get; } = new();

    /// <summary>
    /// Populates the demo fleet (4 robots at the grid's four corners) and inventory catalogue
    /// (8 SKUs at distinct interior positions). Idempotent — a second call is a silent no-op
    /// rather than duplicating robots/inventory, since nothing else here enforces single-call
    /// usage at the DI-registration site.
    /// </summary>
    public void Seed()
    {
        if (!Robots.IsEmpty || !Inventory.IsEmpty)
        {
            return;
        }

        var homeStations = new[]
        {
            new GridPosition(0, 0),
            new GridPosition(GridSize - 1, 0),
            new GridPosition(0, GridSize - 1),
            new GridPosition(GridSize - 1, GridSize - 1),
        };
        var robotNames = new[] { "Atlas", "Bolt", "Cog", "Dash" };

        for (var i = 0; i < homeStations.Length; i++)
        {
            var robot = new Robot
            {
                Id = Guid.NewGuid(),
                Name = robotNames[i],
                HomeStation = homeStations[i],
                Position = homeStations[i],
            };
            Robots[robot.Id] = robot;
        }

        var skuSeeds = new (string Sku, string Description, GridPosition Position, int Quantity)[]
        {
            ("SKU-001", "M8 Hex Bolts (box of 100)", new GridPosition(2, 2), 40),
            ("SKU-002", "M8 Hex Nuts (box of 100)", new GridPosition(4, 2), 40),
            ("SKU-003", "Ball Bearing 608ZZ", new GridPosition(6, 2), 60),
            ("SKU-004", "Aluminum L-Bracket", new GridPosition(2, 4), 25),
            ("SKU-005", "Nitrile Gloves (box)", new GridPosition(4, 4), 15),
            ("SKU-006", "Cable Ties (pack of 200)", new GridPosition(6, 4), 30),
            ("SKU-007", "USB-C Cable 1m", new GridPosition(2, 6), 50),
            ("SKU-008", "Anti-Static Wrist Strap", new GridPosition(6, 6), 20),
        };

        foreach (var seed in skuSeeds)
        {
            Inventory[seed.Sku] = new InventoryLocation
            {
                Sku = seed.Sku,
                Description = seed.Description,
                Position = seed.Position,
                Quantity = seed.Quantity,
            };
        }
    }
}
