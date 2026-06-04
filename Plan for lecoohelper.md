Đây là plan chi tiết để Agent (Claude Code) có thể chạy và build repo từ đầu:

---

# AGENT PLAN: Lecoo Fighter Helper

## Thông tin máy (từ ảnh)
```
CPU: Intel i7-13650HX | 14 cores | ~3.5GHz
GPU: NVIDIA RTX 5060 Laptop | 8GB VRAM
RAM: 16GB DDR5 4800 MT/s
SSD: 1000GB (606GB free)
Fan: 大风扇 (big) + 小风扇 (small)
OS:  Windows 11
```

---

## PHASE 0 — Setup môi trường (Agent tự làm)

```
TASK: Khởi tạo project C# WinForms .NET 8

COMMANDS:
dotnet new winforms -n LecooHelper --framework net8.0-windows
cd LecooHelper
dotnet add package LibreHardwareMonitorLib
dotnet add package Microsoft.Win32.Registry

CẤU TRÚC THƯ MỤC TẠO RA:
LecooHelper/
├── LecooHelper.csproj
├── Program.cs
├── App/
│   ├── Hardware/
│   │   ├── CpuMonitor.cs
│   │   ├── GpuMonitor.cs
│   │   ├── RamMonitor.cs
│   │   └── FanMonitor.cs
│   ├── Power/
│   │   ├── PowerMode.cs
│   │   └── PerformanceProfile.cs
│   ├── Tray/
│   │   ├── TrayApp.cs
│   │   └── PopupForm.cs
│   └── Utils/
│       ├── AutoStart.cs
│       └── Settings.cs
├── Resources/
│   └── tray_icon.ico
└── README.md
```

---

## PHASE 1 — Hardware Monitoring (không cần driver Lecoo)

```
FILE: App/Hardware/CpuMonitor.cs

MỤC TIÊU:
- Đọc CPU usage % (PerformanceCounter)
- Đọc CPU temp (LibreHardwareMonitor)
- Đọc CPU frequency (WMI)

AGENT INSTRUCTION:
"Tạo class CpuMonitor.cs dùng LibreHardwareMonitor
để đọc: Usage%, Temperature, Frequency
Cập nhật mỗi 2 giây bằng System.Timers.Timer
Expose qua public properties: Usage, Temp, FreqGhz"
```

```
FILE: App/Hardware/GpuMonitor.cs

MỤC TIÊU:
- GPU usage %
- GPU temp
- VRAM used/total
- GPU frequency

AGENT INSTRUCTION:
"Tạo class GpuMonitor.cs dùng LibreHardwareMonitor
Filter hardware type = GpuNvidia
Đọc: Load, Temperature, MemoryUsed, Clock"
```

```
FILE: App/Hardware/FanMonitor.cs

MỤC TIÊU:
- Đọc fan RPM (nếu driver expose qua LibreHardwareMonitor)
- Fallback: đọc qua WMI Win32_Fan

AGENT INSTRUCTION:
"Tạo FanMonitor.cs thử đọc fan speed bằng
LibreHardwareMonitor trước, nếu không có
thì fallback sang WMI query:
SELECT * FROM Win32_Fan"
```

---

## PHASE 2 — System Tray UI

```
FILE: App/Tray/TrayApp.cs

MỤC TIÊU:
- Icon trên taskbar (system tray)
- Right-click menu hiện: CPU temp, GPU temp
- Left-click mở popup window chi tiết
- Double-click = exit

AGENT INSTRUCTION:
"Tạo TrayApp.cs kế thừa ApplicationContext
Dùng NotifyIcon với ContextMenuStrip
Menu items:
  [CPU: 55°C | 0%]     ← dynamic, update mỗi 2s
  [GPU: 42°C | 2%]     ← dynamic
  [RAM: 89% | 14.2GB]  ← dynamic
  ─────────────────
  [⚡ Performance Mode ▶]  ← submenu
  [🚀 Mở Dashboard]
  ─────────────────
  [❌ Thoát]
Icon tray đổi màu theo nhiệt độ:
  <70°C = xanh, 70-85°C = vàng, >85°C = đỏ"
```

```
FILE: App/Tray/PopupForm.cs

MỤC TIÊU:
- Popup nhỏ khi click icon
- Hiện đầy đủ thông số như app Bellator
- KHÔNG dùng taskbar entry (ShowInTaskbar = false)
- Dark theme

AGENT INSTRUCTION:
"Tạo PopupForm.cs:
- Size: 400x500px
- FormBorderStyle = None (không có title bar)
- Tự động đóng khi click ra ngoài (Deactivate event)
- Vẽ gauge bằng GDI+ cho CPU/GPU usage
- Dark background: #0A0E1A
- Text color: #00D4FF (cyan)"
```

