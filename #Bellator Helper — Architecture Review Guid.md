# BHelper — Architecture Review Guide

Tài liệu này dùng để agent AI review code trong project, kiểm tra xem cấu trúc và cách viết có đúng chuẩn không.

---

## Cấu trúc thư mục (repo thực tế — tham chiếu G-Helper về nguyên tắc, không copy tên file)

```
App/
├── Hardware/     → PollingMonitorBase, *Monitor, HardwareMonitorHost, SensorReader
├── Power/        → PowerMode, PerformanceProfile (thay folder Mode/ của G-Helper)
├── Tray/         → TrayApp, SettingsForm, HardwareSnapshot, TrayTheme, TrayIconHelper
└── Utils/        → Settings (JSON), AdminHelper, AutoStart, HardwareDebugger
```

Chi tiết đầy đủ: `docs/PROJECT_SPEC.md`, `AGENTS.md`.

---

## Quy tắc bắt buộc (agent phải check từng file)

### 1. SettingsForm — Form chỉ được hiển thị

**Đúng:**
- Nhận data qua method `ApplySnapshot(HardwareSnapshot, string)` hoặc tương đương
- Gọi helper tĩnh để format (VD: `FormatTemp()`)
- Subscribe event từ service bên ngoài

**Sai — phải báo lỗi nếu thấy:**
- Gọi trực tiếp hardware API (WMI, LibreHardwareMonitor) trong Form
- Chứa `Timer` để tự poll dữ liệu
- Chứa business logic (tính toán mode, lưu config)
- Gọi `File.Read` / `File.Write` trực tiếp

---

### 2. HardwareSnapshot — chỉ là data model

**Đúng:**
```csharp
public record HardwareSnapshot(float CpuTemp, float GpuTemp, int CpuFan, int GpuFan);
```

**Sai:**
- Có method gọi sensor bên trong
- Có dependency với UI (import namespace UI/Tray)
- Có `Timer`, `Task`, hoặc `async` bên trong

---

### 3. TrayTheme — chỉ là màu sắc và font

**Đúng:**
```csharp
public static class TrayTheme {
    public static readonly Color Background = Color.FromArgb(28, 30, 38);
    public static Color TempAccent(float celsius) => celsius > 80f ? Hot : ...;
}
```

**Sai:**
- Import namespace ngoài `System.Drawing`
- Chứa logic ngoài việc trả về Color/Font

---

### 4. Settings (Utils) — đọc/ghi config, không có UI

**Đúng:**
- `App/Utils/Settings.cs` — JSON tại `%AppData%\BHelper\settings.json`
- Trả về plain object (không phải Control hay Form)

**Sai:**
- Import `System.Windows.Forms`
- Mở dialog, MessageBox
- Chứa Timer hoặc background thread

---

### 5. Kéo form (drag) — bắt buộc vì dùng FormBorderStyle.None

**Phải có** trong SettingsForm (hoặc base class):
```csharp
private bool _dragging;
private Point _dragStart;
// MouseDown → _dragging = true, ghi _dragStart
// MouseMove → nếu _dragging, dịch chuyển Location
// MouseUp   → _dragging = false
```

Nếu thiếu → người dùng không kéo được form.

---

### 6. Region bo góc — bắt buộc nếu FormBorderStyle.None

**Phải có:**
```csharp
Region = RoundedRegion(Width, Height, radius);
```

Và `RoundedRegion()` phải dùng `GraphicsPath` + `AddArc`.

**Lưu ý:** Giá trị truyền vào `RoundedRegion` phải khớp với `ClientSize`. Nếu khác → góc bị cắt sai.

---

### 7. Controls.Add — thứ tự quan trọng với Dock

**Đúng:**
```csharp
Controls.Add(fillPanel);  // Fill trước
Controls.Add(header);     // Top sau
```

**Sai:**
```csharp
Controls.Add(header);     // Nếu add Top trước...
Controls.Add(fillPanel);  // ...Fill sẽ đè lên header
```

---

### 8. Kích thước hardcode — phải dùng hằng số

**Đúng:**
```csharp
private const int FormW   = 420;
private const int FormH   = 340;
private const int FormPad = 12;
private const int InnerW  = FormW - FormPad * 2;
```

**Sai:**
- Width `316`, `360`, `340` xuất hiện lặp lại nhiều lần mà không có hằng số
- `RoundedRegion(340, 270, 10)` dùng số khác với `ClientSize = new Size(420, 340)`

---

## Checklist review nhanh

Khi đọc từng file `.cs`, agent trả lời các câu sau:

| Câu hỏi | Kết quả |
|---|---|
| File này thuộc layer nào? (UI / Service / Model / Config) | |
| Có import namespace sai layer không? | |
| Form có tự poll hardware không? | |
| Snapshot có chứa logic không? | |
| Có hằng số cho kích thước form chưa? | |
| Thứ tự Controls.Add có đúng không? | |
| Có drag support không (nếu là Form)? | |
| Region có khớp ClientSize không? | |

---

## Tham khảo: G-Helper (seerge/g-helper)

Project G-Helper trên GitHub tổ chức theo cùng nguyên tắc này — mỗi tính năng là 1 folder (`Battery/`, `Fan/`, `Mode/`, `UI/`), file `AppConfig.cs` độc lập, Form chỉ nằm trong `UI/`.

Source: https://github.com/seerge/g-helper/tree/main/app