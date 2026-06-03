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
git clone https://github.com/bugged-bit/The-arcyn-protocoll.git && cd The-arcyn-protocoll && ./scripts/setup-linux.sh && ./scripts/run-linux.sh
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

## 🌐 Global Shortcut

ARCYN supports a **system‑wide global shortcut** that brings the ARCYN window to the front or launches it from any desktop — even when ARCYN is not focused. This shortcut is registered with the Linux desktop environment via a freedesktop.org portal.

The global shortcut is configured during the interactive wizard (`./scripts/wizard.sh`). When prompted, choose a key combination (e.g., `Ctrl+Alt+A`) to use as the global toggle. The wizard installs a `.desktop` file at `~/.local/share/applications/ARCYN.desktop` that registers the shortcut with your DE.

### Desktop Environment Notes

- **KDE Plasma** — The shortcut should work automatically after the wizard completes. You can verify or change it in *System Settings → Shortcuts → Custom Shortcuts*.
- **GNOME** — GNOME does not honour the portal shortcut by default. Use *Settings → Keyboard → Keyboard Shortcuts → Custom Shortcuts* to add an ARCYN entry pointing to the `ARCYN` command, or register it from the terminal:
  ```bash
  gsettings set org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:/org/gnome/settings-daemon/plugins/media-keys/custom-keybindings/arcyn/ name 'ARCYN'
  gsettings set org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:/org/gnome/settings-daemon/plugins/media-keys/custom-keybindings/arcyn/ command 'ARCYN'
  gsettings set org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:/org/gnome/settings-daemon/plugins/media-keys/custom-keybindings/arcyn/ binding '<Ctrl><Alt>A'
  ```
- **Sway / i3** — Add a line to your config (`~/.config/sway/config` or `~/.config/i3/config`):
  ```bash
  bindsym $mod+Shift+A exec ARCYN
  ```
  Replace `$mod+Shift+A` with your preferred combination.
- **Cinnamon** — Open *System Settings → Keyboard → Shortcuts → Custom Shortcuts*, add a new shortcut pointing to `ARCYN`.
- **XFCE** — Open *Settings → Keyboard → Application Shortcuts*, click *Add*, enter `ARCYN`, and press your chosen key combination.

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
