# Warehouse Control System

A portfolio demo of a warehouse control system (WCS): a simulated robot fleet picks inventory
against incoming orders on a grid floor, dispatched by a greedy nearest-robot algorithm and shown
live in a React control-room UI. No database — everything runs in memory and resets on restart.

See `docs/superpowers/specs/2026-09-12-warehouse-control-system-design.md` for the full design.
`roadmap.md` tracks the feature checklist; every item on it is now built.

## Architecture

- **Backend** — `backend/WarehouseControl.Api` (ASP.NET Core minimal API, .NET 10, no external
  NuGet dependencies in the API project itself). `WarehouseState` is an in-memory,
  `ConcurrentDictionary`-backed singleton seeded with 4 robots and 8 SKUs on a 10×10 grid.
  `DispatchService` turns orders into pick tasks and assigns them to the nearest eligible robot;
  `FleetSimulationService` is a `BackgroundService` that ticks every second, moving robots,
  draining/recharging battery, completing tasks, and fulfilling orders. A REST API
  (`backend/WarehouseControl.Api/Endpoints/`) exposes all of it; see
  `WarehouseControl.Api.http` for example requests. `backend/WarehouseControl.Tests` (xUnit)
  covers the domain model and every service.
- **Frontend** — `frontend/` (Vite + React, plain CSS). `App.jsx` polls the backend every 1.5s
  and renders `WarehouseGrid` (an animated SVG floor view), `RobotFleet`, `OrderQueue` (with the
  place-order form), and `KpiBar`. A connection-status badge in the header flips on poll failure
  without wiping the last-known state. Design tokens (dark control-room palette, Space Grotesk +
  IBM Plex Mono) live in `frontend/src/tokens.css`.

## Running it

Two terminals — the backend must be running for the frontend to show live data.

### Backend

Requires the **.NET 10 SDK**. If `dotnet --list-sdks` only shows .NET 8 or earlier, install .NET
10 per-user (no admin rights needed — the machine-wide installer can hang on a UAC prompt in a
non-interactive shell):

```powershell
Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile "$env:TEMP\dotnet-install.ps1"
& "$env:TEMP\dotnet-install.ps1" -Channel 10.0 -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet" -NoPath
```

That install path isn't on `PATH` by default in a fresh shell, so prefix commands with it:

```powershell
$env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
cd backend
dotnet build
dotnet test
dotnet run --project WarehouseControl.Api
```

The API listens on `http://localhost:5299` by default (`dotnet run ... --urls "http://localhost:5299"`
if you need to pin it explicitly).

### Frontend

```powershell
cd frontend
npm install
npm run dev
```

Open `http://localhost:5173`. The dev server is pinned to that port (`vite.config.js`) because
the backend's CORS policy only allows that exact origin.

### Using it

Select one or more SKU chips in the order queue, optionally add a customer reference, and click
"Place order." The nearest idle robot picks it up automatically within a second or two — watch it
move across the floor view, complete the task, and return home (or head to charge, if its battery
dropped below 20% along the way).

## Project layout

```
backend/
  WarehouseControl.Api/        # minimal API — Domain/, State/, Services/, Endpoints/
  WarehouseControl.Tests/      # xUnit
frontend/
  src/
    components/                # WarehouseGrid, RobotFleet, OrderQueue, KpiBar
    lib/statusColor.js         # shared status → color mapping
    api.js                     # backend fetch client
    tokens.css                 # design tokens
docs/superpowers/specs/        # design spec
roadmap.md                     # feature checklist
```
