using WarehouseControl.Api.Domain;
using WarehouseControl.Api.State;

namespace WarehouseControl.Api.Services;

/// <summary>
/// Turns orders into pick tasks and assigns pending tasks to the nearest eligible robot.
/// </summary>
public sealed class DispatchService(WarehouseState state)
{
    /// <summary>
    /// Robots below this battery percent are never assigned a new task, and route home to
    /// <see cref="RobotStatus.Charging"/> instead of <see cref="RobotStatus.ReturningToStation"/>
    /// once their current task completes.
    /// </summary>
    public const double LowBatteryThreshold = 20;

    // RunDispatchCycle is a check-then-act over shared ConcurrentDictionary-backed state: it
    // reads which robots are Idle, then mutates the chosen ones. That read-then-write isn't
    // atomic on its own, and this service is a singleton the background tick calls every second
    // while a future POST /api/dispatch/run endpoint can call it concurrently from a request
    // thread — without this lock, two overlapping calls could both pick the same "Idle" robot
    // before either writes MovingToPick, double-assigning it. Serializing the whole cycle is
    // cheap at this fleet size and removes the race entirely.
    private readonly Lock _dispatchLock = new();

    /// <summary>
    /// Validates every SKU exists in inventory, then creates the order and one pending pick
    /// task per line item. No side effects on validation failure.
    /// </summary>
    public CreateOrderResult CreateOrder(string? customerReference, IReadOnlyList<string>? skus)
    {
        // skus arrives from JSON request binding, where a missing or explicit-null "skus"
        // property deserializes to a null list, not an empty one — a client's malformed
        // request must produce a clean 400 here, not an unhandled NullReferenceException.
        if (skus is null || skus.Count == 0)
        {
            return CreateOrderResult.Failure("An order needs at least one SKU.");
        }

        foreach (var sku in skus)
        {
            if (string.IsNullOrWhiteSpace(sku))
            {
                return CreateOrderResult.Failure("SKUs cannot be null or blank.");
            }

            if (!state.Inventory.ContainsKey(sku))
            {
                return CreateOrderResult.Failure($"Unknown SKU: '{sku}'.");
            }
        }

        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerReference = customerReference,
            LineItemSkus = new List<string>(skus),
            CreatedAt = now,
        };
        state.Orders[order.Id] = order;

        foreach (var sku in skus)
        {
            // Inventory lookup already proven to succeed above; ContainsKey and this
            // TryGetValue share the same OrdinalIgnoreCase-keyed dictionary.
            state.Inventory.TryGetValue(sku, out var location);
            var task = new PickTask
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Sku = sku,
                TargetLocation = location!.Position,
                CreatedAt = now,
            };
            state.Tasks[task.Id] = task;
        }

        return CreateOrderResult.Success(order);
    }

    /// <summary>
    /// Assigns each pending task (oldest first, ties broken by task Id for full determinism) to
    /// the nearest idle, sufficiently-charged robot. Returns the number of tasks assigned.
    /// </summary>
    public int RunDispatchCycle()
    {
        lock (_dispatchLock)
        {
            var pendingTasks = new List<PickTask>();
            foreach (var task in state.Tasks.Values)
            {
                if (task.Status == PickTaskStatus.Pending)
                {
                    pendingTasks.Add(task);
                }
            }

            if (pendingTasks.Count == 0)
            {
                return 0;
            }

            // Secondary sort key (task Id) matters once an order has enough line items that
            // List<T>.Sort's introsort stops being stable (its insertion-sort fallback only
            // holds for small partitions) — every task from one CreateOrder call shares the
            // same CreatedAt, so without this, tie order isn't guaranteed reproducible.
            pendingTasks.Sort(static (a, b) =>
            {
                var byCreatedAt = a.CreatedAt.CompareTo(b.CreatedAt);
                return byCreatedAt != 0 ? byCreatedAt : a.Id.CompareTo(b.Id);
            });

            var assignedCount = 0;

            foreach (var task in pendingTasks)
            {
                var best = FindNearestEligibleRobot(task);
                if (best is null)
                {
                    continue;
                }

                task.Status = PickTaskStatus.Assigned;
                task.AssignedRobotId = best.Id;
                task.AssignedAt = DateTimeOffset.UtcNow;

                // Flips best to non-Idle immediately, so the next iteration's
                // FindNearestEligibleRobot call naturally excludes it — no separate
                // already-claimed tracking needed.
                best.Status = RobotStatus.MovingToPick;
                best.CurrentTaskId = task.Id;
                assignedCount++;

                if (state.Orders.TryGetValue(task.OrderId, out var order) &&
                    order.Status == OrderStatus.Pending)
                {
                    order.Status = OrderStatus.InProgress;
                }
            }

            return assignedCount;
        }
    }

    private Robot? FindNearestEligibleRobot(PickTask task)
    {
        Robot? best = null;
        var bestDistance = int.MaxValue;

        foreach (var robot in state.Robots.Values)
        {
            if (robot.Status != RobotStatus.Idle || robot.BatteryPercent < LowBatteryThreshold)
            {
                continue;
            }

            var distance = robot.Position.ManhattanDistanceTo(task.TargetLocation);

            // Ties broken by robot Id for a deterministic, reproducible assignment.
            if (distance < bestDistance ||
                (distance == bestDistance && best is not null && robot.Id.CompareTo(best.Id) < 0))
            {
                best = robot;
                bestDistance = distance;
            }
        }

        return best;
    }
}

public sealed record CreateOrderResult(bool IsSuccess, Order? Order, string? Error)
{
    public static CreateOrderResult Success(Order order) => new(true, order, null);

    public static CreateOrderResult Failure(string error) => new(false, null, error);
}
