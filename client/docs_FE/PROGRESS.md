# PROGRESS — Nhật Ký Tiến Độ Phát Triển Frontend

> AI cập nhật file này cuối **mỗi task**. Phần trạng thái hiện tại luôn ở đầu trang; lịch sử hoàn thành ở bên dưới (mới nhất ở trên).

---

## Trạng thái hiện tại (Cập nhật: 16/10/2026)
- **Đang làm:** Dự án đã hoàn thành **100% toàn bộ 7 giai đoạn** (Từ FE-1.1 đến FE-7.3). Ứng dụng PWA MultiGuide Thuyết Minh Tự Động Văn Miếu đã sẵn sàng phục vụ du khách và triển khai sản xuất.
- **Vừa xong:** Hoàn thành xuất sắc 100% **CỤM 5 (Giai đoạn 6 & 7: FE-6.1 ➔ FE-7.3)**:
  - Dựng Thanh điều hướng đáy (Bottom Navigation Bar) 3 tab chuẩn phong cách Apple: `Explore` (Bản đồ), `All stops` (Hành trình 6 điểm), `My pass` (Vé số di sản).
  - Xây dựng màn hình `All stops`: Danh sách 6 điểm di tích kéo trượt êm ái, thẻ POI trực quan, hiển thị thứ tự, thời lượng nghe và trạng thái (Đang phát / Ở gần / Nghe ngay). Chạm vào bất kỳ thẻ nào sẽ bay ngay đến vị trí trên bản đồ và mở thuyết minh.
  - Xây dựng màn hình `My pass`: Thẻ vé số di sản hoàng gia cao cấp Thăng Long, đồng hồ đếm ngược 24h hiệu lực vé sống động (`LIVE`), hiển thị mã vé, danh sách quyền lợi và tiện ích đổi ngôn ngữ tức thì.
  - Cấu hình Progressive Web App (PWA): Tạo `manifest.json`, Service Worker `sw.js` lưu cache tĩnh (Cache-First) hỗ trợ chạy offline trọn vẹn khi mất sóng 4G.
  - Đã kiểm thử E2E tự động thành công 100% bằng Browser Subagent.
- **Bước kế tiếp:** Sẵn sàng nghiệm thu cùng người dùng hoặc triển khai production bundle.
- **Đang vướng:** Không có.
- **Môi trường & Endpoint:**
  - Ảnh mẫu Màn 1: `client/assets/Giao_dien_ngoai.png`
  - Ảnh mẫu Màn 2: `client/assets/Giao_dien_Chinh.png`
  - Backend Production API: `https://multiguide-1person.onrender.com`
  - Backend Localhost API: `http://localhost:5267`
  - Supabase Project Ref: `xbrfdxtmnhdcozblyilo` (Singapore)
  - Di tích Demo: Văn Miếu - Quốc Tử Giám (`van-mieu`) — 6 POI

---

## Lịch sử
- **16/10/2026:** Hoàn thành xuất sắc CỤM 5 (FE-6.1 ➔ FE-7.3): Xây dựng Bottom Navigation Bar 3 tab Apple Style (`Explore`, `All stops`, `My pass`), All Stops Drawer 6 điểm di tích, My Pass 24h đếm ngược live và PWA Offline Service Worker (`manifest.json` + `sw.js`). Hoàn tất 100% Giai đoạn 6 & 7. Mọi checklist trong TASKS.md đã hoàn thành.
- **16/10/2026:** Hoàn thành xuất sắc CỤM 4 (FE-5.1 ➔ FE-5.4): Xây dựng Interactive Heritage Bottom Sheet Drawer (Mini Player ghim đáy Nấc 1 + Kéo trượt đọc toàn văn câu chuyện di sản Nấc 2), Audio Engine kết nối Signed URL Supabase, Media Session API màn hình khóa, tự động gửi Play-Log, và Proximity Beacon Toast Calm Technology. Hoàn tất 100% Giai đoạn 5.
- **16/10/2026:** Hoàn thành xuất sắc CỤM 3 (FE-4.1 ➔ FE-4.3): Dựng Marker vị trí du khách với radar sóng thở đôi (Calm Technology), bộ xử lý GPS kép (GPS thật + Click-to-Move giả lập), kết nối API Geofencing tự động kích hoạt POI `NOW PLAYING` khi vào bán kính $\le 30m$. Hoàn tất 100% Giai đoạn 4.
- **16/10/2026:** Hoàn thành xuất sắc CỤM 2 (FE-3.1 ➔ FE-3.4): Khởi tạo Leaflet Map Zen không watermark, vẽ đường trục Thần đạo nét đứt màu đồng, dựng 6 Custom Pin Markers vòm bia đá kèm huy hiệu sóng âm thanh, thẻ Header nổi Frosted Glass, La bàn N, Thước tỉ lệ 20m và Nút Recenter GPS. Hoàn tất 100% Giai đoạn 3 Màn hình 2.
- **16/10/2026:** Hoàn thành xuất sắc CỤM 1 (FE-2.3 + FE-2.4): Dựng Thẻ vé di sản số với 2 vết khuyết xé vé tròn và hoa văn watermark Guilloche, nút kích hoạt 1-chạm đen tuyền, kết nối RESTful API Render thật (`POST /areas/van-mieu/passes`), Wake-up Ping và chuyển cảnh sang bản đồ. Hoàn tất 100% Giai đoạn 2 Màn hình 1 (The Arrival).
- **16/10/2026:** Hoàn thành Task FE-2.2: Dựng thanh chuyển đổi ngôn ngữ (Language Switcher Pill) nổi kính mờ, 3 lá cờ vector sắc nét (Việt Nam, English, 日本語), kết nối Reactive State và Silent Audio Unlock.
- **16/10/2026:** Hoàn thành Task FE-2.1: Dựng Hero Section, Logo MultiGuide, HANOI EDITION và khối Typography nghệ thuật Màn hình ngoài.
- **16/10/2026:** Tích hợp quy tắc bắt buộc xem ảnh UI/UX gốc (`Giao_dien_ngoai.png` & `Giao_dien_Chinh.png`) vào `AGENTS.md` và `TASKS.md`.
- **15/10/2026 (Đêm):** Hoàn thành Giai đoạn 1 (FE-1.1, FE-1.2, FE-1.3): Thiết lập cấu trúc Modular, CSS Tokens di sản, chuẩn hóa Viewport/Safe Area và sinh toàn bộ tài nguyên đồ họa (Hero Image + 9 SVG Icons).
- **15/10/2026:** Khởi tạo bộ tài liệu quy chuẩn phát triển Frontend trong `client/docs_FE/` (AGENTS.md, ARCHITECTURE.md, CONSTRAINTS.md, TASKS.md, PROGRESS.md). Thống nhất kế hoạch và sẵn sàng bước vào giai đoạn code.
