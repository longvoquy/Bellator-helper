# BHelper — Checklist nâng cấp tính năng hiện có

> Đánh dấu `[x]` khi xong. "Đã code" nghĩa là đã sửa code và build sạch (0 warning, 0 error). Chưa chạy thử trên máy thật.
> Roadmap tổng thể: xem [ROADMAP.md](ROADMAP.md).

## A. Hiển thị RAM kèm GB (đã code)

- [x] `HardwareSnapshot` nhận thêm `RamUsedGb`, `RamTotalGb`
- [x] `SettingsForm` hiển thị dạng `58%  (9.3 / 16 GB)`, khi chưa có dữ liệu thì `--`

## B. Đổi mode theo nguồn điện (đã code)

- [x] Khi mở app, áp dụng mode theo nguồn điện hiện tại (nếu bật auto-switch), không hiện balloon
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

## D. Chưa làm (để sau)

- [ ] Hiển thị pin trong dashboard (hiện chỉ có trong menu tray)
- [ ] Đọc health và số chu kỳ pin một lần khi khởi động thay vì mỗi 30 giây
- [ ] Kiểm tra cập nhật tự động mỗi ngày, báo bằng balloon
- [ ] Giới hạn sạc 80% (cần dump `--debug`)
