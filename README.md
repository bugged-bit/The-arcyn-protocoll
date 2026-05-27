# ARCYN — Tactical Workspace Launcher

Define **modes** (a name + apps, folders, and websites). Launch everything with one key press. Designed for fast workflow switching.

---

## Build

**Windows (WPF UI)**
```bash
dotnet build ARCYN.UI -c Release
# Output: ARCYN/ARCYN.UI/bin/Release/net8.0-windows/win-x64/ARCYN.exe
```

**Linux (Avalonia UI)**
```bash
dotnet build ARCYN.Avalonia -c Release
# Output: ARCYN/ARCYN.Avalonia/bin/Release/net8.0/ARCYN.Avalonia
```

**Headless console harness (both OS)**
```bash
dotnet build ARCYN.Console -c Release
```

## Run

```bash
# Windows
ARCYN.exe

# Linux
dotnet run --project ARCYN.Avalonia

# CLI setup wizard (both)
ARCYN --setup
```

## Known limitations

- **WPF UI** (`ARCYN.UI`) is Windows‑only. It is skipped automatically during Linux builds.
- **Avalonia UI** (`ARCYN.Avalonia`) is the Linux entry point. It provides a basic list interface — the animated HUD, particles, telemetry overlay, and acrylic effects are WPF‑only features not yet ported.
- Acrylic / transparency effects are Windows‑only (`user32.dll`). No‑op on Linux.
- Telemetry (CPU/RAM) uses `PerformanceCounter` on Windows; on Linux it reads `/proc/meminfo` for RAM only (CPU reads report zero).
- Folder and URL launching requires `xdg-open` or `gio`. If neither is available, the app logs the failure and continues — it does not crash.
- `.lnk` shortcut files are Windows‑only and ignored on Linux.
