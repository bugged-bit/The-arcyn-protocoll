# ARCYN

ARCYN is a Linux‑only desktop workspace launcher built with .NET 8 and Avalonia. It lets you define **modes** that open a set of applications, folders, and websites with a single click – perfect for quickly switching between development, design, or any custom workflow.

---

## 📋 Prerequisites

- **Linux (x64)** – the app runs on any modern distribution.
- **.NET 8 SDK** – required to build from source (`dotnet --list-sdks` should show a version 8.x).
- **xdg‑open** – used internally to launch folders and URLs.
- **Git** – to clone the repository.

> **Tip:** Most distributions already ship `xdg-open`. Install the .NET 8 SDK from Microsoft’s official packages for your distro.

---

## 🚀 Quick Start (One‑liner)

```bash
git clone https://github.com/bugged-bit/ARCYN.git && cd ARCYN && ./scripts/setup-linux.sh && ./scripts/run-linux.sh
```

Setup is **interactive**: after restoring and building, the script drops you into a terminal wizard that creates `~/.config/ARCYN/arcyn.json` and then launches ARCYN automatically. If you'd rather skip the wizard (e.g. in CI), set `ARCYN_NO_WIZARD=1` and the bundled example config is copied for you.

If you hit a *Permission denied* error, make the scripts executable first:

```bash
chmod +x scripts/*.sh
./scripts/setup-linux.sh
./scripts/run-linux.sh
```

---

## ⚙️ First‑time Configuration

The setup script (`./scripts/setup-linux.sh`) is interactive. After the build completes, it drops you into a terminal wizard that creates `~/.config/ARCYN/arcyn.json` for you.

What the wizard does:

- Detects an existing config and asks whether to **re-run**, **keep**, or **view** the current path.
- Offers preset workspace modes — `CODE` (terminal + editor + GitHub), `BROWSE` (research sites), `CREATE` (GIMP + Figma), `STUDY` (Obsidian + Notion) — and a **Custom** blank mode you fill in yourself.
- Lets you add apps, folders, and websites per mode, with folder paths and URLs validated as you type.
- Lets you set behavior (idle timeout, always-on-top, close-on-launch) and optionally customize the theme.
- Previews the final JSON and asks for confirmation before writing.
- Optionally launches ARCYN at the end.

The wizard uses `whiptail` if available, then `dialog`, then a plain bash fallback. Install one of the first two for the best experience:

```bash
# Debian / Ubuntu
sudo apt install newt
# Fedora
sudo dnf install newt
# Arch
sudo pacman -S libnewt
```

### Re-running the wizard

You can re-run the wizard any time to reconfigure:

```bash
./scripts/wizard.sh
```

If `~/.config/ARCYN/arcyn.json` already exists, the wizard will offer to overwrite, keep, or show the current path.

### Manual configuration

If you'd rather edit the file by hand, copy the example and edit it:

```bash
mkdir -p ~/.config/ARCYN
cp ARCYN/example.arcyn.json ~/.config/ARCYN/arcyn.json
nano ~/.config/ARCYN/arcyn.json
```

The schema is `ARCYN/arcyn.schema.json` (Draft-07). The wizard produces a config that matches this example shape, and ARCYN's config loader silently drops invalid folders (nonexistent paths) and invalid URLs, so small mistakes are forgiven.

---

## ⌨️ Keyboard Shortcuts

ARCYN is designed to be driven from the keyboard. Set a `shortcut` on any mode in `arcyn.json`:

```json
{
  "modes": [
    { "name": "CODE",   "shortcut": "Ctrl+Alt+1", "apps": ["code"],            "websites": [], "folders": [] },
    { "name": "BROWSE", "shortcut": "Super+K",    "apps": [],                  "websites": ["https://news.ycombinator.com"], "folders": [] },
    { "name": "CREATE",                            "apps": ["gimp"],            "websites": [], "folders": [] }
  ]
}
```

Accepted formats:

