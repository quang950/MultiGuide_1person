# KỊCH BẢN DEMO THUYẾT TRÌNH 5 PHÚT (Task T-4.9)

> **Dự án:** MultiGuide Backend — Hệ Thống Thuyết Minh Du Lịch Tự Động Đa Ngôn Ngữ  
> **Người thực hiện:** Dương Hào Quang  
> **Thời lượng:** 5 phút trình diễn trực quan

---

## I. CHECKLIST CHUẨN BỊ TRƯỚC GIỜ BÁO CÁO (30 PHÚT TRƯỚC)

1. [ ] **Đánh thức Web Service trên Render (Tránh Cold Start):**
   - Render gói miễn phí sẽ ngủ sau 15 phút không có request.
   - Chạy lệnh sau trên terminal hoặc mở trên trình duyệt:
     ```bash
     curl https://multiguide-1person.onrender.com/health
     ```
   - Đảm bảo nhận về: `{"status":"ok"}`.
2. [ ] **Kiểm tra Supabase không bị Pause:**
   - Kiểm tra kết nối CSDL và tính sẵn sàng:
     ```bash
     curl https://multiguide-1person.onrender.com/health/ready
     ```
   - Đảm bảo nhận về: `{"status":"ready","database":"healthy"}`.
3. [ ] **Mở sẵn các Tab trình duyệt phục vụ Demo:**
   - **Tab 1:** Web Test Client (Vercel hoặc `http://127.0.0.1:5500/client/index.html`).
   - **Tab 2:** Swagger UI Live (`https://multiguide-1person.onrender.com/swagger`).
   - **Tab 3:** Supabase Dashboard (Bucket `audio` & Table Editor).
   - **Tab 4:** GitHub Actions (Màn hình xanh toàn bộ pipeline CI và CD).

---

## II. KỊCH BẢN THUYẾT TRÌNH CHI TIẾT THEO TỪNG PHÚT

### ⏱️ PHÚT 1: GIỚI THIỆU BÀI TOÁN & KIẾN TRÚC 3 LỚP (0:00 - 1:00)
- **Lời nói:**
  > *"Kính thưa thầy cô và hội đồng, MultiGuide là hệ thống thuyết minh du lịch tự động đa ngôn ngữ giúp khách tham quan tự do trải nghiệm di tích mà không cần hướng dẫn viên đi kèm. Dự án được em thiết kế theo mô hình Modular Monolith 3 lớp nghiêm ngặt trên nền tảng .NET 10: Tầng Presentation (Api) giao tiếp RESTful, tầng Business độc lập hoàn toàn chứa toàn bộ quy tắc nghiệp vụ và định nghĩa mọi interface, tầng Data hiện thực kết nối CSDL Supabase Postgres và lưu trữ Supabase Storage, và tầng Domain độc lập không phụ thuộc bất kỳ ai. Mọi ranh giới kiến trúc đều được khóa vĩnh viễn bằng 18 bài Architecture Test tự động."*
- **Thao tác màn hình:**
  - Chiếu sơ đồ kiến trúc 3 lớp và sơ đồ thực thể ERD 7 bảng trong tài liệu báo cáo.

---

### ⏱️ PHÚT 2: DEMO TÍNH NĂNG ADMIN & SINH MÃ QR (1:00 - 2:00)
- **Lời nói:**
  > *"Trước tiên, em xin demo luồng quản trị. Admin đăng nhập bằng tài khoản Supabase Auth được phân quyền `role=admin`. Tại Swagger UI, Admin có thể quản lý khu di tích Văn Miếu, các POI, và bản dịch 3 thứ tiếng Việt, Anh, Nhật kèm file audio MP3 đã được kiểm tra tính hợp lệ qua magic bytes. Đặc biệt, hệ thống cung cấp endpoint xuất mã QR du lịch dạng ảnh PNG chất lượng cao."*
- **Thao tác màn hình:**
  - Mở Tab Swagger UI, chọn `GET /admin/areas/{id}/qr` bấm **Try it out** ➔ **Execute**.
  - Hiển thị trực tiếp ảnh mã QR PNG sắc nét dẫn tới đường link web du khách.

---

### ⏱️ PHÚT 3: DEMO DU KHÁCH QUÉT QR & MUA VÉ THAM QUAN (2:00 - 3:00)
- **Lời nói:**
  > *"Khi khách quét mã QR, giao diện Web Client xuất hiện ngay trên điện thoại hoặc trình duyệt. Khách bấm nút 'Mua vé tham quan' qua cổng thanh toán mock 50.000đ. Hệ thống sinh mã Token 64 ký tự CSPRNG cực kỳ an toàn, chỉ lưu mã băm SHA-256 trong CSDL để bảo vệ quyền riêng tư. Vé được cấp có thời hạn 24 giờ và hiển thị đồng hồ đếm ngược trực quan."*
