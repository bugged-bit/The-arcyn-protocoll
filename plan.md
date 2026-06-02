# ARCYN Linux-Only GitHub Deployment Hardening Plan

## 0. Purpose and Non-Negotiable Outcome

This repository must become a clean, Linux-only, GitHub-ready project that a new user can clone, understand, build, run, test, and troubleshoot without guessing. The finished repository should not feel like a migration-in-progress, a mixed Windows/Linux experiment, or a private working folder. It should feel like a maintained public Linux application with clear setup instructions, predictable commands, guarded scripts, and no stray artifacts.

The desired end state is:

- A Linux-only ARCYN project centered on the Avalonia desktop application.
- A repository root that contains only useful, intentional project files.
- A simple first-run path for users who only want to run the app.
- A simple developer path for users who want to build from source.
- A CI workflow that proves the project builds and tests on Linux.
- Documentation that explains every required command, where to run it, what it does, and what to do if it fails.
- Packaging guidance or scripts that produce a Linux release artifact without requiring the user to know .NET publishing internals.
- No stale Windows instructions, WPF-first project structure claims, broken package metadata, ignored required files, old exports, generated session logs, or confusing leftovers.

## 1. Current Repository Findings

These findings are based on a read-only inspection before any code edits.

### 1.1 Repository Shape

The repository currently contains:

- `.github/`
- `ARCYN/`
- `docs/`
- `src/`
- `tests/`
- `node_modules/`
- `.gitignore`
- `arcyn_export.json`
- `CHANGELOG.md`
- `CODE_OF_CONDUCT.md`
- `CONTRIBUTING.md`
- `LICENSE`
- `package-lock.json`
- `package.json`
- `README.md`
- `session-ses_1901.md`
- a root file matching `The-arcyn-proc...`

The actual .NET solution lives under `ARCYN/` and contains:

- `ARCYN.sln`
- `ARCYN.Core/`
- `ARCYN.Avalonia/`
- `ARCYN.UI/`
- `tests/ARCYN.Core.Tests/`
- `arcyn.schema.json`
- `example.arcyn.json`

### 1.2 Linux-Only Goal Conflicts

The solution currently includes `ARCYN.UI`, which is a WPF-oriented project. WPF is Windows-only. Even though `ARCYN.UI.csproj` has conditional targeting for `net8.0` and `net8.0-windows`, its presence creates confusion for a Linux-only repository.

The Avalonia project currently references both:

- `ARCYN.Core`
- `ARCYN.UI`

That means the Linux Avalonia application is still coupled to a project named `ARCYN.UI`, which contains WPF assets and Windows-specific source. For a Linux-only public project, this is confusing and fragile. Any shared UI/view model/service code should either be moved into a neutral shared project or kept directly in the Avalonia project. Since the user asked for a clean Linux-only project, the likely cleanup path is to remove the WPF project from the published solution and keep only Linux-compatible projects.

### 1.3 Documentation Conflicts

`README.md` currently says the project supports Linux, but it still includes Windows/WPF build instructions. It also contains mojibake text caused by broken character encoding, such as garbled block characters and arrows.

The README also presents a migration checklist, which makes the repository look like an unfinished internal migration rather than a ready-to-use public project.

The project structure section describes Windows/WPF files heavily and does not clearly identify the Linux-only app entry point.

### 1.4 Root Metadata Conflicts

`package.json` currently has:

- A garbled description.
- A placeholder `test` script that always fails.
- `license` set to `ISC`, while the repository `LICENSE` and README badge say MIT.
- Repository URLs pointing to `The-arcyn-protocoll`, while README badges point to `ARCYN`.
- Playwright dependencies, but no clean Linux-only test workflow described at the root.

For a public GitHub deployment, root metadata should not contradict the project identity or license.

### 1.5 Stray or Generated Files

The following root files are likely not suitable for a clean public repository:

- `session-ses_1901.md`: generated session log, large, not useful to users.
- `arcyn_export.json`: export artifact, currently ignored by `.gitignore`.
- `The-arcyn-proc...`: likely old/generated/temporary file.
- `node_modules/`: dependency install directory, should not be committed.

The cleanup must distinguish between tracked files and untracked files. If a file is tracked but should not be public, remove it from the repository. If it is untracked/generated, leave it ignored or delete it locally only when safe.

### 1.6 `.gitignore` Problems

`.gitignore` currently ignores `plan.md`. The user explicitly requested `plan.md` in the project folder, so this ignore rule is wrong for this task.

`.gitignore` also includes reasonable generated-file entries such as:

