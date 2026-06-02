# Contributing To ARCYN

ARCYN is a Linux-only .NET 8 Avalonia app. Keep contributions focused, documented, and easy for a new Linux user to run.

## Prerequisites

- Linux x64
- .NET 8 SDK
- `xdg-open`
- Node.js 18 or newer only if you work on the optional UI smoke test

## First-Time Setup

```bash
git clone https://github.com/bugged-bit/ARCYN.git
cd ARCYN
chmod +x scripts/*.sh
./scripts/setup-linux.sh
```

## Development Commands

```bash
./scripts/run-linux.sh
./scripts/test-linux.sh
./scripts/publish-linux.sh
```

Manual commands:

```bash
dotnet restore ARCYN/ARCYN.sln
dotnet build ARCYN/ARCYN.sln -c Release
dotnet test ARCYN/ARCYN.sln -c Release
dotnet publish ARCYN/ARCYN.Avalonia/ARCYN.Avalonia.csproj -c Release -r linux-x64 --self-contained true -o dist/ARCYN-linux-x64
```

## Code Guidelines

- Keep the project Linux-only.
- Do not add Windows-only UI projects or WPF dependencies.
- Keep app launch behavior in `ARCYN.Core`.
- Keep desktop UI behavior in `ARCYN.Avalonia`.
- Keep config examples using Linux commands and Linux paths.
- Add tests when changing config parsing, mode behavior, or launch validation.
- Avoid personal paths, generated files, local logs, and build outputs in commits.

## Pull Request Checklist

- `./scripts/test-linux.sh` passes.
- README instructions still match the actual commands.
- No `bin/`, `obj/`, `publish/`, `dist/`, `node_modules/`, logs, session files, or exported configs are committed.
- Config schema changes include an updated example config.
- User-facing errors are specific and actionable.

## Reporting Issues

Use the GitHub issue templates. Include:

- Linux distribution and version
- ARCYN version or commit
- Exact command that failed
- Terminal output
- Relevant `~/.config/ARCYN/arcyn.json` content with personal paths redacted