- **Thao tác màn hình:**
  - Chuyển sang Tab Web Client (`client/index.html`).
  - Bấm nút **"Mua vé tham quan"** ➔ Trạng thái vé lập tức chuyển sang **ACTIVE** màu xanh lá, hiện Token rút gọn và thời gian hết hạn vé.

---

### ⏱️ PHÚT 4: DEMO GIẢ LẬP GPS & TỰ ĐỘNG PHÁT THUYẾT MINH (3:00 - 4:00)
- **Lời nói:**
  > *"Tiếp theo là tính năng cốt lõi: Hệ thống không lưu tọa độ GPS của khách để bảo mật quyền riêng tư tuyệt đối, mà chỉ dùng tọa độ để tính toán thuật toán Bounding Box và khoảng cách Haversine. Trên bản đồ, các điểm di tích Văn Miếu được gắn biểu tượng văn hóa đặc trưng. Khi em kéo vị trí khách tham quan bước vào bán kính kích hoạt của Điện Đại Thành hoặc Khuê Văn Các, icon di tích sẽ lập tức phóng to, phát sáng hào quang màu xanh ngọc và lan tỏa sóng radar. Đồng thời, âm thanh thuyết minh từ Supabase Storage được phát ra ngay lập tức và hệ thống tự động ghi nhận Play-Log khi khách nghe xong."*
- **Thao tác màn hình:**
  - Nhấp chuột trên bản đồ kéo vị trí khách lại gần **Điện Đại Thành**.
  - Quan sát icon Điện Đại Thành chuyển màu xanh, phóng to và tỏa sóng radar.
  - Bấm nút **"Nghe audio"** ➔ Thanh Audio Player mở ra và phát giọng đọc AI mượt mà từ Supabase Storage.

---

### ⏱️ PHÚT 5: DEMO CHẤT LƯỢNG KIỂM THỬ, CI/CD & ĐO TẢI HIỆU NĂNG (4:00 - 5:00)
- **Lời nói:**
  > *"Để đảm bảo chất lượng phần mềm đạt chuẩn công nghiệp, toàn bộ giải pháp đạt 297/297 bài test xanh lá 100%. Độ phủ mã nguồn (Code Coverage) của tầng Business đạt tới 96.41%, và em đã thiết lập rào chắn tự động trong GitHub Actions CI để build lập tức fail nếu độ phủ < 60%. Pipeline CD tự động áp dụng Migration Bundle lên CSDL Supabase và chạy smoke test `/health/ready` trong tối đa 3 phút. Cuối cùng, kết quả đo tải bằng k6 cho thấy hệ thống đạt thông lượng 12.96 req/s với tỷ lệ lỗi 0.00% và độ trễ p95 chỉ khoảng 400ms. Em xin kết thúc phần trình bày và sẵn sàng nhận câu hỏi từ hội đồng."*
- **Thao tác màn hình:**
  - Mở Tab GitHub Actions: chỉ vào pipeline CI và CD xanh lá.
  - Chiếu bảng báo cáo đo tải trong [docs/LOAD_TEST_RESULTS.md](file:///d:/MultiGuide_1person/docs/LOAD_TEST_RESULTS.md).

---

## III. CÁC CÂU HỎI HỘI ĐỒNG HAY HỎI & CÁCH TRẢ LỜI

1. **Hỏi:** *Tại sao lại chọn kiến trúc 3 lớp thay vì Clean Architecture hay CQRS?*  
   **Đáp:** *Dự án có thời hạn hoàn thành trong 4 ngày bởi 1 lập trình viên duy nhất. Kiến trúc 3 lớp với tầng Business độc lập hoàn toàn giúp tách bạch rõ ràng giữa nghiệp vụ và hạ tầng ngoại vi, dễ viết Unit Test (đạt 96% coverage) mà không làm tăng độ phức tạp không cần thiết (over-engineering).*
2. **Hỏi:** *Hệ thống bảo vệ quyền riêng tư vị trí của khách như thế nào?*  
   **Đáp:** *Hệ thống tuyệt đối không lưu tọa độ lat, lng vào bất kỳ bảng nào trong CSDL và không log tọa độ ra console/file log. Tọa độ chỉ tồn tại trong bộ nhớ RAM trong thời gian micro-giây để tính toán Haversine rồi giải phóng ngay lập tức. Điều này được kiểm tra bằng 3 bài test kiến trúc tự động trong CI.*
3. **Hỏi:** *Tại sao token vé không lưu trực tiếp mà lại băm SHA-256?*  
   **Đáp:** *Theo nguyên tắc Zero-Trust, kể cả khi CSDL bị lộ, kẻ tấn công cũng không thể giả mạo token để truy cập vào hệ thống vì chỉ có mã băm SHA-256 được lưu trữ, còn chuỗi ngẫu nhiên 32-byte CSPRNG chỉ trả về 1 lần duy nhất cho khách khi mua vé.*
