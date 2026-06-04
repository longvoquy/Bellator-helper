# LecooHelper — Hướng dẫn cho AI / Agent

Đọc file này **trước** khi sửa code. Chi tiết đầy đủ nằm trong [`docs/`](docs/).

**Cursor:** rule tự áp dụng trong [`.cursor/rules/`](.cursor/rules/) (`lecoohelper.mdc` mọi chat; `lecoohelper-hardware.mdc` khi mở file `App/Hardware/`).

## Mục tiêu dự án

Ứng dụng **system tray** nhẹ (WinForms, .NET 8) thay Bellator Control Center trên laptop **Lecoo Fighter N176B** (và tương tự). Chỉ Windows 10/11.

## Tài liệu bắt buộc

| File | Mục đích |
|------|----------|
| [docs/PROJECT_SPEC.md](docs/PROJECT_SPEC.md) | Đặc tả kiến trúc, quy tắc đặt code, trạng thái từng module |
| [docs/structure.json](docs/structure.json) | Cấu trúc máy đọc được (JSON) cho tool tự động |
| [README.md](README.md) | Build, publish, `--debug` |

## Quy tắc ngắn (không vi phạm)

1. **Một nguồn sensor:** Mọi đọc LibreHardwareMonitor qua `HardwareMonitorHost` + `SensorReader` — không tạo `Computer` mới ở class khác.
2. **Monitor mới:** Kế thừa `PollingMonitorBase`, đặt trong `App/Hardware/`, expose property + event `Updated`.
3. **UI tray:** Chỉ trong `App/Tray/` — không nhét WinForms vào `Hardware/` hay `Power/`.
4. **Power mode:** Logic trong `App/Power/PowerMode.cs` — UI chỉ gọi `PowerMode.SetMode`.
5. **Settings / registry:** Chỉ `App/Utils/`.
6. **Không** thêm package hoặc framework mới trừ khi user yêu cầu.
7. **Không** copy logic ASUS/G-Helper — chỉ tham khảo pattern tổ chức thư mục.
8. **Không** chạy `dotnet build` / compile — user tự build (theo rule repo).
9. **Console/log:** Không dùng emoji/icon trong `Console.WriteLine`.
10. **Phạm vi thay đổi:** Sửa đúng layer; không refactor lan man file không liên quan task.

## Entry point

- `Program.cs` — single instance (`Mutex`), `--debug` → `HardwareDebugger.DumpAll()`, còn lại → `TrayApp`.
- UAC admin: `app.manifest` (`requireAdministrator`) — cần cho nhiệt độ CPU Intel (MSR).

## Cấu trúc tóm tắt

```
Program.cs
App/
  Hardware/   # Sensor polling (LibreHardwareMonitor, WMI, PerformanceCounter)
  Power/      # Performance profiles (chưa điều khiển phần cứng thật)
  Tray/       # NotifyIcon, menu, SettingsForm (dashboard)
  Utils/      # Settings JSON, AutoStart, AdminHelper, HardwareDebugger
Resources/
  tray_icon.ico
```

## Module chưa gắn UI (đừng duplicate)

- `RamMonitor`, `FanMonitor` — đã có class, **chưa** dùng trong `TrayApp`. Khi cần hiển thị RAM/fan: wire vào `TrayApp` + mở rộng `HardwareSnapshot`, không tạo monitor trùng.

## Tham chiếu nhanh trạng thái

| Khu vực | Trạng thái |
|---------|------------|
| CPU/GPU monitor + tray | Hoạt động |
| SettingsForm (dashboard) | Hoạt động |
| PowerMode.SetMode | Stub (`return false`) — chờ dump phần cứng |
| RamMonitor / FanMonitor | Code sẵn, chưa integrate |
| AutoStart trong UI | Kiểm tra `SettingsForm` / settings |

Khi không chắc đặt file ở đâu → mở [docs/PROJECT_SPEC.md](docs/PROJECT_SPEC.md) mục **Ma trận trách nhiệm** và **Checklist thêm tính năng**.
