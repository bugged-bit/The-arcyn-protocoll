# Changelog

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
- Windows-native workspace launcher (ARCYN)