---

## PHASE 3 — Performance Modes (QUAN TRỌNG - cần test trên máy thật)

```
FILE: App/Power/PowerMode.cs

STRATEGY: Thử từng cách theo thứ tự ưu tiên

AGENT INSTRUCTION:
"Tạo PowerMode.cs với 4 mode:
  Performance, Balanced, Silent, Turbo

Thử theo thứ tự:
1. Windows Power Plan (luôn hoạt động):
   - Performance  = GUID: 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c
   - Balanced     = GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  
   - PowerSaver   = GUID: a1841308-3541-4fab-bc81-f71556f20b4a

2. Registry check (Lecoo-specific, có thể không có):
   HKLM\SYSTEM\CurrentControlSet\OEM\Lecoo\PowerMode
   hoặc
   HKLM\SOFTWARE\Bellator\PerformanceMode

3. WMI ACPI (nếu driver expose):
   SELECT * FROM WmiMonitorBrightnessMethods
   (placeholder, cần test trên máy thật)

Log tất cả attempt ra file: logs/power_debug.txt
để debug sau"
```

---

## PHASE 4 — Auto Start & Settings

```
FILE: App/Utils/AutoStart.cs

AGENT INSTRUCTION:
"Tạo AutoStart.cs:
- Enable: thêm registry key
  HKCU\Software\Microsoft\Windows\CurrentVersion\Run
  'LecooHelper' = path to exe
- Disable: xóa key đó
- Check: IsEnabled() return bool"
```

```
FILE: App/Utils/Settings.cs

AGENT INSTRUCTION:
"Tạo Settings.cs dùng System.Text.Json
Lưu file: %APPDATA%\LecooHelper\settings.json
Properties:
  - UpdateIntervalMs: int = 2000
  - StartMinimized: bool = true
  - AutoStart: bool = false  
  - DefaultPowerMode: string = 'Balanced'
  - ShowInTray: bool = true"
```

---

## PHASE 5 — Debug & Reverse Engineering helper

```
FILE: App/Utils/HardwareDebugger.cs

MỤC TIÊU: Tool để tự debug driver Lecoo

AGENT INSTRUCTION:
"Tạo HardwareDebugger.cs với method DumpAll():
1. List tất cả LibreHardwareMonitor sensors ra file
2. Dump tất cả WMI namespaces liên quan
3. List tất cả Registry keys dưới:
   HKLM\SYSTEM\CurrentControlSet\Services
   filter tên có: lecoo, bellator, fighter, n176
4. Lưu ra: logs/hardware_dump.txt
Chạy 1 lần khi debug mode"
```

---

## FILE CUỐI: Program.cs

```csharp
// AGENT: Tạo Program.cs như sau:

[STAThread]
static void Main()
{
    // Chỉ cho chạy 1 instance
    using var mutex = new Mutex(true, "LecooHelper", out bool isNew);
    if (!isNew) return;

    Application.EnableVisualStyles();
    Application.SetCompatibleTextRenderingDefault(false);
    
    // Chạy debug dump nếu có flag --debug
    if (args.Contains("--debug"))
        HardwareDebugger.DumpAll();
    
    Application.Run(new TrayApp());
}
```

---

## Thứ tự Agent nên chạy

```
1. dotnet new winforms         ← tạo project
2. dotnet add package ...      ← cài dependencies  
3. Tạo CpuMonitor.cs          ← test đọc được temp chưa
4. Tạo TrayApp.cs             ← test icon tray
5. Tạo PopupForm.cs           ← test UI
6. Chạy --debug mode          ← dump hardware info
7. Tạo PowerMode.cs           ← dựa trên dump kết quả
8. dotnet publish -r win-x64  ← build exe cuối
```

---

## Câu lệnh prompt cho Agent

Copy paste cái này vào Claude Code:

```
Tạo project C# WinForms tên "LecooHelper" cho laptop 
Lecoo Fighter N176B với specs:
- CPU: i7-13650HX
- GPU: RTX 5060 Laptop  
- RAM: 16GB
- OS: Windows 11

Mục tiêu: System tray app nhẹ (<20MB RAM) thay thế 
app Bellator đang tốn 300MB.

Bắt đầu từ Phase 0, tạo đủ cấu trúc thư mục,
sau đó implement Phase 1 (Hardware Monitoring)
trước, test được thì mới qua Phase 2.

Tham khảo cách tổ chức code từ:
https://github.com/seerge/g-helper
nhưng KHÔNG copy code ASUS-specific.
```

---

> **Lưu ý quan trọng:** Phase 3 (Performance Modes) cần bạn chạy `--debug` trên máy thật trước để biết Lecoo dùng driver gì, sau đó Agent mới có thể viết đúng code control.