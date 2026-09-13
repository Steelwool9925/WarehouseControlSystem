# Warehouse Control System

A portfolio demo of a warehouse control system (WCS): a simulated robot fleet picks inventory
against incoming orders on a grid floor, dispatched by a greedy nearest-robot algorithm. Backend
is ASP.NET Core (.NET 10) with no persistence — everything runs in memory. Frontend is a React
control-room UI (not built yet — see status below).

See `docs/superpowers/specs/2026-09-12-warehouse-control-system-design.md` for the full design,
and `roadmap.md` for the feature checklist and links to each implementation plan.

## Status

Built so far: the domain model (`GridPosition`, `Robot`, `InventoryLocation`, `PickTask`, `Order`)
in `backend/WarehouseControl.Api/Domain/`. Everything else on the roadmap — in-memory state,
dispatch/simulation services, REST endpoints, and the whole frontend — is planned but not yet
implemented. This section will be kept current as each roadmap item lands.

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
