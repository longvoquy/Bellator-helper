# BHelper — Roadmap cải tiến

> Đánh dấu `[x]` khi xong, `[ ]` khi chưa làm. Cập nhật file này khi hoàn thành hoặc đổi kế hoạch.
> Quy tắc layer và checklist thêm tính năng: xem [PROJECT_SPEC.md](PROJECT_SPEC.md).
> "Đã code" nghĩa là đã sửa code và build sạch (0 warning, 0 error). Chưa chạy thử trên máy thật.

## Nên làm trước (nhanh, ít rủi ro)

- [x] **1. Sửa tài liệu cho khớp code**
  - [x] `PROJECT_SPEC.md`: `PowerMode` đã gọi WMI Bellator, không còn là stub
  - [x] `PROJECT_SPEC.md` + `AGENTS.md`: `RamMonitor` / `FanMonitor` đã wire vào Tray
  - [x] `docs/structure.json` cập nhật theo trạng thái thật
- [x] **2. Hiển thị RAM** (đã code)
  - [x] Wire `RamMonitor` vào `TrayApp`
  - [x] Thêm `RamUsagePercent` vào `HardwareSnapshot`
  - [x] Hiển thị RAM trong `SettingsForm` (dòng RAM dưới GPU)
- [x] **3. Đổi mode theo nguồn điện** (đã code)
  - [x] Logic `IsOnAcPower()` trong `PowerMode`, lắng nghe `SystemEvents.PowerModeChanged` trong `TrayApp`
  - [x] Ghi nhớ mode riêng cho AC và pin (`AcPowerMode`, `BatteryPowerMode` trong `Settings.cs`), cập nhật khi người dùng chọn mode
  - [x] Không còn toggle bật/tắt: tính năng luôn bật
  - [x] Đổi mode nền không ghi đè `DefaultPowerMode`
- [x] **4. Phím tắt đổi mode** (đã code)
  - [x] Hotkey toàn cục qua `GlobalHotkeyWindow`, mặc định Ctrl+Alt+P, vòng qua các mode
  - [x] Cấu hình qua `HotkeyModifiers` / `HotkeyKey` trong `settings.json` (chưa có UI chọn phím)
- [x] **5. Hoàn tất kiểm tra cập nhật** (đã code)
  - [x] Menu tray "Check for updates" dùng `UpdateChecker`
  - [x] Có bản mới thì hỏi mở trang release, đã là bản mới nhất thì báo

## Nên làm sau (cần thêm thông tin từ máy)

- [~] **6. Sparkline lịch sử nhiệt độ** (đã làm rồi gỡ bỏ)
  - [x] Đã thử: ring buffer + panel sparkline. Bỏ vì trùng thông tin với số hiện trên dashboard, hai đường gần như chồng nhau
- [~] **7. Thông tin pin** (đã code một phần)
  - [x] `BatteryMonitor` (WMI `Win32_Battery`, `root\WMI`): phần trăm, nguồn AC/pin, sức khỏe, số chu kỳ
  - [x] Hiển thị trong menu tray
  - [ ] Hiển thị trong `SettingsForm` (chưa làm, dùng menu tray trước)
  - [ ] Chưa kiểm chứng trên máy: tên class/field WMI `root\WMI` cần xác nhận bằng `--debug` hoặc chạy thử
- [ ] **8. Giới hạn sạc 80%** — cần kiểm tra WMI/EC của máy trước (`--debug`)
- [x] **9. Ghi log power ra file** (đã code)
  - [x] `PowerDebugLog` ghi `logs/power_debug.txt` trong `%APPDATA%\BHelper`
  - [x] Mặc định tắt, bật bằng `EnablePowerDebugLog` trong `settings.json`

## Rủi ro cao (cần reverse engineering hoặc dump phần cứng)

- [ ] **10. Điều khiển quạt** (curve, max fan) — phụ thuộc EC Lecoo, chưa reverse
- [ ] **11. Chuyển GPU mode** (iGPU / dGPU / Optimus) — cần kiểm tra WMI/registry có hỗ trợ không
