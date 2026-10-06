# Contributing to B-helper

Thanks for your interest in B-helper, a lightweight tray app for Bellator / Lecoo Fighter N176B laptops.

## Getting started

1. Fork the repository and create a branch from `main`.
2. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows 10/11 only).
3. Build: `dotnet build BHelper.sln`
4. The app requests Administrator rights (needed for CPU temperature via MSR). Close any running `BHelper.exe` before building.

See [README.md](README.md) for publish and `--debug` instructions.

## Project rules

Read [AGENTS.md](AGENTS.md) and [docs/PROJECT_SPEC.md](docs/PROJECT_SPEC.md) before changing code. The short version:

- Read sensors only through `HardwareMonitorHost` and `SensorReader`; do not create another `Computer`.
- New monitors derive from `PollingMonitorBase` and live in `App/Hardware/`.
- WinForms code stays in `App/Tray/`; power logic stays in `App/Power/`; settings and registry code stays in `App/Utils/`.
- Do not call WMI, `schtasks`, or `Computer.Open()` on the UI thread.
- Do not add packages or frameworks without discussing it first.
- Do not copy code from ASUS or G-Helper projects.
- No emoji in `Console.WriteLine` output.

## Pull requests

- Keep changes focused on one task and the right layer; avoid unrelated refactors.
- Describe what changed and how you tested it (hardware model, Windows version).
- Make sure `dotnet build BHelper.sln` succeeds.

## Reporting bugs and ideas

Open an issue using the templates. For security problems, follow [SECURITY.md](SECURITY.md) instead.

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
