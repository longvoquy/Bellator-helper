# BHelper — Hướng dẫn cho AI / Agent

Đọc file này **trước** khi sửa code. Chi tiết đầy đủ nằm trong [`docs/`](docs/).

**Cursor:** rule tự áp dụng trong [`.cursor/rules/`](.cursor/rules/) (`bhelper.mdc` mọi chat; `bhelper-hardware.mdc` khi mở file `App/Hardware/`).

## Mục tiêu dự án

Ứng dụng **system tray** nhẹ (WinForms, .NET 8) thay Bellator Control Center trên laptop **Bellator / Lecoo Fighter N176B** (và tương tự). Hiển thị: **Bellator Helper** / **B-helper**. Chỉ Windows 10/11.

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
8. **Được phép** chạy `dotnet build` / compile để kiểm tra code (AI có thể build; user vẫn có thể build lại khi cần).
9. **Console/log:** Không dùng emoji/icon trong `Console.WriteLine`.
10. **Phạm vi thay đổi:** Sửa đúng layer; không refactor lan man file không liên quan task.
11. **Build khi app đang chạy:** Trước khi `dotnet build` hoặc chạy thử, kiểm tra `BHelper.exe` có đang chạy không. Nếu có thì thử tắt (`Stop-Process -Name BHelper -Force`) rồi mới build. Bản chạy bằng Administrator thường không tắt được từ shell không elevate (báo "Access is denied"). Khi đó đừng tự nâng quyền, mà báo user đóng app thủ công rồi build lại.

## Skill (`.claude/skills/`)

Có sẵn bộ skill (using-superpowers, brainstorming, writing-plans, systematic-debugging, verification-before-completion, karpathy-guidelines, v.v.). Gọi skill phù hợp qua Skill tool **trước** khi làm, không chờ user nhắc tên.

- Task mới lớn / nhiều bước: `brainstorming` → `writing-plans`.
- Bug, hành vi lạ (đặc biệt lag UI, WMI, sensor): `systematic-debugging` trước khi đề xuất fix.
- Viết / sửa code: áp dụng `karpathy-guidelines` (thay đổi tối thiểu, đúng layer, nêu giả định).
- Trước khi báo "xong": `verification-before-completion`.
- Task không khớp skill nào thì làm bình thường, không ép dùng.

**Ưu tiên khi xung đột:** quy tắc ngắn ở trên thắng skill. Cụ thể:

- Repo **không có project test**. Bước "build để verify" của `verification-before-completion` được thực hiện bằng `dotnet build BHelper.sln` (quy tắc 8). Phần test tự động vẫn không có: đọc lại diff, đối chiếu quy tắc layer, và không báo "pass" khi chưa có bằng chứng thật (build thành công chỉ chứng minh code compile, chưa chứng minh app chạy đúng).
- Không tự thêm project test hay package (quy tắc 6) chỉ vì skill TDD khuyến nghị; hỏi user trước.
- `using-git-worktrees` / `finishing-a-development-branch`: chỉ dùng khi user yêu cầu. Không tự commit, push hay tạo PR.
- Lưu ý khi sửa code: UI thread không gọi WMI, `schtasks` hay `Computer.Open()` trực tiếp (xem lịch sử tối ưu tốc độ mở tray).

## Entry point

- `Program.cs` — single instance (`Mutex`), `--debug` → `HardwareDebugger.DumpAll()`, còn lại → `TrayApp`.
- UAC admin: `app.manifest` (`requireAdministrator`) — cần cho nhiệt độ CPU Intel (MSR).

## Cấu trúc tóm tắt

```
Program.cs
App/
  Hardware/   # Sensor polling (LibreHardwareMonitor, WMI, PerformanceCounter)
  Power/      # Performance profiles
  Tray/       # NotifyIcon, menu, SettingsForm (dashboard)
  Utils/      # Settings JSON, AutoStart, AdminHelper, HardwareDebugger
Resources/
  gcc.ico
```

## Module chưa gắn UI (đừng duplicate)

- `RamMonitor`, `FanMonitor` — đã wire vào `TrayApp` và `SettingsForm`. Khi cần thêm metric: mở rộng `HardwareSnapshot`, không tạo monitor trùng.

## Tham chiếu nhanh trạng thái

| Khu vực | Trạng thái |
|---------|------------|
| CPU/GPU monitor + tray | Hoạt động |
| SettingsForm (dashboard) | Hoạt động |
| PowerMode.SetMode | WMI Bellator |
| RamMonitor / FanMonitor | Đã wire vào Tray |
| AutoStart trong UI | Task Scheduler `BHelper` |

Khi không chắc đặt file ở đâu → mở [docs/PROJECT_SPEC.md](docs/PROJECT_SPEC.md) mục **Ma trận trách nhiệm** và **Checklist thêm tính năng**.
