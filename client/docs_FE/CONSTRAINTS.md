# CONSTRAINTS — Ràng Buộc Kỹ Thuật & Giải Pháp (Mobile Web PWA)

Hệ thống hoạt động dưới dạng **Web App di động không cần cài đặt (Zero-Install Web App)** phục vụ du khách thực địa tại di tích. Môi trường này có 5 ràng buộc kỹ thuật cốt lõi cần xử lý triệt để:

---

## 1. Ràng buộc Autoplay Âm thanh (iOS Safari & Android Chrome)
* **Vấn đề:** 
  Cả iOS Safari và Google Chrome trên Android đều áp dụng chính sách chặn nghiêm ngặt: Trình duyệt **từ chối gọi `audio.play()`** nếu lệnh này không bắt nguồn trực tiếp từ một sự kiện chạm/click của người dùng (`User Gesture`). Nếu hệ thống tự động phát audio khi GPS bước vào bán kính trigger, trình duyệt sẽ ném lỗi `NotAllowedError: play() failed because the user didn't interact with the document first`.
* **Giải pháp kỹ thuật:**
  1. **Unlock AudioContext sớm:** Khi người dùng bấm nút **"Kích hoạt vé tham quan (1-Tap Buy)"** hoặc chọn chuyển đổi ngôn ngữ, trình duyệt sẽ phát một file âm thanh rỗng/im lặng 0.1 giây ở mức âm lượng 0 (`silentAudio.play()`). Thao tác này mở khóa vĩnh viễn quyền phát âm thanh cho phiên làm việc hiện tại.
  2. **Trạng thái công tắc "GPS Audio ON":** Trên giao diện bản đồ có pill chỉ báo `● GPS audio on`. Nếu trình duyệt vẫn chặn, pill này chuyển thành nút tương tác nhẹ nhàng: `"Nhấp để bật thuyết minh tự động"`.

---

## 2. Ràng buộc Phát Âm Thanh Khi Tắt Màn Hình (Background Playback)
* **Vấn đề:** 
  Du khách thường bật thuyết minh rồi đút điện thoại vào túi áo/túi quần hoặc màn hình tự động khóa sau 30 giây. Khi màn hình khóa, JavaScript trên trình duyệt di động thường bị hệ điều hành "đóng băng" (freeze timers & background fetch).
* **Giải pháp kỹ thuật:**
  1. **Tích hợp Media Session API (`navigator.mediaSession`):**
     - Đăng ký metadata cho bài thuyết minh hiện tại:
       ```javascript
       navigator.mediaSession.metadata = new MediaMetadata({
         title: poi.title,
         artist: 'MultiGuide — Văn Miếu Quốc Tử Giám',
         album: 'Văn Miếu Collection',
         artwork: [{ src: '/assets/khue-van-cac.jpg', sizes: '512x512', type: 'image/jpeg' }]
       });
       ```
     - Cài đặt Action Handlers (`play`, `pause`, `seekto`, `stop`). Nhờ đó, bài thuyết minh xuất hiện như một bài hát trên Spotify/Apple Music ở màn hình khóa, người dùng có thể tạm dừng hoặc phát tiếp mà không cần mở khóa điện thoại.
  2. **Screen Wake Lock API:** Khi người dùng đang mở xem bản đồ, kích hoạt `navigator.wakeLock.request('screen')` để ngăn màn hình tự tắt làm gián đoạn việc xem hướng đi.

---

## 3. Ràng buộc Khởi động Lạnh Backend (Render Free Tier Cold Start)
* **Vấn đề:** 
  Web service Backend được triển khai trên gói Render Free, sẽ tự động "ngủ đông" (sleep) sau 15 phút không nhận request. Thời gian đánh thức server có thể mất từ 30 đến 50 giây. Nếu du khách quét QR trúng lúc server đang ngủ, việc mua vé hoặc tải dữ liệu có thể bị treo.
* **Giải pháp kỹ thuật:**
  1. **Wake-up Ping tức thời:** Ngay dòng đầu tiên khi tải file `index.html`, Client chủ động gửi ngầm một request `fetch('https://multiguide-1person.onrender.com/health')` để kích hoạt server thức dậy ngay trong lúc du khách đang xem ảnh chào đón và đọc giới thiệu ở Màn 1.
  2. **Skeleton & Shimmer Loading:** Nếu server đang thức dậy, hiển thị hiệu ứng ánh sáng lượn sương mờ sang trọng, thông báo `"Đang kết nối không gian di sản..."` thay vì để màn hình đơ cứng.

---

## 4. Ràng buộc Tọa độ GPS & Quyền Riêng Tư (Privacy & Accuracy)
* **Vấn đề:**
  - Trình duyệt di động chỉ cho phép truy cập `navigator.geolocation` khi trang web chạy trên giao thức **HTTPS an toàn**.
  - Tín hiệu GPS ngoài trời khi đi dưới các tán cây cổ thụ và tường gạch Văn Miếu có thể bị sai số từ 5m đến 15m, khiến vị trí nhảy liên tục.
* **Giải pháp kỹ thuật:**
  1. **HTTPS bắt buộc:** Client được cấu hình và triển khai qua Vercel với chứng chỉ SSL/TLS tự động.
  2. **Bộ lọc làm mịn tọa độ (Location Smoothing / Dead Reckoning):** Chỉ cập nhật vị trí và gọi lại API `nearby` khi khoảng cách dịch chuyển $> 5$ mét hoặc sau mỗi chu kỳ tối thiểu 3 giây để tránh làm quá tải mạng.
  3. **Không lưu trữ GPS:** Tọa độ chỉ tồn tại trong bộ nhớ Client và gửi trong tham số URL của request `GET /pois/nearby`. Backend không lưu vào CSDL và Client không lưu vào `localStorage`, đảm bảo quyền riêng tư tuyệt đối cho khách.
  4. **Chế độ Giả lập (Simulator Mode):** Cho phép nhấp chuột trên bản đồ để di chuyển vị trí du khách nhằm phục vụ thuyết trình hoặc kiểm thử khi không đứng trực tiếp tại Hà Nội.

---

## 5. Ràng buộc Công thái học Cảm ứng & Viewport Mobile (iOS Safari)
* **Vấn đề:**
  - Safari trên iOS có thanh URL tự co giãn khi cuộn, dùng đơn vị CSS `100vh` cũ sẽ khiến chân trang (Bottom Navigation) bị thanh Safari che khuất.
  - Cử chỉ chạm đúp (double-tap) có thể kích hoạt phóng to trang web không mong muốn.
* **Giải pháp kỹ thuật:**
  1. **Sử dụng đơn vị Viewport hiện đại:** Đặt kích thước toàn màn hình bằng `height: 100dvh` (Dynamic Viewport Height).
  2. **Xử lý Safe Area Inset:** 
     ```css
     .bottom-nav {
       padding-bottom: calc(12px + env(safe-area-inset-bottom, 0px));
     }
     .top-header {
       padding-top: calc(12px + env(safe-area-inset-top, 0px));
     }
     ```
  3. **Vô hiệu hóa cử chỉ ngoài ý muốn trên UI:** Đặt `touch-action: manipulation` cho các nút bấm và thanh điều khiển; chỉ cho phép cử chỉ pinch-to-zoom bên trong phạm vi khung bản đồ Leaflet.
