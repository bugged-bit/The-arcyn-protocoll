# Contributing to ARCYN

Thanks for your interest! ARCYN is a lightweight project — contributions are welcome but keep it simple.

## Prerequisites

- **Linux** (any distro with .NET 8 support)
- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** (install via your package manager or dotnet.microsoft.com)

## Getting started

1. Fork the repo
2. Create a branch: `git checkout -b feature/your-feature`
3. Make changes
4. Build: `dotnet build ARCYN/ARCYN.UI -c Release`
5. Submit a PR

## Code conventions

- **Language**: C# 12, .NET 8, WPF
- **Style**: Follow existing code — brace positions, naming, patterns
- **No XML comments** — only `//` when the "why" isn't obvious
- **No personal paths** — all config must be user-defined in `arcyn.json`
- **One concern per file** — services for logic, XAML for UI, models for data

## Project structure

```
ARCYN.UI/          — Main WPF UI (WPF)
  App.xaml(.cs)          — Startup + error handling
  Program.cs             — Entry point
  MainWindow.xaml(.cs)   — HUD window + input routing
  Services/              — UI-level services
ARCYN.Core/              — Shared business logic
  Models/                — Config and mode data models
  Services/              — Config, mode, launch, and logging services
  Abstractions/          — Interfaces for platform abstraction
ARCYN.Platform/          — Platform-specific implementations
  ConfigPathProvider.cs  — XDG-compliant config paths (~/.config/arcyn/)
  LinuxPlatformLauncher.cs — xdg-open / gio launcher
  ProcessExecutor.cs     — Process start helpers
```

## Building and packaging

### Debug build

```bash
dotnet build ARCYN/ARCYN.UI -c Debug
```

### Release build

```bash
dotnet build ARCYN/ARCYN.UI -c Release
```

### Self-contained publish (portable binary)

```bash
dotnet publish ARCYN/ARCYN.UI -c Release -r linux-x64 --self-contained true
# Output: ARCYN/ARCYN.UI/bin/Release/net8.0/linux-x64/publish/ARCYN
```

### Packaging for distribution

**AppImage** — use `dotnet publish` then bundle with [AppImageKit](https://appimage.org/):

```bash
dotnet publish ARCYN/ARCYN.UI -c Release -r linux-x64 --self-contained true
# Then wrap the publish directory in an AppDir and run appimagetool
```

**Flatpak** — requires a Flatpak manifest:

```bash
flatpak-builder build-dir flatpak/io.github.arcyn.ARCYN.yml --force-clean
flatpak-builder --run build-dir io.github.arcyn.ARCYN.yml
flatpak build-export export-dir build-dir
```

**Snap** — requires a `snap/snapcraft.yaml`:

```bash
snapcraft
```

**Debian package (.deb)**:

```bash
dotnet publish ARCYN/ARCYN.UI -c Release -r linux-x64 --self-contained true
# Use dpkg-deb to package the publish directory with proper DEBIAN/control
```

**RPM package**:

```bash
dotnet publish ARCYN/ARCYN.UI -c Release -r linux-x64 --self-contained true
# Use rpmbuild to package the publish directory
```

## Pull request guidelines

- One feature/fix per PR
- Rebase on main before submitting
- Ensure `dotnet build ARCYN/ARCYN.UI -c Release` passes with 0 errors, 0 warnings
- No breaking changes to `arcyn.json` schema without migration support
- Test manually: run the app, create a mode, launch it, verify keyboard nav

## Reporting issues

Use the [issue templates](.github/ISSUE_TEMPLATE/) — include:
- ArcYN version / build date
- Steps to reproduce
- Expected vs actual behavior
- Config file (redact personal paths)
