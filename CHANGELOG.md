# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- A dark theme, on by default, with light and "follow Windows" as alternatives. The title
  bar, list view, tool strips and check boxes are all drawn to match; the shell's own
  message boxes and file dialogs are not ours to colour and stay as Windows draws them.
- Locking a layout. A locked layout is skipped by the automatic pruner however old it
  gets, does not count against the history limit, and refuses to be deleted until it is
  unlocked.

- A standalone build. Releases now ship two zips: a small framework-dependent one, and a
  self-contained one that carries the .NET runtime and runs on a machine with nothing
  installed.

### Fixed

- The CLI was named `iconssaver.exe` and the tray app `IconsSaver.exe`. Windows treats
  those as the same file name, so publishing both into one folder left only whichever was
  written last — which would have shipped every release zip broken. The CLI is now
  `iconssaver-cli.exe`.
- The last setting in the settings dialog was hidden behind the buttons. The dialog now
  measures its content and sizes itself to fit, scrolling only when the screen cannot.

## [0.1.0] - 2026-07-30

First working version.

### Added

- Capture the position of every desktop icon and restore it exactly, by reading and
  writing the shell's own icon view.
- Named layouts, stored one JSON document each under `%APPDATA%\IconsSaver`.
- A rolling history of automatic snapshots, pruned to a configurable depth, skipped when
  nothing has moved since the last one.
- Automatic restore after the display arrangement changes, debounced until the shell has
  finished rearranging.
- A display fingerprint on every layout — virtual screen bounds plus each monitor's stable
  device id, position, size and DPI — so a layout is never applied to a setup where its
  coordinates would make no sense.
- Two-pass icon matching by display name, with a case-insensitive second pass and index
  tie-breaking, so a layout survives files being added, removed and renumbered.
- A tray application with a live recent-layouts menu, and a window with a scale preview of
  each saved layout.
- "What would change?", which reports the effect of a restore without performing it.
- Global hotkeys for save and restore.
- A command line tool covering save, list, restore, show, delete, export, import and
  status.
- Import and export of layouts as JSON.
- Start with Windows, via the per-user `Run` key.

### Notes

- Restoring turns "Auto arrange icons" off and leaves it off, because switching it back on
  repacks every icon and would undo the restore. The application reports when it has done
  this.
- The build is 64-bit only. A 32-bit build reads every icon name back as an empty string
  while still reading positions correctly, which would produce layouts that save cleanly
  and restore nothing; this is now a hard startup failure rather than a silent one.

[Unreleased]: https://github.com/KilluaZoldyck0099/Icons-Saver/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/KilluaZoldyck0099/Icons-Saver/releases/tag/v0.1.0
