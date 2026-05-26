# Contributing to ARCYN

Thanks for your interest! ARCYN is a lightweight project — contributions are welcome but keep it simple.

## Getting started

1. Fork the repo
2. Create a branch: `git checkout -b feature/your-feature`
3. Make changes
4. Build: `cd ARCYN/ARCYN.UI && dotnet build -c Release`
5. Submit a PR

## Code conventions

- **Language**: C# 12, .NET 8, WPF
- **Style**: Follow existing code — brace positions, naming, patterns
- **No XML comments** — only `//` when the "why" isn't obvious
- **No personal paths** — all config must be user-defined in `arcyn.json`
- **One concern per file** — services for logic, XAML for UI, models for data

## Project structure

```
ARCYN.UI/
  Models/        — Data models (no UI code)
  Services/      — Business logic (no direct UI references)
  Styles/        — XAML themes and templates
  Assets/        — Fonts and resources
  *.xaml(.cs)    — Windows and code-behind
```

## Pull request guidelines

- One feature/fix per PR
- Rebase on main before submitting
- Ensure `dotnet build -c Release` passes with 0 errors, 0 warnings
- No breaking changes to `arcyn.json` schema without migration support
- Test manually: run the app, create a mode, launch it, verify keyboard nav

## Reporting issues

Use the [issue templates](.github/ISSUE_TEMPLATE/) — include:
- ArcYN version / build date
- Steps to reproduce
- Expected vs actual behavior
- Config file (redact personal paths)
