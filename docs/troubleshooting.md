# Troubleshooting

Use these fixes from the repository root unless a step says otherwise.

## `dotnet: command not found`

Your system does not have the .NET SDK installed or it is not on `PATH`.

Check:

```bash
dotnet --version
```

Fix: install the .NET 8 SDK from your distribution packages or from Microsoft, then open a new terminal and rerun:

```bash
./scripts/setup-linux.sh
```

## Wrong .NET Version

ARCYN targets .NET 8. Check installed SDKs:

```bash
dotnet --list-sdks
```

Fix: install a `8.x` SDK. Runtime-only installs are not enough for building from source.

## `Permission denied` When Running Scripts

The shell scripts are not executable on your machine.

Fix:

```bash
chmod +x scripts/*.sh
```

Then rerun the command.

## `xdg-open: command not found`

ARCYN uses `xdg-open` to open folders and websites.

Check:

```bash
command -v xdg-open
```

Fix on Debian/Ubuntu:

```bash
sudo apt update
sudo apt install xdg-utils
```

Fix on Fedora:

```bash
sudo dnf install xdg-utils
```

Fix on Arch:

```bash
sudo pacman -S xdg-utils
```

## App Starts But No Window Appears

You may not have a graphical Linux session available.

Check:

```bash
echo "$DISPLAY"
echo "$WAYLAND_DISPLAY"
```

At least one should usually be set in a desktop session. If you are connected over SSH, run ARCYN on the desktop machine or configure X11/Wayland forwarding.

## No Modes Are Shown

ARCYN could not find a valid config.

Create one:

```bash
mkdir -p ~/.config/ARCYN
cp ARCYN/example.arcyn.json ~/.config/ARCYN/arcyn.json
```

Then click `Reload config` in ARCYN or restart the app.

## Config File Is Malformed

Validate the JSON:

```bash
python3 -m json.tool ~/.config/ARCYN/arcyn.json
```

Fix any reported syntax errors. Common mistakes are trailing commas, missing quotes, and unescaped backslashes copied from Windows paths.

## A Configured App Does Not Launch

Confirm the command exists:

```bash
command -v code
```

Replace `code` with the command from your config. If nothing prints, install that app or use the full executable path.

## A Folder Does Not Open

Confirm the folder exists:

```bash
test -d /home/you/projects && echo ok
```

Fix the path in `~/.config/ARCYN/arcyn.json`. Use Linux paths, not Windows paths.

## A Website Does Not Open

Confirm the URL starts with `http://` or `https://`.

Good:

```text
https://github.com
```

Bad:

```text
github.com
```

Also confirm `xdg-open` works:

```bash
xdg-open https://github.com
```

## Publish Output Is Missing

Run:

```bash
./scripts/publish-linux.sh
```

The expected binary is:

```text
dist/ARCYN-linux-x64/ARCYN
```

## Shortcut Does Not Launch a Mode

Check the following, in order:

1. **ARCYN must be the focused window.** Shortcuts are scoped to the window, not the whole desktop. Click the ARCYN title bar first.
2. **The shortcut parses.** Re-check the format. The last segment must be the key, modifiers must come first, only one key per combo. Examples that work: `Ctrl+Alt+1`, `Super+K`, `F5`, `Escape`. Examples that do **not**: `1+Ctrl` (reversed), `Ctrl+` (trailing modifier), `Ctrl+Alt+Ctrl+1` (duplicate modifier), `Foo+Bar` (unknown key).
3. **Reload the config.** Click the **Reload config** button in the ARCYN window after editing `arcyn.json`.
4. **Use the implicit digit fallback.** If a mode has no `shortcut` field, press the bare digit `1`–`9` to launch that mode by position.
5. **Press `Esc` to close.** `Esc` always closes the ARCYN window, regardless of focus or any mode.
6. **Check the status text.** If a configured shortcut is invalid, the status text shows `Ignored invalid shortcut for MODE: '<text>'`. The mode is preserved and can still be launched via the button or the digit binding.

## Status Text Reports `Ignored invalid shortcut`

The string in the `shortcut` field did not parse. Fix the format (see the previous section) and reload. Common mistakes are trailing `+`, reversed order, lowercase modifier names with odd casing, and unknown keys.
