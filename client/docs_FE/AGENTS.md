# MultiGuide Frontend — Hướng Dẫn Cho AI Agent

> Đọc file này đầu tiên trước mỗi phiên làm việc trên phần Client / Frontend.

## 1. Bản chất & Mục tiêu sản phẩm
- **Sản phẩm:** Web Client (Mobile-first PWA) thuyết minh du lịch tự động đa ngôn ngữ cho du khách tại di tích Văn Miếu – Quốc Tử Giám.
- **Triết lý sản phẩm:** **"Zero-Download / No-Install"** — Khách quét QR code tại cổng là vào app web ngay lập tức, không bắt cài ứng dụng từ App Store / Google Play.
- **Phong cách thiết kế:** *"Heritage Modernism & Calm Technology"* (Hiện đại hóa di sản & Công nghệ tĩnh lặng, lấy cảm hứng từ Apple Maps, Airbnb Experiences, Monocle). Tông màu giấy dó ấm (`#FBF9F5`), mực nho (`#181816`), nhấn đồng cổ (`#C28E58`).

---

## 2. Quy trình làm việc bắt buộc của Agent
1. **Bắt đầu phiên:**
   - Đọc `client/docs_FE/PROGRESS.md` (đang ở đâu, việc dở là gì).
   - Đọc `client/docs_FE/TASKS.md` (task kế tiếp cần làm).
   - Tóm tắt cho người dùng và xác nhận trước khi viết code.
2. **Trong khi code:**
   - 👁️ **BẮT BUỘC ĐỐI CHIẾU ẢNH UI/UX (Visual Ground-Truth):**
     + Khi làm bất kỳ task nào thuộc **Giai đoạn 2 (Màn hình ngoài)**: Agent **BẮT BUỘC** phải gọi công cụ `view_file` xem trực tiếp `client/assets/Giao_dien_ngoai.png` trước khi viết HTML/CSS. Soi chiếu từng pixel: logo, cờ ngôn ngữ, typography, thẻ vé, nút bấm, footer.
     + Khi làm bất kỳ task nào thuộc **Giai đoạn 3, 4, 5, 6 (Màn hình trong & Bản đồ)**: Agent **BẮT BUỘC** phải gọi công cụ `view_file` xem trực tiếp `client/assets/Giao_dien_Chinh.png` trước khi viết code. Soi chiếu: vị trí và thứ tự 6 POIs, thẻ header nổi, radar GPS, mini player ghim đáy, thanh 3 tabs.
     + **Tuyệt đối không tự ý bịa đặt hay làm sai lệch** so với 2 bức ảnh chuẩn này.
   - Tuân thủ nghiêm ngặt các quy tắc kiến trúc trong `client/docs_FE/ARCHITECTURE.md` và các ràng buộc trong `client/docs_FE/CONSTRAINTS.md`.
   - Mỗi lượt chỉ làm **1 task** trong `client/docs_FE/TASKS.md`.
   - Giữ mã nguồn thuần (Vanilla JS ES Modules, Clean CSS), không tùy tiện cài thêm các thư viện cồng kềnh làm phình to bundle.
3. **Kết thúc task:**
   - Kiểm tra giao diện hiển thị đúng trên mobile viewport (`390x844px` iPhone).
   - Đánh dấu `[x]` vào task tương ứng trong `client/docs_FE/TASKS.md`.
   - Cập nhật nhật ký trong `client/docs_FE/PROGRESS.md`.

---

## 3. Tech Stack cốt lõi
- **Ngôn ngữ:** Vanilla HTML5, CSS3 hiện đại (CSS Variables, Flexbox, Grid, Glassmorphism), JavaScript ES6+ Modules.
- **Bản đồ:** Leaflet.js (v1.9.4) tích hợp CartoDB Positron / Voyager vector tiles (phong cách giấy dó thanh lịch, nhẹ nhàng).
- **Audio & Media:** HTML5 Audio API + Media Session API (cho phép điều khiển trên màn hình khóa điện thoại).
- **Backend API:** Kết nối ASP.NET Core .NET 10 tại `https://multiguide-1person.onrender.com` (hoặc `http://localhost:5267` khi test local).
- **Triển khai:** Tối ưu PWA tĩnh trên Vercel.

---

## 4. Các quy tắc "bất khả xâm phạm" (Hard Rules)
1. **Bảo mật & Quyền riêng tư:** Tuyệt đối không lưu vĩnh viễn tọa độ GPS của người dùng vào `localStorage` hay cookie. Không log token vé hay tọa độ nhạy cảm ra `console.log`.
2. **Mobile First & Safe Area:** Toàn bộ giao diện phải fit hoàn hảo với đơn vị `100dvh` và tôn trọng vùng an toàn tai thỏ / thanh vuốt Home iPhone (`env(safe-area-inset-top)`, `env(safe-area-inset-bottom)`).
3. **Autoplay Policy:** Luôn tôn trọng chính sách chặn âm thanh tự động của trình duyệt di động. Phải unlock AudioContext thông qua tương tác người dùng (nút "Kích hoạt vé" hoặc nút gạt "GPS Audio On").
4. **Offline Resilience:** Khi mất mạng tạm thời trong khuôn viên, app vẫn phải hiển thị giao diện và thông tin các điểm đã tải mà không bị crash trắng màn hình.
