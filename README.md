<div align="center">
  <img src="https://pan.samyyc.dev/s/VYmMXE" />
  <h2><strong>MapRestart</strong></h2>
  <h3>Automatically reloads the current map to mitigate CS2 tick drift on long-running, low-population servers.</h3>
</div>

<p align="center">
  <img src="https://img.shields.io/badge/build-passing-brightgreen" alt="Build Status">
  <img src="https://img.shields.io/github/downloads/Shmitzas/MapRestart/total" alt="Downloads">
  <img src="https://img.shields.io/github/stars/Shmitzas/MapRestart?style=flat&logo=github" alt="Stars">
  <img src="https://img.shields.io/github/license/Shmitzas/MapRestart" alt="License">
</p>

---

## Features

- 🔄 Automatic map reload via `map` (or `host_workshop_map` for workshop maps) when tick drift conditions are likely
- 🧩 Workshop-aware — uses `host_workshop_map <workshopId>` when the current map has a workshop ID, otherwise falls back to `map <mapName>`
- ⏱️ Configurable map-age threshold (defaults to 1 hour)
- 👤 Triggers only when the server is empty (zero disruption — no one is playing when the restart fires)

## Commands

This plugin exposes no chat or console commands. It operates passively in the background.

## Configuration

Config file: `addons/swiftlys2/plugins/MapRestart/config.jsonc`

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `DetailedLogging` | bool | `false` | Enable verbose informational logging for diagnostics. Warnings and errors are always logged. |
| `MapRestartThresholdMinutes` | int | `60` | Minimum map age (in minutes) before a restart can be triggered. |

### Example Configuration

```json
{
  "MapRestart": {
    "DetailedLogging": false,
    "MapRestartThresholdMinutes": 60
  }
}
```

## How It Works

CS2 servers develop "tick drift" when a map stays loaded for extended periods. This plugin reloads the map to reset tick state, but only when the server is empty so no active session is disrupted.

On `OnMapLoad`, the plugin records the map name and timestamp and arms a periodic 60s timer. Two triggers can then fire the empty-server check:

1. `OnClientDisconnected` — if the map has exceeded the threshold, waits 2 seconds and counts non-bot players.
2. The 60s periodic timer — short-circuits until `elapsed ≥ threshold`, then each tick runs the same human-count check. Covers a server that stayed empty from map load.

If **zero** humans remain when the check runs, the plugin picks the restart command based on `Core.Engine.WorkshopId`: `host_workshop_map <workshopId>` when a workshop ID is present, otherwise `map <mapName>`.

```
OnMapLoad → store map + timestamp + arm 60s periodic timer
       │
       ├─ OnClientDisconnected     → map age ≥ threshold → wait 2s → humans == 0 → reload map
       │
       └─ periodic timer tick (60s) → map age ≥ threshold → humans == 0 → reload map
```

## Building

- Open the project in your preferred .NET IDE (Visual Studio, Rider, VS Code).
- Run `dotnet build`. Output DLL and resources are placed in `build/`.
- Run `dotnet publish -c Release` to produce a distributable zip in `build/`.
