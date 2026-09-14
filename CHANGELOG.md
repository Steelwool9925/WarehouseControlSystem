# Changelog

What each pull request changed, in merge order. Each PR corresponds to one item on
`roadmap.md`'s feature checklist.

## [PR #1 — Add domain models for Warehouse Control System backend](https://github.com/Steelwool9925/WarehouseControlSystem/pull/1)

Scaffolded `WarehouseControl.Api` (.NET 10 minimal API) and `WarehouseControl.Tests` (xUnit).

**Added**
- `GridPosition` — grid coordinate value type with Manhattan distance and step-toward helpers
- `Robot`, `InventoryLocation`, `PickTask`, `Order` domain entities and their status enums
- `GridPositionTests` — correctness plus a zero-allocation proof for the hot-path math

**Fixed during review**
- `GridPosition`'s distance/step math widened to `long` internally — the original `int` math could
  theoretically overflow at extreme coordinate values
- Missing `.gitignore` entries for backend build artifacts, added before they could get committed
- A redundant `using` directive

**Tests:** 8/8 passing.

## [PR #2 — Add in-memory state, seeding, JSON enum config and CORS](https://github.com/Steelwool9925/WarehouseControlSystem/pull/2)

**Added**
- `WarehouseState` — the `ConcurrentDictionary`-backed singleton holding all runtime state, seeded
  with 4 robots (grid corners) and 8 SKUs (interior positions) at startup
- Enum-as-string JSON serialization and a CORS policy scoped to the frontend's dev origin
- `WarehouseStateTests` — seed correctness, case-insensitive SKU lookup with a measured
  allocation-proof, host-level enum/CORS behavior via `WebApplicationFactory`

**Fixed during review**
- `Seed()` had no idempotency guard — a second call would have silently duplicated the fleet
- An unused test-local variable

**Tests:** 14/14 passing.

## [PR #3 — Add dispatch and fleet simulation services](https://github.com/Steelwool9925/WarehouseControlSystem/pull/3)

**Added**
- `DispatchService` — SKU-validated order creation, nearest-idle-robot task assignment
- `FleetSimulationService` — the 1-second background tick driving robot movement, battery drain/
  recharge, task completion, inventory decrement, and order fulfillment
- Scripted multi-tick tests covering a full pick → complete → return → recharge → idle cycle,
  plus a measured allocation-budget test for the dispatch loop

**Fixed during review**
- A real check-then-act **race condition**: `RunDispatchCycle` could double-assign a robot if
  called concurrently by the tick loop and a future manual-dispatch endpoint — fixed with a lock
- A battery-drain bug: a robot already at its target still lost battery for a tick it didn't move
- Non-deterministic task ordering when multiple tasks shared a timestamp — added a secondary sort
  key
- Removed a redundant tracking structure that never changed the assignment outcome

**Tests:** 25/25 passing.

## [PR #4 — Add REST endpoints, KPI calculator, and .http sample requests](https://github.com/Steelwool9925/WarehouseControlSystem/pull/4)

**Added**
- All seven REST endpoints (robots, inventory, orders ×3, tasks, dispatch/run, kpis)
- `KpiCalculator` — single-pass stat aggregation instead of multiple LINQ `.Count()` passes
- `WarehouseControl.Api.http` with one example request per endpoint
- `EndpointsTests` via `WebApplicationFactory`, covering every endpoint including error paths

**Fixed during review**
- A real **crash-class bug**: a request body omitting the `skus` field threw an unhandled 500
  instead of the documented 400 — fixed at the service layer, with regression tests at both the
  service and HTTP layers

**Tests:** 42/42 passing, 97.5% line coverage.

## [PR #5 — Add frontend scaffold, design tokens, and api client](https://github.com/Steelwool9925/WarehouseControlSystem/pull/5)

**Added**
- Vite + React scaffold (plain JS, no TypeScript), dev server pinned to `:5173` to match the
  backend's CORS allow-list
- `tokens.css` — the dark control-room palette and Space Grotesk / IBM Plex Mono type system
- `api.js` — one shared `request()` helper backing all eight backend endpoint calls

Verified live in-browser against the running backend, both the success and error paths.

## [PR #6 — Add control-room components: grid, fleet list, order queue, KPI bar](https://github.com/Steelwool9925/WarehouseControlSystem/pull/6)

**Added**
- `WarehouseGrid` — SVG floor view with transform-animated robot markers
- `RobotFleet` — status dot + battery bar per robot
- `OrderQueue` — status-colored order cards, plus the place-order form (SKU chip picker, inline
  error surfacing on failure)
- `KpiBar` — the 7-cell stat strip
- `lib/statusColor.js` — the shared color semantics every component above draws from

Verified against a mock-data harness, including an interactive chip-select/submit pass and a
deliberate-mistake pass on the form (empty submit, double-submit, malformed input).

## [PR #7 — Wire App.jsx to live polling, add responsive shell, finalize README](https://github.com/Steelwool9925/WarehouseControlSystem/pull/7)

The final PR — completed the roadmap.

**Added**
- Wired `App.jsx` to real polling: `Promise.all` over robots/orders/kpis every 1.5s, inventory
  once on mount, a connection-status badge, and `handleCreateOrder` (with an immediate refresh on
  success so a new order shows up without waiting for the next poll)
- `App.css` — the responsive CSS Grid shell, collapsing to one column below 900px
- Finalized `README.md`

**Found and fixed live**, wiring both stacks together for the first time:
- `WarehouseGrid` had no size bound — at `width: 100%` of the wide main column with a 1:1 aspect
  ratio, it rendered nearly 1300px tall, pushing the order queue off-screen
- A large empty gap before the order queue, from CSS Grid distributing the viewport's leftover
  height across rows that should have sized to content

Both fixed and re-verified live before merge (full order → dispatch → fulfill cycle, connection
drop/recovery, inline error handling, the responsive breakpoint).

---

## Post-merge verification (not a PR — a standalone UAT pass)

A full end-to-end UAT pass was run afterward against the completed `main`, covering everything
above as one continuous session rather than per-PR spot checks. New coverage: a single order with
4 SKUs, dispatching all 4 robots simultaneously — confirmed the nearest-robot algorithm assigns
correctly under concurrent load, and that inventory only decrements on actual task completion
(verified against task timestamps), not on assignment. No defects found; the two bugs from PR #7
were reconfirmed fixed.
