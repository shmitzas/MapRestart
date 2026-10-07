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
- 👤 Triggers only when the server is empty, confirmed across two consecutive checks (zero disruption — no one is playing when the restart fires)
- 🎯 Fires on `OnClientDisconnected` and via a 1-minute periodic check, so a server that stays empty since map load (no client events at all) still restarts on schedule
- 🔇 Silent by default with an optional detailed logging mode for diagnostics

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

**Overview**

CS2 servers can develop "tick drift" when a map stays loaded for extended periods, causing timing to gradually desync from the intended tick rate. This plugin opportunistically reloads the current map via the `map` engine command (or `host_workshop_map` for workshop maps) when doing so will not disrupt any ongoing gameplay.

**Map Age Tracking:**
The plugin subscribes to `OnMapLoad` and records the map name plus a timestamp every time the map changes. A "restart triggered" flag is also reset, ensuring at most one restart attempt per map load.

**Trigger Evaluation:**
Two triggers can fire the empty-server check:

1. `OnClientDisconnected` — whenever a client leaves. If the current map's age has already exceeded `MapRestartThresholdMinutes`, the human-count check is deferred by 2 seconds (the disconnecting player may still register as valid the instant the listener fires).
2. A 60-second periodic timer armed at map load. It short-circuits until the map age reaches the threshold, after which every tick runs the same human-count check. This covers a server that stayed empty from the moment the map loaded, where no client event would ever fire.

**Restart Decision:**
Each check counts connected non-bot players by their controller rather than their pawn. Pawn-based validity reads a full server as empty during the round-restart window, where CS2 has destroyed every pawn and not yet respawned. A restart requires **two consecutive** zero readings, so a single mistimed check can never reload a map out from under a live match; any non-zero reading resets the counter.

Once confirmed, the map is re-issued through `Core.Engine.ExecuteCommand`, resetting the engine's tick state before the next player joins. A "restart triggered" flag caps this at one attempt per map load.

**Workshop vs Built-in Maps:**
The restart command is picked based on `Core.Engine.WorkshopId`. If the current map has a workshop ID, the plugin issues `host_workshop_map <workshopId>`. Otherwise it issues `map <mapName>` using the map name recorded on `OnMapLoad`.

**Trigger Flow:**

```
OnMapLoad → store map + timestamp + arm 60s periodic timer
       │
       ├─ OnClientDisconnected ──► wait 2 seconds ─┐
       │                                           │
       └─ periodic timer tick (60s) ───────────────┤
                                                   ▼
                                          map age ≥ threshold?
                                                   │
                                          ┌────────┴────────┐
                                         no                yes
                                          │                 ▼
                                         skip         humans check
                                                            │
                                          ┌─────────────────┴──────────────┐
                                    humans ≠ 0                        humans = 0
                                          │                                │
                                 reset counter, skip        ┌──────────────┴──────────────┐
                                                      1st reading                   2nd reading
                                                            │                             │
                                                  wait for next check                     │
                                                                    ┌─────────────────────┴─────────────────────┐
                                                              workshop map                                built-in map
                                                                    │                                           │
                                                   host_workshop_map <workshopId>                      map <mapName>
```

## Troubleshooting

| Problem | Possible Cause | Solution |
|---------|----------------|----------|
| Map never restarts | Threshold not yet reached | Lower `MapRestartThresholdMinutes` for testing, or wait longer |
| Map never restarts | Server never fully empties | Plugin is working as intended — restarts only happen when no humans are on the server |
| Plugin restarts map at unexpected time | Hot reload reset the map-loaded timestamp | Expected: on hot reload the timestamp resets to "now" to prevent an immediate restart; the periodic check will pick up once threshold elapses from that point |
| Map reloaded mid-match | Running a build older than 1.2.0 | Earlier versions counted players by pawn, which reads as empty during the round-restart respawn window. Update the plugin. |
| Want to verify behaviour | Need visibility into decisions | Set `DetailedLogging` to `true` and check the server console |

## Building

- Open the project in your preferred .NET IDE (Visual Studio, Rider, VS Code).
- Run `dotnet build`. Output DLL and resources are placed in `build/`.
- Run `dotnet publish -c Release` to produce a distributable zip in `build/`.
