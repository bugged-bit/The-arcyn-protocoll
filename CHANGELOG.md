# Changelog

## [1.6.0] - 2026-05-29

### Added
- First-run setup wizard with guided configuration
- CLI `--setup` flag for headless/terminal configuration
- Import/Export config for sharing between machines
- Telemetry display (live CPU/RAM monitoring)
- Global hotkey support (`Alt+Shift+D`)
- Per-mode accent color customization
- Right-click context menu (move up/down, duplicate, edit)
- Reduced effects mode for low-end systems
- Compact mode for smaller card sizing

### Changed
- Platform references standardized to Linux-first terminology
- Config path defaults to `~/.config/ARCYN/arcyn.json`

### Technical
- Migrated previous entries to 1.5.1 release

## [1.5.1] - 2026-05-28

### Added
- Cinematic HUD overhaul with idle timer, staggered entrance, pulsing UI, and matrix cascades
- 1–6 keyboard shortcut support for quick mode switching
- Loading window with matrix overlay during launch

### Fixed
- Stability improvements: async `SelectMode`, proper `Process` disposal, thread-safe static logging
- Launch fixes: improved folder/shortcut handling, success detection, async target launch
- Deactivation handling during launch (ignored gracefully)
- Abort action now returns to UI without exiting
- Border subtle brush and build artifact cleanup

### Changed
- Extended loading delay for better UX
- Added detailed logging during launch validation
- Boot animation skipped for faster startup
- Loading timer leak prevention

### Technical
- Migrated to local JSON config (no database, no cloud services)
- WPF UI with .NET 8
- Native workspace launcher (ARCYN)
