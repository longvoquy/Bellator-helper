# BHelper — Đặc tả dự án

> Tài liệu này mô tả **cấu trúc hiện tại** và **quy tắc mở rộng** để mọi platform/agent (Cursor, Claude Code, Copilot, v.v.) chỉnh sửa nhất quán, tránh code lan man và trùng lặp.

**Phiên bản cấu trúc:** 2026-06-01  
**Target:** .NET 8 WinForms, `net8.0-windows`, Windows 10/11 x64

---

## 1. Mục đích & phạm vi

| Mục | Nội dung |
|-----|----------|
| Sản phẩm | App tray hiển thị nhiệt độ/usage CPU-GPU, chọn performance mode, dashboard nhỏ |
| Phần cứng mục tiêu | Intel i7-13650HX, NVIDIA RTX 5060 Laptop, 16GB DDR5, Win11 |
| Không nằm trong phạm vi | Driver độc quyền Lecoo chưa reverse; web/mobile; cross-platform |
| Tham chiếu style | [G-Helper](https://github.com/seerge/g-helper) — **chỉ** cách tách folder, **không** copy code ASUS |

---

## 2. Công nghệ & phụ thuộc

| Thành phần | Chi tiết |
|------------|----------|
| Runtime | .NET 8, Windows Forms |
| Sensors | `LibreHardwareMonitorLib` 0.9.6 — singleton qua `HardwareMonitorHost` |
| CPU usage | `System.Diagnostics.PerformanceCounter` |
| WMI | `System.Management` (trong `WmiHelper`, monitors) |
| Registry | `Microsoft.Win32.Registry` — AutoStart, debug dump |
| Settings | `%APPDATA%\BHelper\settings.json` (`System.Text.Json`) |
| Quyền | `app.manifest` → `requireAdministrator` (nhiệt CPU Intel/MSR) |

**Không thêm** package UI (WPF, Avalonia), DI container, hoặc logging framework trừ khi có yêu cầu rõ ràng.

---

## 3. Cây thư mục (source)

```
BHelper/
├── AGENTS.md                 ← Điểm vào cho AI (đọc trước)
├── README.md                 ← Hướng dẫn người dùng: build, publish, debug
├── Program.cs                ← Main, mutex, --debug
├── BHelper.csproj
├── BHelper.sln
├── app.manifest              ← UAC administrator
├── Plan for BHelper.md   ← Lịch sử plan gốc (tham khảo, có thể lệch code thực tế)
├── docs/
│   ├── PROJECT_SPEC.md       ← File này
│   └── structure.json        ← Metadata máy đọc
├── App/
│   ├── Hardware/
│   ├── Power/
│   ├── Tray/
│   └── Utils/
└── Resources/
    └── tray_icon.ico
```

**Bỏ qua khi tìm kiếm / chỉnh sửa:** `bin/`, `obj/`, `.vs/`, `node_modules/` (nếu có).

---

## 4. Kiến trúc lớp

```
                    ┌─────────────────┐
                    │   Program.cs    │
                    └────────┬────────┘
                             │
              --debug        │         normal
                 │           │           │
                 ▼           ▼           ▼
        HardwareDebugger   Mutex      TrayApp (ApplicationContext)
                                        │
                    ┌───────────────────┼───────────────────┐
                    ▼                   ▼                   ▼
              CpuMonitor           GpuMonitor          SettingsForm
              GpuMonitor...        PowerMode           NotifyIcon + menu
                    │                   │
                    └─────────┬─────────┘
                              ▼
                    HardwareMonitorHost (singleton Computer)
                    SensorReader / WmiHelper
```

### Luồng dữ liệu polling

1. `TrayApp` tạo `CpuMonitor`, `GpuMonitor` với `Settings.UpdateIntervalMs`.
2. Monitor `Start()` → `HardwareMonitorHost.Acquire()` → timer → `UpdateAll()` → `Refresh()` → `Updated` event.
3. `TrayApp` subscribe `Updated` → marshal UI thread → cập nhật menu, icon, `SettingsForm` qua `HardwareSnapshot`.

### Luồng UI tray (Windows 11)

- **Không** gán `ContextMenuStrip` trực tiếp lên `NotifyIcon` (chặn left-click).
- Left-click / double-click → mở `SettingsForm`.
- Right-click → `ContextMenuStrip.Show` thủ công tại `Cursor.Position`.

---

## 5. Namespace & quy ước đặt tên

| Quy tắc | Ví dụ |
|---------|--------|
| Root namespace | `BHelper` |
| App code | `BHelper.App.{Layer}` |
| Layer = tên folder | `BHelper.App.Hardware` |
| Monitor class | `{Part}Monitor` : `PollingMonitorBase` |
| Static helpers | `{Name}Helper` hoặc tên miêu tả (`SensorReader`) |
| UI form | `{Name}Form` trong `Tray/` |
| Internal khi chỉ dùng trong layer | `HardwareMonitorHost`, `SensorReader`, `HardwareSnapshot` |

File **một public type chính** (trừ nested nhỏ). Không gom nhiều monitor vào một file.

---

## 6. Ma trận trách nhiệm (file → vai trò)

### `App/Hardware/`

| File | Trách nhiệm | Ghi chú |
|------|-------------|---------|
| `HardwareMonitorHost.cs` | Singleton `Computer`, ref-count Acquire/Release, `UpdateAll()` | **Duy nhất** nơi `new Computer()` |
| `PollingMonitorBase.cs` | Timer, Acquire/Release, `PollNow()`, event `Updated` | Base cho mọi monitor |
| `SensorReader.cs` | Đọc sensor LHM theo `HardwareType` / `SensorType` | `internal static` |
| `WmiHelper.cs` | Query WMI dùng chung | Tránh duplicate WMI string |
| `CpuMonitor.cs` | Usage %, temp, freq | PerformanceCounter + LHM |
| `GpuMonitor.cs` | Load, temp, VRAM, clock | LHM GpuNvidia |
| `RamMonitor.cs` | RAM used/total % | **Chưa** dùng bởi Tray |
| `FanMonitor.cs` | Fan RPM LHM + WMI fallback | **Chưa** dùng bởi Tray |

### `App/Power/`

| File | Trách nhiệm | Ghi chú |
|------|-------------|---------|
| `PowerModeKind.cs` | Enum trong `PowerMode.cs` | Performance, Balanced, Silent, Turbo |
| `PowerMode.cs` | `SetMode`, `Current` | **Stub** — chưa gọi power plan/registry |
| `PerformanceProfile.cs` | Metadata hiển thị menu (`All`) | UI đọc từ đây, không hardcode tên mode |

### `App/Tray/`

| File | Trách nhiệm | Ghi chú |
|------|-------------|---------|
| `TrayApp.cs` | `ApplicationContext`, NotifyIcon, menu, lifecycle monitors | Orchestrator UI |
| `SettingsForm.cs` | Dashboard popup (thay PopupForm trong plan cũ) | Dark theme qua `TrayTheme` |
| `HardwareSnapshot.cs` | DTO readonly CPU/GPU temp cho UI | Mở rộng khi thêm RAM/fan |
| `TrayIconHelper.cs` | Icon màu theo nhiệt độ | Dispose icon cũ khi đổi |
| `TrayTheme.cs` | Màu nền/chữ UI | `#0A0E1A`, cyan text |

### `App/Utils/`

| File | Trách nhiệm | Ghi chú |
|------|-------------|---------|
| `Settings.cs` | Load/Save JSON AppData | |
| `AutoStart.cs` | HKCU `Run` key `BHelper` | |
| `AdminHelper.cs` | Kiểm tra quyền admin | Hiển thị gợi ý CPU temp |
| `HardwareDebugger.cs` | `--debug` dump sensors/WMI/registry | Output `logs/hardware_dump.txt` |

### Root

| File | Trách nhiệm |
|------|-------------|
| `Program.cs` | Args, mutex, khởi chạy |

---

## 7. Phụ thuộc được phép / cấm

### Ma trận phụ thuộc (layer được import layer)

|  | Hardware | Power | Tray | Utils |
|--|:--------:|:-----:|:----:|:-----:|
| **Hardware** | ✓ | ✗ | ✗ | ✗ |
| **Power** | ✗ | ✓ | ✗ | ✗* |
| **Tray** | ✓ | ✓ | ✓ | ✓ |
| **Utils** | ✓** | ✗ | ✗ | ✓ |

\* Power sau này có thể cần ghi log file — dùng `System.IO` trực tiếp, không reference Tray.  
\** Chỉ `HardwareDebugger` reference Hardware/LHM.

### Cấm tuyệt đối

- `Hardware` → `Tray` / WinForms
- Tạo thêm `Computer` ngoài `HardwareMonitorHost`
- Duplicate WMI/LHM parsing khi đã có `SensorReader` / `WmiHelper`
- Logic power plan trong `TrayApp` (phải qua `PowerMode`)
- File mới ở root `App/` (luôn vào subfolder)

---

## 8. Pattern bắt buộc

### Thêm monitor phần cứng mới

```csharp
// App/Hardware/XxxMonitor.cs
public sealed class XxxMonitor : PollingMonitorBase
{
    public XxxMonitor(int intervalMs = 2000) : base(intervalMs) { }

    public float SomeMetric { get; private set; }

    protected override void Refresh()
    {
        // Đọc qua HardwareMonitorHost.Computer + SensorReader
        // hoặc WmiHelper — không new Computer()
    }
}
```

Wire trong `TrayApp`: construct → `Updated` → `HardwareSnapshot` → UI.

### Cập nhật UI từ background

- Dùng `SynchronizationContext.Post` (đã có trong `TrayApp`) — không gọi trực tiếp control từ timer thread.

### Power mode (khi implement thật)

Thứ tự ưu tiên (theo plan gốc):

1. Windows power plan GUID  
2. Registry OEM Lecoo/Bellator (sau `--debug`)  
3. WMI ACPI (nếu có)

Toàn bộ trong `PowerMode.SetMode`; log debug `logs/power_debug.txt` nếu cần.

### Settings

- Đọc: `Settings.Load()` lúc khởi động `TrayApp`  
- Ghi: `settings.Save()` sau thay đổi UI — không ghi file JSON rải rác

---

## 9. Trạng thái triển khai (roadmap thực tế)

| Phase | Nội dung | Trạng thái |
|-------|----------|------------|
| 1 | Hardware monitoring CPU/GPU | ✅ |
| 2 | Tray + SettingsForm dashboard | ✅ (SettingsForm thay PopupForm) |
| 3 | Power modes điều khiển thật | ⏳ Stub |
| 4 | Auto-start + settings persistence | ⚠️ Một phần (class có, UI tùy form) |
| 5 | `--debug` HardwareDebugger | ✅ |
| — | RamMonitor / FanMonitor trên UI | ❌ Chưa wire |

---

## 10. Anti-patterns (tránh code lan man)

| Không làm | Làm thay thế |
|-----------|----------------|
| Copy class monitor từ repo khác nguyên khối | Adapt vào `PollingMonitorBase` + `SensorReader` |
| Thêm `PopupForm` song song `SettingsForm` | Mở rộng `SettingsForm` |
| Hardcode chuỗi mode trong menu | `PerformanceProfile.All` |
| Nhiều timer đọc LHM riêng | Một host + nhiều monitor share ref-count |
| Logic sensor trong `TrayApp` | Property trên monitor |
| File `Helper2`, `UtilsNew` | Mở rộng helper hiện có hoặc đúng layer |

---

## 11. Checklist thêm tính năng

Trước khi tạo file mới, trả lời:

1. Thuộc layer nào? (`Hardware` / `Power` / `Tray` / `Utils`)
2. Đã có class tương tự chưa? (grep tên metric)
3. Có cần `PollingMonitorBase` không?
4. UI có cần snapshot mới không? → sửa `HardwareSnapshot`
5. Có cần setting mới không? → property trong `Settings.cs`
6. Có vi phạm ma trận phụ thuộc không?

---

## 12. Lệnh vận hành

| Lệnh | Mục đích |
|------|----------|
| `dotnet build` | Build (user thực hiện) |
| `dotnet run -- --debug` | Dump hardware → `bin/.../logs/hardware_dump.txt` |
| `dotnet publish -c Release -r win-x64 --self-contained false` | Publish folder |

---

## 13. Đồng bộ tài liệu

Khi thay đổi **cấu trúc** (folder mới, class chính, wire monitor, implement PowerMode):

1. Cập nhật `docs/structure.json`
2. Cập nhật bảng trạng thái trong file này và `AGENTS.md` nếu cần
3. Cập nhật `README.md` layout nếu user-facing path đổi

---

## 14. Liên hệ plan cũ

`Plan for BHelper.md` mô tả `PopupForm.cs` — **code hiện tại dùng `SettingsForm.cs`**. Agent mới **không** tạo lại PopupForm trừ khi user yêu cầu đổi tên/flow.