- `**/bin/`
- `**/obj/`
- `dist/`
- `publish/`
- `node_modules/`
- `session-*.md`
- `arcyn_export.json`

The ignore file should be cleaned so it protects the repo from generated files while not hiding intentional documentation.

### 1.7 Build and Test Concerns

The .NET solution currently references the WPF project and tests. A Linux-only CI pipeline should:

- Restore the Linux-compatible solution or project.
- Build with `Release` configuration.
- Run .NET tests.
- Publish the Avalonia app for `linux-x64`.
- Optionally validate the produced binary.

The root Playwright test launches the built binary and tries to verify a visible window using Linux tools such as `xdotool` or `wmctrl`. This can be useful in CI, but it must be run under a virtual display such as `xvfb-run`, and the docs must tell users that UI smoke tests require display support.

### 1.8 Error Handling and User Setup Concerns

The app already has configuration service logic and a launcher service. The docs claim first-run setup and CLI setup, but the actual Linux/Avalonia entry point and user-facing setup behavior must be verified before documenting it as guaranteed.

The project needs a user-friendly setup wrapper that reduces mistakes. Ideally, a new Linux user should be able to run one obvious command such as:

```bash
./scripts/setup-linux.sh
```

or:

```bash
./scripts/run-linux.sh
```

The scripts should:

- Check that they are being run on Linux.
- Check whether `dotnet` exists.
- Check that the installed .NET SDK is compatible with .NET 8.
- Restore dependencies.
- Build the app.
- Tell the user exactly what command to run next.
- Print clear failure messages when a prerequisite is missing.

## 2. Work Order

The work must happen in this order because the user explicitly required a plan before code changes:

1. Create this `plan.md` file in the repository root.
2. Remove the `.gitignore` rule that ignores `plan.md`.
3. Identify tracked versus untracked/generated files.
4. Clean repository metadata and documentation.
5. Convert the solution and project references to a Linux-only shape.
6. Add user-friendly Linux setup/run/publish scripts if they do not already exist.
7. Update CI for Linux-only validation.
8. Verify build, tests, and publish commands.
9. Re-check git status and summarize exactly what changed.

## 3. Target Repository Structure

The final repository should be understandable from the root. A recommended final structure is:

```text
.
├── .github/
│   └── workflows/
│       └── linux.yml
├── ARCYN/
│   ├── ARCYN.sln
│   ├── ARCYN.Avalonia/
│   ├── ARCYN.Core/
│   ├── tests/
│   │   └── ARCYN.Core.Tests/
│   ├── arcyn.schema.json
│   └── example.arcyn.json
├── docs/
│   ├── screenshot.png
│   └── troubleshooting.md
├── scripts/
│   ├── setup-linux.sh
│   ├── run-linux.sh
│   ├── test-linux.sh
│   └── publish-linux.sh
├── tests/
│   └── ui.test.ts
├── .gitignore
├── CHANGELOG.md
├── CODE_OF_CONDUCT.md
├── CONTRIBUTING.md
├── LICENSE
├── README.md
├── package-lock.json
├── package.json
└── plan.md
```

If `src/` is empty or unused, remove it. If it contains real code, document its purpose or move the contents into the appropriate project area.

## 4. Linux-Only Code and Project Cleanup

### 4.1 Remove WPF from the Public Build Surface

The Linux-only repository should not ask users to build `ARCYN.UI`. The solution should contain only:

- `ARCYN.Core`
- `ARCYN.Avalonia`
- `ARCYN.Core.Tests`

The WPF project should either be removed from the solution or moved out of the deployable build path. If Avalonia depends on shared view models or services inside `ARCYN.UI`, those shared pieces must be moved to a Linux-compatible location.

Possible approaches:

- Preferred: move shared non-WPF classes from `ARCYN.UI` into `ARCYN.Core` or a new neutral project such as `ARCYN.Shared`, then remove the `ARCYN.UI` reference from `ARCYN.Avalonia`.
- Conservative: keep `ARCYN.UI` on disk temporarily but remove it from `ARCYN.sln`, remove Windows docs, and ensure `ARCYN.Avalonia` no longer references it.
- Strict cleanup: delete the WPF project once no Linux-compatible code depends on it.

The preferred outcome is strict cleanup, but only after verifying the Avalonia build does not need any WPF-only files.

### 4.2 Verify Avalonia Entry Point

Check these files:

