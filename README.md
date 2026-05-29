    █████  ██████  ██████ ██   ██ ███   ██
   ██   ██ ██      ██     ██   ██ ████  ██
   ███████ █████   ██     ███████ ██ ██ ██
   ██   ██ ██      ██     ██   ██ ██  ████
   ██   ██ ██████  ██████ ██   ██ ██   ███

# ARCYN

[![Build](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/bugged-bit/ARCYN)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Platform: Linux](https://img.shields.io/badge/platform-linux-orange)](https://github.com/bugged-bit/ARCYN)
[![Version](https://img.shields.io/badge/version-1.6.0-9cf)](https://github.com/bugged-bit/ARCYN/releases)

**Tactical workspace launcher — modes, apps, folders, websites at your fingertips.**

ARCYN is a HUD-style launcher that opens your apps, websites, and folders together with one click or keypress. Configure workspaces ("modes") and launch your entire stack instantly.

![ARCYN HUD](docs/screenshot.png)

---

## Features

- **Mode-based launching** — each mode opens a set of apps, websites, and folders
- **Keyboard-first** — press `1`–`9` to launch, arrows to navigate, `Enter` to confirm
- **Tactical HUD aesthetic** — dark red-on-black acrylic interface with particle effects
- **Per-mode accent colors** — customize each mode's color
- **Fully configurable** — edit `arcyn.json` or use the built-in wizard
- **Telemetry display** — live CPU/RAM monitoring
- **First-run setup wizard** — guided configuration for new users
- **CLI setup** — `ARCYN --setup` for headless/terminal configuration
- **Import/Export** — share your config between machines

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (required for building)

---

## Quick start

### Download (prebuilt)

1. Download the latest `ARCYN` from [Releases](https://github.com/bugged-bit/ARCYN/releases)
2. Run `./ARCYN`
3. Follow the setup wizard to create your first mode

### Build from source

```bash
git clone https://github.com/bugged-bit/ARCYN.git
cd ARCYN/ARCYN.UI
dotnet restore
dotnet publish -c Release --self-contained true
```

### CLI setup (headless)

```bash
./ARCYN --setup
```

Prompts for mode name, apps, folders, and websites in the terminal. Saves config then launches HUD on next run.

---

## Configuration

ARCYN stores its config at:

- `~/.config/ARCYN/arcyn.json` (user config, default)
- `./arcyn.json` (portable — overrides user config)

### Example `arcyn.json`

```json
{
  "theme": {
    "accent": "#D64545",
    "glow_opacity": 0.28,
    "scanlines": true,
    "animations": true
  },
  "behavior": {
    "idle_timeout_seconds": 10,
    "always_on_top": true,
    "close_on_launch": true
  },
  "modes": [
    {
      "name": "CODE",
      "description": "Development stack",
      "accent": "#D64545",
      "apps": ["code", "firefox"],
      "websites": ["https://github.com"],
      "folders": []
    }
  ]
}
```

### Config reference

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `theme.accent` | string | `"#D64545"` | Global accent color (hex) |
| `theme.glow_opacity` | number | `0.28` | Ambient glow intensity (0–1) |
| `theme.scanlines` | bool | `true` | CRT scanline overlay |
| `theme.animations` | bool | `true` | Enable/disable animations |
| `theme.reduced_effects` | bool | `false` | Disable particles, trail, glow, boot animation (low-end systems) |
| `theme.compact_mode` | bool | `false` | Compact card sizing (more modes visible at once) |
| `behavior.idle_timeout_seconds` | int | `10` | Auto-close after inactivity (0 = disable) |
| `behavior.always_on_top` | bool | `true` | Keep window above other apps |
| `behavior.close_on_launch` | bool | `true` | Close ARCYN after launching a mode |
| `modes[].name` | string | — | Mode display name |
| `modes[].description` | string | `""` | Short description |
| `modes[].accent` | string | `"#D64545"` | Per-mode accent color (hex) |
| `modes[].apps` | string[] | `[]` | Application names/paths |
| `modes[].websites` | string[] | `[]` | URLs to open |
| `modes[].folders` | string[] | `[]` | Folder paths to open |

### Customization tips

- **Reorder modes** — right-click a card → Move Up/Down
- **Duplicate a mode** — right-click → Duplicate
- **Change accent color** — right-click → Edit → pick a color
- **Theme presets** — change `theme.accent` for instant recoloring

---

## Keyboard shortcuts

| Key | Action |
|-----|--------|
| `1`–`9` | Launch mode by position |
| `↑` / `↓` or `←` / `→` | Navigate between modes |
| `Enter` | Launch selected mode |
| `Esc` | Cancel launch / Close ARCYN |
| `Alt`+`Shift`+`D` | Global hotkey (requires launch script) |

---

## Project structure

```
ARCYN/
  ARCYN.sln                   Solution file
  example.arcyn.json          Example config
  arcyn.schema.json           JSON Schema
  scripts/                    Optional launch scripts
  ARCYN.UI/
    App.xaml(.cs)             Startup + error handling
    AppState.cs               Phase state machine
    MainWindow.xaml(.cs)      HUD window + input routing
    SetupWindow.xaml(.cs)     First-run setup wizard
    EditModeWindow.xaml(.cs)  Mode editor dialog
    ParticleEngine.cs         Ambient particle system
    TelemetryMonitor.cs       CPU/RAM sampling
    NativeMethods.cs          Platform interop
    Models/
      ArcynConfig.cs          Root config model
      ModeConfig.cs           Mode + target models
    Services/
      ConfigService.cs        Config load/save/validate/migrate
      ModeService.cs          Mode CRUD + selection
      LaunchService.cs        Process launch logic
      ThemeService.cs         Brush resolution + presets
      AnimationService.cs     Fade/resize/scale helpers
      RenderService.cs        Render loop subscriber
      LogService.cs           Async file logger
    Styles/
      Theme.xaml              Visual theme + styles
    Assets/Fonts/             JetBrainsMono Nerd Font
```

---

## Build

```bash
cd ARCYN/ARCYN.UI
dotnet restore
dotnet build -c Release
dotnet publish -c Release --self-contained true
```

Requires .NET 8 SDK.

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

---

## License

[MIT](LICENSE)
