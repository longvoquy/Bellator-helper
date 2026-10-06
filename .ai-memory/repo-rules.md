# Cached Repository Rules

Last refreshed: 2026-06-08

Source rule files:
- `AGENTS.md`
- `.cursor/rules/bhelper.mdc`
- `.cursor/rules/bhelper-hardware.mdc`

Summary:
- Project is BHelper / Bellator Helper, a lightweight Windows 10/11 system tray app using WinForms and .NET 8 for Bellator / Lecoo Fighter N176B and similar laptops.
- Read `AGENTS.md`, `docs/PROJECT_SPEC.md`, `docs/structure.json`, and `README.md` for project conventions when needed.
- Do not run `dotnet build`, compile, or publish unless the user explicitly asks.
- Do not add packages, UI frameworks, dependency injection, or logging frameworks unless explicitly requested.
- Keep changes scoped to the relevant layer and avoid unrelated refactors.
- Hardware code belongs in `App/Hardware/`; new monitors inherit `PollingMonitorBase`, expose properties plus `Updated`, and use `HardwareMonitorHost` plus `SensorReader` / `WmiHelper`.
- `HardwareMonitorHost` is the only place that may create a LibreHardwareMonitor `Computer`.
- Tray/UI code belongs in `App/Tray/`; do not put WinForms code in `Hardware` or `Power`.
- Power mode logic belongs in `App/Power/PowerMode.cs`; UI calls `PowerMode.SetMode`.
- Settings, registry, auto-start, admin/debug helpers belong in `App/Utils/`.
- `SettingsForm` main content uses a vertical `TableLayoutPanel` named `contentLayout`; add or resize UI sections by updating rows/controls there, not by manually calculating `Top`/`Bottom` coordinates.
- Do not duplicate existing `RamMonitor` or `FanMonitor`; wire them into `TrayApp` and extend `HardwareSnapshot` if needed.
- Use the existing `SettingsForm` dashboard for tray UI changes; do not recreate `PopupForm`.
- For UI updates from background monitor events, marshal via `SynchronizationContext.Post`.
- For NotifyIcon behavior on Windows 11, avoid assigning `ContextMenuStrip` directly to the icon; show the menu manually on right click.
- Avoid emoji/icons in `Console.WriteLine`.
- Directory-specific `.cursor/rules/bhelper-hardware.mdc` applies to `App/Hardware/**/*.cs`.
- Rule cache validation should include hidden rule folders such as `.cursor/rules`; skip full rescans only when the listed source hashes still match.
