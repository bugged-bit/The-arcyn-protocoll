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

If you hit a *Permission denied* error, make the scripts executable first:

```bash
chmod +x scripts/*.sh
./scripts/setup-linux.sh
./scripts/run-linux.sh
```

---

## ⚙️ First‑time Configuration

ARCYN reads a JSON file located at `~/.config/ARCYN/arcyn.json`.

1. **Create the config directory and copy the example**:
   ```bash
   mkdir -p ~/.config/ARCYN
   cp ARCYN/example.arcyn.json ~/.config/ARCYN/arcyn.json
   ```
2. **Edit the file** with your favourite editor (e.g. `nano`, `vim`, `code`):
   ```bash
   nano ~/.config/ARCYN/arcyn.json
   ```
3. **Populate the fields**:
   - **Apps** – commands available on your machine (`code`, `firefox`, `gnome-terminal`, `/usr/bin/nautilus`, …).
   - **Folders** – absolute Linux paths, e.g. `/home/you/projects`.
   - **Websites** – full URLs starting with `http://` or `https://`.

### Minimal Example
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
scripts/                     # Helper scripts: setup, run, test, publish
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

If your shell says `Permission denied`, run:

```bash
chmod +x scripts/*.sh
./scripts/setup-linux.sh
./scripts/run-linux.sh
```

## First Configuration

ARCYN reads this config file:

```text
~/.config/ARCYN/arcyn.json
```

Create the folder and copy the included example:

```bash
mkdir -p ~/.config/ARCYN
cp ARCYN/example.arcyn.json ~/.config/ARCYN/arcyn.json
```

Then edit the file:

```bash
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
scripts/                 Linux setup, run, test, and publish helpers
docs/                    Screenshot and troubleshooting
tests/                   Optional UI smoke test
```

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md).

## License

MIT. See [LICENSE](LICENSE).
