# Warehouse Control System — Design Spec

Date: 2026-09-12
Status: Approved for planning

## Purpose

A portfolio-grade, full-stack demo of a warehouse control system (WCS): a
simulated robot fleet picks inventory against incoming orders on a grid
floor, dispatched by a greedy nearest-robot algorithm and animated live in
a React control-room UI. Demonstrates clean domain modeling, a
dependency-light ASP.NET Core backend, a background simulation loop, and a
polished real-time-feeling frontend — with no external services or
database required to run it.

## Repo layout

```
WarehouseControlSystem/
├── backend/
│   ├── WarehouseControl.sln
│   ├── WarehouseControl.Api/        # minimal API, no external NuGet deps
│   └── WarehouseControl.Tests/      # xUnit
├── frontend/
│   └── (Vite React app)
├── docs/superpowers/specs/          # this document
├── roadmap.md                       # feature checklist, grouped & ordered
├── README.md
└── .gitignore
```

Note: this project currently lives nested inside `Project1/` because the
session's sandbox root could not be renamed live; the user will rename/
flatten `Project1` → `WarehouseControlSystem` after the session ends. All
paths inside the project are relative, so this move is safe.

## Backend architecture (.NET 10, ASP.NET Core minimal API)

**Project: `WarehouseControl.Api`** — no external NuGet dependencies.
Top-level `Program.cs` composes endpoint groups per resource (`robots`,
`inventory`, `orders`, `tasks`, `dispatch`, `kpis`).

### Domain (`Domain/`)

- `GridPosition` — record struct `(int X, int Y)` with Manhattan distance
  and a `StepToward(GridPosition target)` helper that moves one cell
  along the axis with the larger remaining delta (ties broken on X).
- `Robot` — id, name, home station (`GridPosition`), current position,
  status enum (`Idle`, `MovingToPick`, `ReturningToStation`, `Charging`),
  battery percent, current task id (nullable).
- `InventoryLocation` — sku, description, grid position, quantity.
- `PickTask` — id, order id, sku, target location, status enum
  (`Pending`, `Assigned`, `Completed`), assigned robot id (nullable),
  created/assigned/completed timestamps.
- `Order` — id, customer reference, line item skus, status enum
  (`Pending`, `InProgress`, `Fulfilled`), created/fulfilled timestamps.

All status enums serialize as strings via
`JsonStringEnumConverter`, configured globally in `Program.cs`.

### State (`State/WarehouseState.cs`)

Singleton holding four `ConcurrentDictionary<Guid, T>` (robots,
inventory keyed by sku, tasks, orders). A seed method runs at startup:
4 robots at distinct home stations, 8 SKUs placed at distinct grid
positions with sample quantities.

### Services

- `DispatchService`
  - `CreateOrder(customerRef, skus[])` — validates every sku exists in
    inventory (throws/returns a validation failure otherwise), creates
    an `Order` (`Pending`), and explodes it into one `PickTask`
    (`Pending`) per line item.
  - `RunDispatchCycle()` — for each `Pending` task, finds the nearest
    `Idle` robot (Manhattan distance to the task's target) whose battery
    is above a minimum threshold (e.g. 20%), assigns the task
    (`Assigned`), and flips the robot to `MovingToPick`. Ties broken by
    robot id for determinism.
- `FleetSimulationService : BackgroundService` — ticks every 1s:
  1. Calls `RunDispatchCycle()`.
  2. Steps every `MovingToPick`/`ReturningToStation` robot one grid cell
     toward its target via `GridPosition.StepToward`.
  3. On arrival at a pick target: completes the task, sends the robot to
     `ReturningToStation` (or `Charging` if battery is below threshold),
     decrements inventory quantity.
  4. On arrival at home/charging station: robot becomes `Idle`
     (or continues `Charging` until battery reaches full, then `Idle`).
  5. Drains robot battery by a fixed amount per tick while moving;
     recharges while `Charging`.
  6. Rolls an `Order` to `Fulfilled` once all its tasks are `Completed`.

### Endpoints

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/robots` | List all robots |
| GET | `/api/inventory` | List all inventory locations |
| GET | `/api/orders` | List all orders |
| POST | `/api/orders` | Create an order (see `CreateOrder`) |
| GET | `/api/orders/{id}` | Get one order |
| GET | `/api/tasks` | List all pick tasks |
| POST | `/api/dispatch/run` | Force an off-cycle dispatch pass |
| GET | `/api/kpis` | Active/idle/charging robot counts, order counts by status, average pick time |

CORS is enabled for the local Vite dev origin. A `WarehouseControl.Api.http`
file provides example requests for every endpoint for manual testing.

### Testing (`WarehouseControl.Tests`, xUnit)

- `GridPosition`: distance calculation, `StepToward` convergence and
  axis-tie behavior.
- `DispatchService.CreateOrder`: rejects unknown SKUs, explodes valid
  orders into the right number of tasks.
- `DispatchService.RunDispatchCycle`: assigns to the nearest idle robot,
  excludes robots below the battery threshold, deterministic tie-break.
- A simulation-tick test: a robot steps toward its target, completes the
  task on arrival, and the order fulfills once all its tasks complete.

## Frontend architecture (React + Vite, plain CSS)

- **Design tokens** (`tokens.css`): dark slate control-room background,
  amber/teal/red status colors, Space Grotesk for UI text, IBM Plex Mono
  for data/IDs.
- **`api.js`** — one fetch function per endpoint; throws on non-2xx so
  callers can catch and surface connection state.
- **Components**
  - `WarehouseGrid` — SVG floor view: grid lines, inventory racks,
    charging docks, robots rendered as circles that animate (CSS
    transition on transform) between polled positions.
  - `RobotFleet` — list view, status dot + battery bar per robot.
  - `OrderQueue` — horizontally scrolling, status-color-coded order
    cards; hosts the "place order" form (SKU chip picker + optional
    customer reference field).
  - `KpiBar` — stat cells sourced from `/api/kpis`.
- **`App.jsx`** — polls `/api/robots`, `/api/orders`, `/api/kpis` every
  ~1.5s; fetches `/api/inventory` once on mount; tracks a
  connection-status flag that flips to "disconnected" on fetch failure
  (grid freezes in place rather than crashing); composes the responsive
  layout — grid + fleet/KPI sidebar, order queue strip below.

## Data flow & error handling

1. `POST /api/orders` validates SKUs (400 + message on unknown sku),
   creates the order and its tasks (`Pending`).
2. Each simulation tick (or a manual `POST /api/dispatch/run`) assigns
   pending tasks to eligible idle robots and advances all in-flight
   robots one grid step, completing tasks/orders as robots arrive.
3. Frontend polling failures never crash the UI — they flip the
   connection badge and pause updates until the API is reachable again.

## Out of scope (YAGNI for this demo)

- Persistence (no database — in-memory state only, resets on restart).
- Authentication/authorization.
- Multi-warehouse / multi-floor support.
- Real hardware/PLC integration — this is a simulation only.

## Roadmap

See `roadmap.md` for the ordered, checkable feature list.