- `ARCYN/ARCYN.Avalonia/Program.cs`
- `ARCYN/ARCYN.Avalonia/App.axaml`
- `ARCYN/ARCYN.Avalonia/App.axaml.cs`
- `ARCYN/ARCYN.Avalonia/MainWindow.axaml`
- `ARCYN/ARCYN.Avalonia/MainWindow.axaml.cs`
- `ARCYN/ARCYN.Avalonia/Views/MainWindow.axaml`
- `ARCYN/ARCYN.Avalonia/Views/MainWindow.axaml.cs`
- `ARCYN/ARCYN.Avalonia/ViewModels/MainWindowViewModel.cs`

Resolve duplicate windows or duplicate view model files if they are unused. A user should not see two competing UI trees in the project.

### 4.3 Make Linux Launching Robust

Review `ARCYN.Core/Services/LaunchService.cs` for Linux behavior:

- Folders should open with `xdg-open`.
- Websites should open with `xdg-open`.
- Apps should launch from PATH or absolute Linux paths.
- Windows-only `.lnk` handling should be removed or guarded as unsupported on Linux.
- Windows drive path detection should be removed from Linux-only logic.
- Error strings should be clear enough to show in logs or UI.

Linux-specific examples:

- App: `code`
- App: `firefox`
- Folder: `/home/alex/projects`
- Website: `https://github.com`

### 4.4 Make Config Paths Linux-Friendly

Review `ConfigService`:

- Prefer `~/.config/ARCYN/arcyn.json`.
- Keep portable `./arcyn.json` behavior only if clearly documented.
- Avoid relying on Windows application-data semantics.
- Preserve migration only if it helps existing users and does not confuse new users.

### 4.5 Remove Windows-Only Metadata

Remove or isolate:

- `app.manifest` in Linux-only build paths unless Avalonia specifically needs it.
- WPF XAML documentation.
- Windows publish commands.
- Windows-only global hotkey claims unless implemented on Linux.

## 5. User-Friendly Scripts

Add a root `scripts/` directory if it does not exist.

### 5.1 `scripts/setup-linux.sh`

Purpose:

- Prepare the project for a first-time Linux user.

Behavior:

- Exit immediately on errors.
- Confirm OS is Linux.
- Confirm repository root is correct.
- Confirm `dotnet` exists.
- Confirm .NET SDK 8.x is available.
- Restore the solution.
- Build the Avalonia app in Release mode.
- Print the run command.

Expected user command:

```bash
./scripts/setup-linux.sh
```

### 5.2 `scripts/run-linux.sh`

Purpose:

- Run ARCYN from source with one command.

Behavior:

- Confirm OS is Linux.
- Confirm `dotnet` exists.
- Run the Avalonia project.

Expected user command:

```bash
./scripts/run-linux.sh
```

### 5.3 `scripts/test-linux.sh`

Purpose:

- Run all developer checks.

Behavior:

- Run .NET restore.
- Run .NET tests.
- Build Release.
- Optionally run Node/Playwright binary validation if dependencies are installed.

Expected user command:

```bash
./scripts/test-linux.sh
```

### 5.4 `scripts/publish-linux.sh`

Purpose:

- Produce a Linux release artifact.

Behavior:

- Confirm OS is Linux.
- Publish `ARCYN.Avalonia` for `linux-x64`.
- Use Release configuration.
- Place output under `dist/ARCYN-linux-x64/`.
- Print the exact binary path.

Expected user command:

```bash
./scripts/publish-linux.sh
```

## 6. Documentation Rewrite Plan

### 6.1 README Goals

Rewrite `README.md` so it answers these questions immediately:

- What is ARCYN?
- Who is it for?
- What platforms are supported?
- How do I run it on Linux?
- How do I build it from source?
- How do I publish a standalone binary?
- Where is the config file?
- How do I fix common errors?
- How do I report an issue?

### 6.2 README Structure

Recommended README sections:

1. Project title and one-sentence description.
2. Screenshot.
3. Supported platform: Linux only.
4. Quick start for users.
5. Build from source.
6. Publish a standalone Linux binary.
7. First-run setup and config location.
8. Example config.
9. Troubleshooting.
10. Developer commands.
11. Project structure.
12. Contributing.
13. License.

### 6.3 Remove Confusing README Content

Remove:

- Windows/WPF commands.
- Migration checklist.
- Broken mojibake ASCII art.
- Claims that are not verified.
- Internal implementation detail that does not help users.

### 6.4 Troubleshooting Coverage

Add a troubleshooting section or `docs/troubleshooting.md` covering:

- `dotnet: command not found`
- wrong .NET SDK version
- `xdg-open: command not found`
- app starts but no window appears
- permission denied on scripts
- missing Linux display environment
- config file malformed
- app command in a mode does not launch
- folder path does not exist
- browser does not open