- **Modifiers** – one or more of `Ctrl`, `Alt`, `Shift`, `Meta`, `Super` (also `Cmd` / `Win`), joined by `+`.
- **Key** – a digit `0`–`9`, a letter `A`–`Z`, a function key `F1`–`F24`, or a named key (`Escape`, `Tab`, `Space`, `Enter`, `Insert`, `Delete`, `Home`, `End`, `PageUp`, `PageDown`, `Up`, `Down`, `Left`, `Right`).
- Modifiers come **before** the key. `Ctrl+1`, `Ctrl+Alt+Shift+F11`, `Super+K` are all valid.

Behaviour:

- A configured `shortcut` launches the matching mode when ARCYN is the focused window.
- If a mode has no `shortcut`, the **bare digit** `1`–`9` launches that mode by position.
- `Esc` always closes the ARCYN window.
- Invalid `shortcut` strings are ignored with a clear warning in the status text; the mode is still usable via the Launch button or the implicit digit binding.

**Note:** Shortcuts are scoped to the ARCYN window. This keeps the app reliable across X11 and Wayland with no extra Linux dependencies. Global system-wide hotkeys would require `libx11-dev` and only work on X11.

---

## 🛠️ Building from Source

The convenience script handles dependency checks, restores NuGet packages, and builds in Release mode:

```bash
./scripts/setup-linux.sh
```

**Manual steps** (if you prefer the raw `dotnet` commands):
```bash
# Restore NuGet packages
dotnet restore ARCYN/ARCYN.sln
# Build the solution
dotnet build ARCYN/ARCYN.sln -c Release
```

---

## ▶️ Running the App

From the checkout directory:
```bash
./scripts/run-linux.sh
```

Or launch the compiled binary directly:
```bash
./dist/ARCYN-linux-x64/ARCYN
```

---

## 📦 Publishing a Standalone Linux Build

```bash
./scripts/publish-linux.sh
```

The self‑contained binary is written to `dist/ARCYN-linux-x64/`. Distribute the entire folder or copy the `ARCYN` executable to `/usr/local/bin`.

---

## ✅ Development Checks & Tests

Run the full test suite (including the optional UI smoke test) with:
```bash
./scripts/test-linux.sh
```

**Manual equivalents**:
```bash
# Restore & build
dotnet restore ARCYN/ARCYN.sln
dotnet build ARCYN/ARCYN.sln -c Release
# Run .NET unit tests
dotnet test ARCYN/ARCYN.sln -c Release
```

*UI smoke test*: `tests/ui.test.ts` requires Node.js, a Linux display server, or `xvfb‑run`.

---

## 🐞 Troubleshooting

See the dedicated [troubleshooting guide](docs/troubleshooting.md) for common issues such as:
- Missing .NET 8 SDK
- Script permission errors
- `xdg-open` not found
- Configuration JSON errors
- Display problems for the UI test
- Setup wizard skipped in a non-interactive shell (CI, `curl | bash`, no TTY) — run `./scripts/wizard.sh` in a real terminal to customize

---

## 📂 Project Layout

```text
ARCYN/
  ARCYN.sln                # Solution file
  ARCYN.Avalonia/          # Avalonia UI project (Linux desktop)
  ARCYN.Core/              # Core logic – config parsing, mode handling
  tests/                   # .NET unit tests + optional UI test
  arcyn.schema.json        # JSON schema for the config file
  example.arcyn.json       # Starter configuration
scripts/                   # Helper scripts: setup, wizard, run, test, publish
docs/                       # Screenshots and troubleshooting docs
```

---

## 🤝 Contributing

Read the full contribution guide in [CONTRIBUTING.md](CONTRIBUTING.md) before opening pull requests.

---

## 📄 License

MIT – see the [LICENSE](LICENSE) file for details.

---

*Happy hacking! 🎉*

ARCYN is a Linux desktop workspace launcher. It opens the apps, folders, and websites for a saved workspace mode with one click.

![ARCYN screenshot](docs/screenshot.png)

## Supported Platform

ARCYN is Linux-only. The app targets .NET 8 and uses Avalonia for the desktop UI.

Tested deployment target:

- Linux x64
- .NET 8 SDK for building from source
- `xdg-open` for opening folders and websites

## Quick Start

Run these commands from a terminal:

