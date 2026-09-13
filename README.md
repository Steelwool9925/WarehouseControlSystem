# Warehouse Control System

A portfolio demo of a warehouse control system (WCS): a simulated robot fleet picks inventory
against incoming orders on a grid floor, dispatched by a greedy nearest-robot algorithm. Backend
is ASP.NET Core (.NET 10) with no persistence — everything runs in memory. Frontend is a React
control-room UI (not built yet — see status below).

See `docs/superpowers/specs/2026-09-12-warehouse-control-system-design.md` for the full design,
and `roadmap.md` for the feature checklist and links to each implementation plan.

## Status

Built so far:
- Domain model (`GridPosition`, `Robot`, `InventoryLocation`, `PickTask`, `Order`) in
  `backend/WarehouseControl.Api/Domain/`.
- In-memory state (`WarehouseState`, seeded with a 4-robot fleet and 8-SKU catalogue), enum
  JSON serialization, and CORS for the frontend dev origin, in `backend/WarehouseControl.Api/State/`
  and `Program.cs`.
- Dispatch and simulation: `DispatchService` (order → pick tasks, nearest-idle-robot assignment)
  and `FleetSimulationService` (1s background tick: movement, battery, task/order completion) in
  `backend/WarehouseControl.Api/Services/`.
- REST API: `GET /api/robots`, `GET /api/inventory`, `GET/POST /api/orders`,
  `GET /api/orders/{id}`, `GET /api/tasks`, `POST /api/dispatch/run`, `GET /api/kpis`, in
  `backend/WarehouseControl.Api/Endpoints/`. See `backend/WarehouseControl.Api/WarehouseControl.Api.http`
  for example requests.

The backend is now fully functional end-to-end. Everything else on the roadmap — the whole
frontend — is planned but not yet implemented. This section will be kept current as each roadmap
item lands.

## Running the backend

Requires the .NET 10 SDK. If only .NET 8 or earlier shows up in `dotnet --list-sdks`, install it
per-user (no admin rights needed):

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

## Running the frontend

Not built yet — instructions land here once the frontend-scaffold plan is implemented.