Each error should include:

- What it means.
- How to confirm it.
- Exact command to fix it where practical.

## 7. GitHub Readiness

### 7.1 GitHub Actions

Create or update `.github/workflows/linux.yml`.

Workflow requirements:

- Run on `push`.
- Run on `pull_request`.
- Use `ubuntu-latest`.
- Install .NET 8 SDK.
- Restore.
- Build Release.
- Run .NET tests.
- Publish Linux artifact.
- Upload artifact.

Optional UI smoke test:

- Install `xvfb`, `xdotool`, and `wmctrl`.
- Run the published binary under `xvfb-run`.
- Run Playwright validation only when it is stable.

### 7.2 Release Readiness

The publish script and CI should agree on output paths. Recommended artifact:

```text
dist/ARCYN-linux-x64/
```

The README should explain that users can run:

```bash
./ARCYN
```

from inside the published folder.

### 7.3 Repository Metadata

Update `package.json`:

- Name should match the project.
- Description should be plain text.
- License should match `LICENSE`, likely `MIT`.
- Test scripts should be real or not present.
- Repository URLs should match the intended GitHub repository.

If Node is only used for optional UI smoke tests, document that clearly.

## 8. Cleanup Plan for Stray Files

### 8.1 Files to Remove or Keep Out of Git

Candidates for removal from public repo:

- `session-ses_1901.md`
- `arcyn_export.json`
- root file matching `The-arcyn-proc*`
- `node_modules/`
- empty or unused `src/`

Before deleting tracked files, check `git status` and `git ls-files` to confirm whether they are tracked.

### 8.2 Files to Keep

Keep:

- `README.md`
- `LICENSE`
- `CHANGELOG.md`
- `CODE_OF_CONDUCT.md`
- `CONTRIBUTING.md`, but update if it references unsupported platforms.
- `docs/screenshot.png` if the screenshot is current.
- `ARCYN/example.arcyn.json`
- `ARCYN/arcyn.schema.json`
- `tests/ui.test.ts` if CI or docs use it.
- `plan.md`, because the user specifically requested it.

## 9. Verification Plan

The final verification should run as much as possible from this workspace:

1. `git status --short`
2. `dotnet restore ARCYN/ARCYN.sln`
3. `dotnet build ARCYN/ARCYN.sln -c Release`
4. `dotnet test ARCYN/ARCYN.sln -c Release`
5. `dotnet publish ARCYN/ARCYN.Avalonia/ARCYN.Avalonia.csproj -c Release -r linux-x64 --self-contained true -o dist/ARCYN-linux-x64`
6. `npm test` if root scripts are updated to a meaningful non-placeholder command.

Because the current environment is Windows, true Linux GUI execution cannot be fully proven locally unless WSL or a Linux CI runner is used. The repository should therefore rely on GitHub Actions `ubuntu-latest` for Linux proof.

## 10. Acceptance Checklist

The project is ready when all of these are true:

- `plan.md` exists and is not ignored.
- README contains only Linux instructions.
- README has exact commands for setup, run, test, and publish.
- README explains config location and first-run behavior.
- Troubleshooting covers common setup failures.
- `.gitignore` ignores generated artifacts but not required docs.
- `package.json` metadata is clean and license-consistent.
- No root session logs or export artifacts remain in the public tree.
- The solution no longer exposes WPF as part of the Linux build.
- Avalonia builds without depending on Windows-only code.
- Linux publish output goes to a predictable `dist/` folder.
- GitHub Actions validate Linux restore/build/test/publish.
- The final `git status --short` contains only intentional changes.

## 11. Risk Notes

- Removing `ARCYN.UI` may require moving shared view models or services first.
- Avalonia UI files appear duplicated in both root and `Views/`; deleting the wrong one could break startup.
- The current environment is Windows, so Linux runtime verification must be delegated to CI unless WSL or a Linux runner is available.
- The existing README contains encoding damage; rewriting it cleanly is safer than patching individual garbled characters.
- If `node_modules/` or generated files are tracked, removal must be done carefully and intentionally.

## 12. Immediate Next Actions After This Plan

After this file is created, proceed with:

1. Edit `.gitignore` to stop ignoring `plan.md`.
2. Inspect tracked files with `git ls-files`.
3. Inspect `.github/workflows`.
4. Determine whether `ARCYN.UI` is required by Avalonia at compile time.
5. Make the smallest set of code/project changes needed to build Linux-only.
6. Rewrite user-facing docs.
7. Add scripts.
8. Run verification commands and report any environment-limited checks.
