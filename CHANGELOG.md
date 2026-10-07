# Changelog

Notable changes to MapRestart, newest first.

<!-- Release notes are taken from the section matching PluginMetadata.Version in
     src/MapRestart.cs, so every release needs a "## [x.y.z]" heading here. -->

## [1.2.0] - 2026-10-07

Fixes a case where the map could reload while people were still playing. Worth updating.

- The map no longer reloads mid-match. The empty-server check could briefly read a
  full server as empty during a round restart, and a check landing in that moment
  reloaded the map.
- A reload now needs two empty readings in a row instead of one.
- `config.jsonc` is re-read on save, so `MapRestartThresholdMinutes` and
  `DetailedLogging` no longer need a server restart to take effect.
