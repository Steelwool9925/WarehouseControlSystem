# Roadmap

Feature checklist for the Warehouse Control System demo, grouped and
ordered as specified. See `docs/superpowers/specs/2026-09-12-warehouse-control-system-design.md`
for the full design.

## Backend (ASP.NET Core minimal API, .NET 10, no external NuGet deps)

**Plan:** `FEATURE_PLAN_domain-models.md` (Tier 2, built & merged — plan pruned by cleanup-crew; see `.claude/reports/TEST_REPORT_domain-models.md`)
- [x] Scaffold a minimal API project: `WarehouseControl.Api`
- [x] Domain model: `GridPosition` (record struct, Manhattan distance + step-toward helpers)
- [x] Domain model: `Robot` (id, name, home station, position, status enum: Idle/MovingToPick/ReturningToStation/Charging, battery %, current task id)
- [x] Domain model: `InventoryLocation` (sku, description, grid position, quantity)
- [x] Domain model: `PickTask` (id, order id, sku, target location, status enum: Pending/Assigned/Completed, assigned robot id, timestamps)
- [x] Domain model: `Order` (id, customer reference, line item skus, status enum: Pending/InProgress/Fulfilled, timestamps)

**Plan:** [`FEATURE_PLAN_state-and-seeding.md`](.claude/plans/FEATURE_PLAN_state-and-seeding.md) (Tier 2 selected, approved)
- [ ] Thread-safe in-memory state (`ConcurrentDictionary`) for robots/inventory/orders/tasks, seeded with a sample fleet (4 robots) and inventory catalogue (8 SKUs on a grid)
- [ ] Configure enums to serialize as strings, not ints
- [ ] Enable CORS for the local frontend dev origin

**Plan:** [`FEATURE_PLAN_dispatch-and-simulation.md`](.claude/plans/FEATURE_PLAN_dispatch-and-simulation.md) (Tier 2 selected, approved)
- [ ] `DispatchService.CreateOrder`: validates SKUs exist, explodes an order into one pick task per line item
- [ ] `DispatchService.RunDispatchCycle`: greedily assigns pending tasks to the nearest idle robot (by Manhattan distance) with battery above a minimum threshold
- [ ] `FleetSimulationService` (BackgroundService): ticks every second — reruns dispatch, steps busy robots one grid cell toward their target, completes tasks on arrival, sends low-battery robots home to charge, marks orders fulfilled once all their tasks complete
- [ ] `WarehouseControl.Tests` (xUnit): unit tests for `DispatchService` and simulation tick behavior (GridPosition tests land earlier, in the domain-models plan)

**Plan:** [`FEATURE_PLAN_rest-endpoints.md`](.claude/plans/FEATURE_PLAN_rest-endpoints.md) (Tier 2 selected, approved)
- [ ] REST endpoints: `GET /api/robots`, `GET /api/inventory`, `GET/POST /api/orders`, `GET /api/orders/{id}`, `GET /api/tasks`, `POST /api/dispatch/run`, `GET /api/kpis` (active/idle/charging robots, order counts by status, average pick time)
- [ ] Add a `.http` file with example requests for manual testing

## Frontend (React + Vite)

**Plan:** [`FEATURE_PLAN_frontend-scaffold.md`](.claude/plans/FEATURE_PLAN_frontend-scaffold.md) (Tier 1 selected, approved)
- [ ] Scaffold with Vite, plain CSS (no UI framework)
- [ ] Design tokens: dark control-room palette (slate background, amber/teal/red status colors), Space Grotesk for UI text, IBM Plex Mono for data/IDs
- [ ] `api.js`: fetch wrapper for all backend endpoints

**Plan:** [`FEATURE_PLAN_frontend-components.md`](.claude/plans/FEATURE_PLAN_frontend-components.md) (Tier 1 selected, approved)
- [ ] `WarehouseGrid`: SVG floor view — grid lines, inventory racks, charging docks, robots as animated circles that transition position on each poll
- [ ] `RobotFleet`: list view with status dot, battery bar per robot
- [ ] `OrderQueue`: horizontally scrolling cards color-coded by status
- [ ] `KpiBar`: stat cells from `/api/kpis`

**Plan:** [`FEATURE_PLAN_app-shell-and-finish.md`](.claude/plans/FEATURE_PLAN_app-shell-and-finish.md) (Tier 1 selected, approved)
- [ ] `App.jsx`: polls robots/orders/kpis every ~1.5s, inventory once on load, shows a connection-status badge if the API is unreachable, includes a "place order" form (SKU chip picker + optional reference field)
- [ ] Responsive layout: grid view + fleet/KPIs sidebar + order queue strip below

## Finish

(see `FEATURE_PLAN_app-shell-and-finish.md` above — README + .gitignore are planned together with the app shell)
- [ ] Root README explaining the architecture and how to run both halves
- [ ] `.gitignore` for both stacks (node_modules, dist, bin, obj)
