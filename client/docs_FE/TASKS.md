# TASKS — Danh Sách Công Việc Frontend (MultiGuide PWA)

> **Quy ước:** `[ ]` chưa làm · `[x]` xong (đã kiểm tra hiển thị mobile, test logic & commit).
> **Nguyên tắc:** Mỗi lượt thực hiện **duy nhất 1 task**. Sau khi xong 1 task, kiểm tra kỹ lưỡng, tick `[x]` và cập nhật `PROGRESS.md`.

---

## Giai đoạn 1: Thiết lập Nền tảng & Design System Di Sản
- [x] **FE-1.1**: Thiết lập cấu trúc thư mục modular (`assets/`, `css/`, `js/`) và nhúng bộ Typography di sản (*Playfair Display / Lora* + *Plus Jakarta Sans*).
- [x] **FE-1.2**: Xây dựng hệ thống CSS Tokens (`variables.css` & `base.css`): Bảng màu giấy dó `#FBF9F5`, mực nho `#181816`, đồng cổ `#C28E58`, hiệu ứng Frosted Glass, thiết lập `100dvh` và Safe Area Insets cho iPhone.
- [x] **FE-1.3**: Chuẩn bị tài nguyên đồ họa: Ảnh hero Khuê Văn Các xử lý gradient mask sương mờ và bộ SVG Icons tối giản (Compass, Audio waves, Pin markers, 3 Tabs).

---

## Giai đoạn 2: Màn hình 1 — The Arrival & The Digital Pass
> ⚠️ **BẮT BUỘC:** Agent phải gọi `view_file` xem trực tiếp ảnh `client/assets/Giao_dien_ngoai.png` để đối chiếu từng pixel trước khi code.
- [x] **FE-2.1 [Đối chiếu Giao_dien_ngoai.png]**: Dựng bố cục Hero Màn hình 1: Header logo `MultiGuide` + `HANOI EDITION`, ảnh di tích Khuê Văn Các với gradient mask hòa vào nền giấy dó `#FBF9F5`, khẩu hiệu *"Every step, a story."* bằng Serif nghệ thuật.
- [x] **FE-2.2 [Đối chiếu Giao_dien_ngoai.png]**: Xây dựng bộ chuyển đổi ngôn ngữ (Language Switcher Pill): Nổi kính mờ giữa ảnh và nội dung với 3 cờ 🇻🇳 Tiếng Việt, 🇬🇧 English, 🇯🇵 日本語; cập nhật trạng thái tập trung trong `state.js`.
- [x] **FE-2.3 [Đối chiếu Giao_dien_ngoai.png]**: Xây dựng thẻ vé di sản số (Digital Heritage Pass Card): Thẻ bo góc sang trọng, họa tiết hoa văn watermark, 2 vết khuyết xé vé tròn (ticket notches), thông tin giá `$5.00 USD` (50.000 đ), 3 tích xanh (GPS Geofence Audio, 6 POIs, Offline Ready), đếm ngược 24h, mã `HN — 001`.
- [x] **FE-2.4 [Đối chiếu Giao_dien_ngoai.png]**: Tích hợp nút 1-chạm đen tuyền: `"Kích hoạt vé tham quan (1-Tap Buy)"`, footer cam kết bảo mật `🔒 Secure checkout · No app download needed`, gọi `POST /areas/van-mieu/passes` và lưu token vào `localStorage`.

---

## Giai đoạn 3: Màn hình 2 — Bản đồ Thật & 6 POI Thần Đạo
> ⚠️ **BẮT BUỘC:** Agent phải gọi `view_file` xem trực tiếp ảnh `client/assets/Giao_dien_Chinh.png` để đối chiếu layout bản đồ và các thành phần nổi.
- [x] **FE-3.1 [Đối chiếu Giao_dien_Chinh.png]**: Khởi tạo Leaflet Map với tile Zen map style tối giản, khóa khung cuộn (maxBounds) quanh khuôn viên Văn Miếu `[21.0245, 105.8325]` đến `[21.0325, 105.8385]`.
- [x] **FE-3.2 [Đối chiếu Giao_dien_Chinh.png]**: Vẽ đường trục Thần đạo nét đứt màu đồng nhạt kết nối từ Cổng Văn Miếu (Nam) lên Nhà Thái Học (Bắc) và 2 nhánh quanh Giếng Thiên Quang.
- [x] **FE-3.3 [Đối chiếu Giao_dien_Chinh.png]**: Định vị và gắn 6 Custom Pin Markers chuẩn vị trí thực tế của Backend (`DbSeeder.cs`), hình vòm bia đá giọt nước có số thứ tự từ 1 đến 6 kèm huy hiệu sóng âm thanh tròn nhỏ, nhãn địa danh đối xứng hai bên, và trạng thái Active `NOW PLAYING` màu vàng đồng hoàng gia.
- [x] **FE-3.4 [Đối chiếu Giao_dien_Chinh.png]**: Thêm các thành phần định hướng tĩnh: La bàn góc hướng Bắc (Compass N), thước tỉ lệ bản đồ (Scale bar "20 m"), thẻ Header nổi Frosted Glass hiển thị `Temple of Literature` + nút tùy chọn filters, pill `● GPS audio on`, và nút tròn recenter GPS góc phải.

