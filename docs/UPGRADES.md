# BHelper — Checklist nâng cấp tính năng hiện có

> Đánh dấu `[x]` khi xong. "Đã code" nghĩa là đã sửa code và build sạch (0 warning, 0 error). Chưa chạy thử trên máy thật.
> Roadmap tổng thể: xem [ROADMAP.md](ROADMAP.md).

## A. Hiển thị RAM kèm GB (đã code)

- [x] `HardwareSnapshot` nhận thêm `RamUsedGb`, `RamTotalGb`
- [x] `SettingsForm` hiển thị dạng `58%  (9.3 / 16 GB)`, khi chưa có dữ liệu thì `--`

## B. Đổi mode theo nguồn điện (đã code)

- [x] Khi mở app, áp dụng mode theo nguồn điện hiện tại, không hiện balloon
- [x] Debounce 3 giây cho `SystemEvents.PowerModeChanged` để tránh đổi mode nhiều lần khi cắm sạc
- [x] Balloon thông báo khi đổi mode tự động hoặc bằng phím tắt
- [x] Hotkey đổi mode: thông báo mode mới sau khi đổi

## C. Cấu hình ngay trong dashboard (đã code)

- [x] Đã gỡ checkbox "Switch mode by power source" khỏi dashboard và menu tray. Tính năng đổi mode theo nguồn điện luôn bật
- [x] Ghi nhớ mode theo nguồn: mode người dùng chọn khi cắm sạc được dùng lại lần sau khi cắm sạc, tương tự với pin. Mặc định Balanced (AC) và Silent (pin) khi chưa chọn. Không có UI riêng cho phần này
- [x] Nút phím tắt: bấm rồi nhấn tổ hợp phím để đổi (cần ít nhất một modifier Ctrl/Alt/Shift, Esc để hủy)
- [x] Phím tắt không đăng ký được thì khôi phục phím cũ và báo lỗi
- [x] Đổi cấu hình trong dashboard được lưu vào `settings.json` ngay

## C2. Phím Bellator (Fn+F10) qua WMI (đã code, đã test trên máy)

- [x] `HidEventListener` nghe `HID_EVENT20` (`root\WMI`): `01-23-xx` = Fn+F10 → đổi mode theo vòng
- [x] Event `01-0F-xx` (SystemPerMode) cập nhật menu/dashboard ngay, bỏ trùng 100 ms vì firmware gửi hai lần
- [x] Ctrl+Alt+P giữ làm phím dự phòng, đổi được trong dashboard
- [x] `EnableHidEventLog` chỉ bật log thô `hid_events.txt`, không ảnh hưởng Fn+F10
- Ghi chú: Fn đơn lẻ gửi VK `0xFF`, đừng đăng ký `RegisterHotKey` với mã này

## D. Pin và cập nhật (đã code, build sạch)

- [x] Hàng "Battery" trong dashboard: `99%  Plugged in  Health 98%  2 cycles`, ẩn khi máy không có pin. Ba trạng thái: Charging, Plugged in (cắm sạc, pin đầy nên không nạp), On battery. Rê chuột vào hàng này hiện tooltip giải thích
- [x] Health và số chu kỳ đọc một lần qua IOCTL của driver pin (`BatteryInfoReader`), không dùng `root\WMI` vì `BatteryStaticData` báo "Generic failure" trên máy này. Đã kiểm tra: 57990 / 57000 mWh, 2 chu kỳ, 98%, khớp `powercfg /batteryreport`
- [x] Kiểm tra cập nhật tự động tối đa mỗi 24 giờ, chờ 30 giây sau khi khởi động, báo bằng balloon, bấm vào balloon mở trang tải. Lần kiểm tra thất bại thì thử lại ở lần mở app sau
- [x] URL `UpdateChecker` khớp `git remote` (`longvoquy/Lecco-helper`)

## D2. Chỉnh giao diện (đã code, build sạch)

- [x] Dòng Battery và RAM có dữ liệu ngay khi mở app: `PollingMonitorBase.UsesHardwareHost = false` cho monitor chỉ dùng WMI, nên không chờ LibreHardwareMonitor mở; chạy ở tác vụ riêng
- [x] Hàng Battery xuất hiện muộn thì dashboard giữ mép dưới, không trượt ra ngoài màn hình
- [x] Menu chuột phải tray dùng giao diện tối (`TrayMenuRenderer`): nền, viền, hover, dấu tick trắng, dòng thông tin xám sáng
- [x] Bấm icon tray khi dashboard đang mở thì đóng lại (bỏ sự kiện double-click để không mở rồi đóng nhiều lần)
- [x] Nút đóng dashboard nhuộm trắng (`ResourceImageHelper.LoadTinted`)
- [x] Tooltip hàng Battery vẽ tối, rộng tối đa khoảng 240px
- [x] Menu tray là tool window (`TrayContextMenu`) để không có nút Bellator trên taskbar khi mở menu. **Chưa kiểm chứng**: cần đo lại cửa sổ sau khi build

## E1. Điều khiển quạt: phần nền (đã code, build sạch, **chưa test trên máy**)

- [x] `FanControl` (Power) + `BellatorWmiClient`: method WMI 20 `MaxFanSpeedSwitch` `[loại quạt, bật/tắt]` và 21 `MaxFanSpeed` `[loại quạt, giá trị]`. Giá trị là trần RPM tính theo trăm vòng/phút (24 = 2400 RPM), 0 = quạt CPU/GPU, 1 = quạt hệ thống
- [x] Khoảng giá trị theo mode lấy từ bảng của hãng (Intel/AMD): Silent = Work, Balanced = Playing, Beast = Fastest, Battle = Fullspeed. Giá trị luôn bị kẹp trong khoảng này
- [x] Mặc định tắt (firmware tự lo): app không gửi lệnh quạt nào cho đến khi người dùng bật "Limit max fan speed"
- [x] Cấu hình lưu theo mode trong `settings.json` (`FanLimitEnabled`, `FanCpuGpuLimits`, `FanSysLimits`), áp dụng lại khi mở app và khi đổi mode (giống hãng)
- [x] Giá trị trần mặc định là đầu trên của khoảng (ít giới hạn nhất, giống hiển thị của app hãng). Khoảng cho phép hiện dưới hai đầu thanh trượt, dòng "Running: N RPM" là tốc độ quạt thật, ghi chú giải thích trần chỉ có tác dụng khi quạt muốn chạy nhanh hơn
- [x] UI: hàng "Fan" trong dashboard (`Auto >` / `Limited >`), bấm mở cửa sổ riêng `FanForm` có 2 thanh trượt tối (`DarkSlider`), số RPM trực tiếp, nút Apply và Reset to auto
- [ ] Cần test trên máy: lệnh có đổi RPM thật không, quạt có phản hồi khi cap thấp, firmware có tự reset cap khi đổi mode không
- [ ] Chưa có: đọc lại trạng thái cap từ firmware (hãng không có lệnh Get cho hai method này)

## E. Có thể làm tiếp (source hãng đã có sẵn method)
- [ ] GPU mode (hybrid / discrete): `GPUMode`
- [ ] Fn Lock, khóa touchpad: `FnLock`, `TPLock`