```bash
git clone https://github.com/bugged-bit/ARCYN.git
cd ARCYN
./scripts/setup-linux.sh
./scripts/run-linux.sh
```

Setup is **interactive**: after restoring and building, the script drops you into a terminal wizard that creates `~/.config/ARCYN/arcyn.json` and then launches ARCYN automatically. In CI or non-interactive shells, set `ARCYN_NO_WIZARD=1` to skip the wizard and copy the bundled example config.

If your shell says `Permission denied`, run:

```bash
chmod +x scripts/*.sh
./scripts/setup-linux.sh
./scripts/run-linux.sh
```

## First Configuration

`./scripts/setup-linux.sh` is interactive. After building, it runs `scripts/wizard.sh`, which:

- Detects an existing `~/.config/ARCYN/arcyn.json` and asks whether to re-run, keep, or view the current path.
- Offers preset modes (`CODE`, `BROWSE`, `CREATE`, `STUDY`) and a blank **Custom** mode.
- Lets you add apps, folders, websites, and a keyboard shortcut per mode, with folder and URL validation as you type.
- Previews the final JSON, writes it on confirmation, and optionally launches ARCYN.

The wizard uses `whiptail` if installed, then `dialog`, then a plain bash fallback. For the best experience, install one of the first two:

```bash
# Debian / Ubuntu
sudo apt install newt
# Fedora
sudo dnf install newt
# Arch
sudo pacman -S libnewt
```

Re-run the wizard any time with `./scripts/wizard.sh`.

If you'd rather edit the file by hand, copy the included example:

```bash
mkdir -p ~/.config/ARCYN
cp ARCYN/example.arcyn.json ~/.config/ARCYN/arcyn.json
nano ~/.config/ARCYN/arcyn.json
```

Use Linux commands that exist on your machine. Good examples are:

- `code`
- `firefox`
- `gnome-terminal`
- `/usr/bin/nautilus`

Folders should be Linux paths, for example:

```text
/home/you/projects
```

Websites must start with `http://` or `https://`.

## Example Config

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
      "description": "Development workspace",
      "accent": "#D64545",
      "apps": ["gnome-terminal", "code"],
      "websites": ["https://github.com"],
      "folders": ["/home/you/projects"]
    }
  ]
}
```

## Build From Source

```bash
./scripts/setup-linux.sh
```

That script checks Linux, checks .NET 8, restores packages, and builds the app.

Manual equivalent:

```bash
dotnet restore ARCYN/ARCYN.sln
dotnet build ARCYN/ARCYN.sln -c Release
```

## Run From Source

```bash
./scripts/run-linux.sh
```

Manual equivalent:

```bash
dotnet run --project ARCYN/ARCYN.Avalonia/ARCYN.Avalonia.csproj
```

## Publish A Standalone Linux Build

```bash
./scripts/publish-linux.sh
```

The published app is written to:

```text
dist/ARCYN-linux-x64/
```

Run it with:

```bash
./dist/ARCYN-linux-x64/ARCYN
```

## Developer Checks

Run all normal checks:

```bash
./scripts/test-linux.sh
```

Manual equivalent:

```bash
dotnet restore ARCYN/ARCYN.sln
dotnet build ARCYN/ARCYN.sln -c Release
dotnet test ARCYN/ARCYN.sln -c Release
```

The optional UI smoke test is in `tests/ui.test.ts`. It needs Node.js dependencies and a Linux display or `xvfb-run`.

## Troubleshooting

See [docs/troubleshooting.md](docs/troubleshooting.md) for exact fixes for missing .NET, script permissions, missing `xdg-open`, config errors, and display problems.

## Project Structure

```text
ARCYN/
  ARCYN.sln
  ARCYN.Avalonia/        Linux desktop app
  ARCYN.Core/            Config, modes, and launch logic
  tests/                 .NET unit tests
  arcyn.schema.json      JSON schema for config files
  example.arcyn.json     Starter config
scripts/                 Linux setup, wizard, run, test, and publish helpers
docs/                    Screenshot and troubleshooting
tests/                   Optional UI smoke test
```

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md).

## License

MIT. See [LICENSE](LICENSE).