---

## Giai đoạn 4: Định vị GPS, Radar Pulse & Geofencing
- [x] **FE-4.1 [Đối chiếu Giao_dien_Chinh.png]**: Thiết kế Marker vị trí du khách: Chấm tròn đen viền trắng phát xung sóng radar nhịp thở đôi (`double breathing pulse animation`) đúng như trên bản đồ mẫu.
- [x] **FE-4.2**: Xây dựng bộ xử lý GPS kép: Hỗ trợ chuyển đổi giữa chế độ GPS thật (`navigator.geolocation.watchPosition` có bộ lọc làm mịn Haversine) và chế độ giả lập Click-to-Move trên bản đồ phục vụ demo.
- [x] **FE-4.3 [Đối chiếu Giao_dien_Chinh.png]**: Tích hợp thuật toán Geofencing tự động: Mỗi khi vị trí thay đổi, gọi `GET /pois/nearby`, kiểm tra cờ `withinTrigger` ($\le 30m$) và làm nổi bật POI với nhãn `"NOW PLAYING"` màu đồng trầm.

---

## Giai đoạn 5: Trình phát Audio Thuyết minh Đẳng cấp (Luxury Player)
> ⚠️ **BẮT BUỘC:** Agent phải gọi `view_file` xem trực tiếp ảnh `client/assets/Giao_dien_Chinh.png` để đối chiếu thanh Mini Player ghim đáy.
- [x] **FE-5.1 [Đối chiếu Giao_dien_Chinh.png]**: Dựng Floating Mini Player ghim đáy & Interactive Bottom Sheet 3 nấc: Tiêu đề POI (Serif), số thứ tự (ví dụ: `03 of 06 · The poetry of the stars`), nhãn `• STORIES, IN STEP WITH YOU`, badge ngôn ngữ `EN`, nút tròn Play/Pause màu đồng cổ, thanh tiến trình màu đồng (Scrubber bar), kéo vuốt mở rộng 78vh đọc trọn vẹn câu chuyện lịch sử di tích.
- [x] **FE-5.2**: Tích hợp Audio Engine: Gọi `GET /pois/{id}?lang={lang}` lấy Signed URL từ Supabase Storage qua Backend thật, streaming âm thanh mượt mà, xử lý mở khóa AudioContext và fallback chuông di sản thiền định 432Hz.
- [x] **FE-5.3**: Tích hợp Media Session API (`navigator.mediaSession`): Hiển thị hình ảnh Khuê Văn Các và thanh điều khiển ngay trên màn hình khóa điện thoại khi tắt màn hình.
- [x] **FE-5.4**: Tự động gửi Play-Log: Bắt sự kiện bài nghe kết thúc hoặc dừng để gửi `POST /play-logs` ghi nhận thời gian nghe thực tế của du khách.

---

## Giai đoạn 6: Bottom Navigation & 3 Chế độ Xem (Tabs)
> ⚠️ **BẮT BUỘC:** Agent phải gọi `view_file` xem trực tiếp ảnh `client/assets/Giao_dien_Chinh.png` để đối chiếu 3 tab đáy.
- [x] **FE-6.1 [Đối chiếu Giao_dien_Chinh.png]**: Dựng thanh điều hướng đáy (Bottom Navigation Bar) 3 tab chuẩn phong cách Apple: `Explore` (Bản đồ), `All stops` (Danh sách điểm), `My pass` (Vé của tôi).
- [x] **FE-6.2**: Xây dựng màn hình/drawer `All stops`: Danh sách 6 điểm di tích kéo trượt êm ái, hiển thị thứ tự tham quan, thời lượng nghe và trạng thái (Đang ở gần / Đã nghe).
- [x] **FE-6.3**: Xây dựng màn hình `My pass`: Hiển thị chi tiết thẻ vé số, đồng hồ đếm ngược 24h, mã vé rút gọn và tùy chọn làm mới/gia hạn vé.

---

## Giai đoạn 7: PWA, Tối ưu & Triển khai
- [x] **FE-7.1**: Tạo file `manifest.json` và Service Worker (`sw.js`) để hỗ trợ cài đặt Add-to-Home-Screen và lưu cache tĩnh offline.
- [x] **FE-7.2**: Tích hợp Wake-up Ping gửi `GET /health` khi mở trang để đánh thức Render server.
- [x] **FE-7.3**: Kiểm thử E2E trên các thiết bị di động thật (iOS Safari & Android Chrome) và deploy lên Vercel.
