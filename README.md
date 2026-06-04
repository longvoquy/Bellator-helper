# Lecoo Helper

Lightweight system tray app for **Lecoo Fighter N176B** (and similar models), replacing the Bellator control center.

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
cd LecooHelper
dotnet build
```

## Publish (single-folder exe)

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

Output: `bin\Release\net8.0-windows\win-x64\publish\`

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
  tray_icon.ico
```

For architecture rules and where to put new code, see [docs/PROJECT_SPEC.md](docs/PROJECT_SPEC.md).

## Roadmap

- Phase 1: Hardware monitoring (done)
- Phase 2: Tray UI + settings window (G-Helper style toggle) (done)
- Phase 3: Power modes (requires `--debug` dump on real hardware)
- Phase 4: Auto-start and settings persistence
- Phase 5: Hardware debugger (`--debug`)
