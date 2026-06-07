# B-helper (Bellator Helper)

Lightweight system tray app for **Bellator / Lecoo Fighter N176B** (and similar models), replacing the Bellator control center.

## Target hardware

- CPU: Intel i7-13650HX
- GPU: NVIDIA RTX 5060 Laptop (8 GB)
- RAM: 16 GB DDR5
- OS: Windows 11

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11

## CPU temperature

Intel CPU sensors (MSR) require **Administrator**, same as LibreHardwareMonitor.
The app manifest requests elevation via UAC on startup.

Debug dump:

```powershell
dotnet run -- --debug
```

Output: `bin\Debug\net8.0-windows\logs\hardware_dump.txt`

## Build

```powershell
dotnet build BHelper.sln
```

Output exe: `BHelper.exe`

## Publish (single-folder exe)

```powershell
dotnet publish BHelper.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Output: `bin\Release\net8.0-windows\win-x64\publish\`

## Run at startup

Because the app requires Administrator (CPU temperature via MSR), auto-start uses a **Scheduled Task** with highest privileges instead of the HKCU `Run` registry key (which cannot elevate silently at logon).

Enable it from the dashboard checkbox **Run at startup**. The task is named `BHelper` in Task Scheduler.

## Project layout

```
AGENTS.md       AI/agent entry (read before editing)
docs/           PROJECT_SPEC.md + structure.json
App/
  Hardware/   CPU, GPU, RAM, fan monitors
  Power/      Performance modes
  Tray/       NotifyIcon + popup dashboard
  Utils/      Settings, auto-start
Resources/
  gcc.ico
```

For architecture rules and where to put new code, see [docs/PROJECT_SPEC.md](docs/PROJECT_SPEC.md).

## Roadmap

- Phase 1: Hardware monitoring (done)
- Phase 2: Tray UI + settings window (G-Helper style toggle) (done)
- Phase 3: Power modes (requires `--debug` dump on real hardware)
- Phase 4: Auto-start (done) and settings persistence
- Phase 5: Hardware debugger (`--debug`)
