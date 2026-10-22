> Nguồn sự thật cho yêu cầu. Bản Word gốc: PRD_v1.2_MultiGuide_Backend_3_lop_CICD.docx. Sửa yêu cầu → sửa file này và ghi vào docs/DECISIONS.md.

# PRD v1.3 — MultiGuide: Backend hệ thống thuyết minh tự động đa ngôn ngữ

Môn Công nghệ phần mềm · Phiên bản 1.3 · 22/10/2026 · Tác giả: Quang (đồ án cá nhân) · Phạm vi tài liệu: **Backend kiến trúc 3 lớp + CI/CD + Mở rộng Tính năng Thực tế**

**Quy ước:** (MVP) = nằm trong bản MVP v1.2; (v1.3) = tính năng mở rộng thực tế theo phản hồi trải nghiệm; không đánh dấu = Later. Ưu tiên theo MoSCoW. Mọi thời gian lưu và trả về theo **UTC (ISO 8601)**.

**Lịch sử tiến hóa tài liệu:**
- **v1.1 → v1.2 (Baseline MVP 4 ngày):** Tối giản phạm vi: Supabase Postgres (Session pooler), Supabase Storage, Supabase Auth, Render (free), bỏ VisitorSession, 1 role admin, cắt upload ảnh, rate limit đơn giản.
- **v1.2 → v1.3 (Nâng cấp thực tiễn & Trải nghiệm thực tế):**
  1. *Đo lường Du khách trực tuyến thời gian thực (Live Active Visitors)*: Cơ chế Heartbeat 15s kết hợp In-Memory Sliding Window 30s (`ConcurrentDictionary`) và Auto-polling 15s tại Admin Dashboard (không lưu GPS/token, bảo toàn I/O DB).
  2. *Tự động hóa Geofence (Auto-play Calm Tech)*: Tự động mở Bottom Sheet & phát thuyết minh khi vào bán kính $\le 30$m kèm cơ chế chống lặp tín hiệu (Anti-jitter) và đệm thoát (Hysteresis buffer +8m).
  3. *Tự động sinh audio qua Microsoft Edge Neural TTS*: Endpoint `POST /admin/translations/{id}/generate-audio` kết nối WebSocket Edge TTS không tốn phí, giải quyết bài toán thiếu audio mẫu.
  4. *Trợ lý AI Gemini 2.5 Flash*: Endpoint `POST /admin/pois/{id}/ai-summary` hỗ trợ Admin sinh tóm tắt lịch sử hiện vật tự động.
  5. *Bổ sung trường `image_url` cho POI*: Phục vụ hiển thị hình ảnh di tích/hiện vật trên bản đồ & giao diện du khách.
  6. *Xác thực Admin an toàn (Zero-Exposure Auth Proxy)*: Endpoint `POST /api/v1/admin/auth/login` và `/refresh` ủy quyền qua Supabase Auth, bảo vệ tuyệt đối API key không phơi bày ra trình duyệt.
  7. *Giao diện Quản trị tương tác & Trải nghiệm PWA*: Xây dựng hoàn chỉnh SPA Admin (`client/admin.html`), hỗ trợ nút Đăng xuất/Xóa vé du khách và PWA Service Worker với cơ chế dọn dẹp cache chủ động.

## 1. Tổng quan

### 1.1 Vấn đề

Khách du lịch quốc tế thiếu hướng dẫn viên đúng ngôn ngữ; thuê guide tốn kém, bảng tĩnh nhàm và ít ngôn ngữ. Đơn vị vận hành khó cập nhật nội dung đa ngôn ngữ đồng bộ cho nhiều điểm tham quan.

### 1.2 Giải pháp

**MultiGuide** là hệ thống hướng dẫn tham quan: khách quét QR tại cổng, trả phí một lần (2 USD, cổng thanh toán mock ở MVP) để nhận AccessPass; thiết bị gửi tọa độ lên server, server trả các điểm tham quan (POI) nằm trong bán kính cấu hình (ví dụ 100 m), khách xem thông tin và nghe audio đúng ngôn ngữ. Admin quản lý nội dung qua API và theo dõi lưu lượng du khách trực tuyến.

**Tài liệu này đặc tả Backend** (REST API + CSDL + CI/CD) cùng sự phối hợp với **Web Client du khách & Cổng quản trị Admin Web SPA** (Leaflet, Service Worker dọn dẹp cache).

### 1.3 Phạm vi chốt

| **Hạng mục**         | **Giá trị chốt (MVP v1.2 & Mở rộng v1.3)**                                                           |
|----------------------|-----------------------------------------------------------------------------------------------------|
| Khu du lịch demo     | 1 khu (Văn Miếu - Quốc Tử Giám / demo area)                                                        |
| Số POI               | 6 POI thực tế                                                                                       |
| Ngôn ngữ             | vi, en, ja (thêm ngôn ngữ = thêm dữ liệu, không sửa code)                                           |
| Thanh toán           | Mock, qua interface IPaymentGateway                                                                 |
| AccessPass           | Hạn 24 giờ từ issued_at, mỗi pass gắn 1 khu                                                         |
| Stack                | ASP.NET Core (.NET 10), EF Core + Npgsql, Supabase (Postgres, Storage, Auth), Render, GitHub Actions |
| Tìm POI gần          | Server stateless: nhận lat/lng, trả POI trong bán kính; tính Haversine trong Service                |
| Live Telemetry       | Đo khách online thời gian thực: Heartbeat 15s + In-Memory Sliding Window 30s + Auto-poll 15s        |
| Auto-play Geo        | Khách vào $\le 30$m tự mở Drawer & phát audio; có Anti-jitter và Hysteresis buffer (+8m)           |
| AI & TTS             | Edge Neural TTS tự sinh file MP3; Gemini 2.5 Flash tóm tắt lịch sử POI                              |
| Admin Authentication | Zero-Exposure Proxy: `POST /api/v1/admin/auth/login` và `/refresh` (tự động gia hạn 30 ngày)        |
| Quản lý phiên & PWA  | Client có nút Thoát vé/Đăng xuất; PWA Service Worker hỗ trợ dọn dẹp cache chủ động khi deploy mới  |

**Ngoài phạm vi:** thanh toán thật (Stripe/PayOS), offline pack trọn gói toàn bộ audio trước, PostGIS.

### 1.4 Actor

- **Tourist:** khách dùng điện thoại, không cài app, không tài khoản.

- **Admin:** nhân viên quản lý nội dung; đăng nhập qua Supabase Auth, có claim role = admin.

- **Payment Provider:** hệ thống ngoài (Mock ở MVP).

### 1.5 Mục tiêu và chỉ số

| **Chỉ số**                                | **Ngưỡng MVP** | **Cách đo**                              |
|-------------------------------------------|----------------|------------------------------------------|
| p95 GET /pois/nearby (khi server đã thức) | ≤ 500 ms       | Test tải đơn giản (k6) trước khi báo cáo |
| Coverage tầng Business                    | ≥ 60%          | Coverlet, CI fail nếu thấp hơn           |
| CI trên main                              | Luôn xanh      | Branch protection                        |
| Từ merge đến /health/ready OK             | ≤ 10 phút      | Thời gian workflow CD                    |

### 1.6 Giả định và ràng buộc

- Đồ án cá nhân, **4 ngày**, một người làm cả BE, DevOps và test client.

- Render free **ngủ sau ~15 phút không có request**, request đầu có thể chậm 30–60 giây; Supabase free tạm dừng project sau 1 tuần không hoạt động. Trước buổi demo phải gọi /health để đánh thức.

- Render cấp HTTPS sẵn trên \*.onrender.com nên không cần domain riêng; Geolocation của trình duyệt cần HTTPS (Vercel cũng có sẵn).

- Test client cho phép **bấm trên bản đồ để giả lập vị trí**, nên kiểm thử geofence không phụ thuộc GPS thật.

## 2. Product Backlog

### 2.1 Epic

| **ID** | **Epic**                                                         | **Phiên bản** |
|--------|------------------------------------------------------------------|---------------|
| E1     | Khu và AccessPass (thanh toán mock)                              | (MVP v1.2)    |
| E2     | Tìm POI gần & Kích hoạt Geofencing (Geo)                         | (v1.3)        |
| E3     | Nội dung, audio và log lượt nghe                                 | (MVP v1.2)    |
| E4     | Quản trị nội dung (Admin API + Supabase Auth)                    | (MVP v1.2)    |
| E5     | Vận hành: CI/CD, health                                          | (MVP v1.2)    |
| E6     | Giọng đọc AI & Đo lường lưu lượng trực tuyến (Live Telemetry)     | (v1.3)        |

### 2.2 User story (viết theo góc nhìn API & Trải nghiệm thực tế)

Acceptance Criteria chi tiết, mã lỗi và phân quyền của từng endpoint nằm ở **mục 7**; ở đây chỉ ghi mục tiêu để tránh lặp.

| **Epic** | **ID** | **Story**                                                                                                 | **Ưu tiên** | **Phiên bản** |
|----------|--------|-----------------------------------------------------------------------------------------------------------|-------------|---------------|
| E1       | US-01  | Lấy thông tin khu và danh sách ngôn ngữ từ mã QR                                                          | Must        | MVP v1.2      |
| E1       | US-02  | Thanh toán mock, nhận AccessPass (có Idempotency-Key)                                                     | Must        | MVP v1.2      |
| E1       | US-03  | Chặn truy cập khi thiếu hoặc sai AccessPass (middleware)                                                  | Must        | MVP v1.2      |
| E1       | US-04  | Xem trạng thái và hạn pass hiện tại                                                                       | Should      | MVP v1.2      |
| E2       | US-05  | Gửi tọa độ, nhận POI trong bán kính kèm khoảng cách & kích hoạt thuyết minh tự động (Auto-play Geofence)     | Must        | v1.3          |
| E3       | US-06  | Lấy nội dung POI theo ngôn ngữ, có fallback                                                               | Must        | MVP v1.2      |
| E3       | US-07  | Nhận audio_url tạm thời (signed URL) từ Supabase Storage                                                  | Must        | MVP v1.2      |
| E3       | US-08  | Ghi PlayLog lượt nghe                                                                                     | Should      | MVP v1.2      |
| E4       | US-09  | Xác thực admin bằng JWT của Supabase Auth, chỉ role admin được vào                                        | Must        | MVP v1.2      |
| E4       | US-22  | Đăng nhập Quản trị viên qua Proxy API (Zero-Exposure) và tự động làm mới Refresh Token (30 ngày)          | Must        | v1.3          |
| E4       | US-10  | CRUD khu du lịch                                                                                          | Must        | MVP v1.2      |
| E4       | US-11  | CRUD POI (có phân trang, hỗ trợ image_url)                                                                | Must        | v1.3          |
| E4       | US-12  | CRUD bản dịch                                                                                             | Must        | MVP v1.2      |
| E4       | US-13  | Tải audio mp3 cho bản dịch lên Supabase Storage                                                           | Must        | MVP v1.2      |
| E4       | US-14  | Sinh QR cho khu (PNG)                                                                                     | Could       | MVP v1.2      |
| E4       | US-19  | Sinh tóm tắt lịch sử cho POI bằng Gemini 2.5 Flash AI (Google AI Studio Free)                             | Could       | v1.3          |
| E5       | US-15  | CI mỗi PR/push, branch protection                                                                         | Must        | MVP v1.2      |
| E5       | US-16  | CD: merge main → migration → deploy hook Render → smoke test                                              | Must        | MVP v1.2      |
| E5       | US-17  | Health check: /health (liveness) và /health/ready (kiểm tra DB)                                           | Must        | MVP v1.2      |
| E6       | US-20  | Tự động sinh file audio MP3 cho bản dịch qua Microsoft Edge Neural TTS                                    | Should      | v1.3          |
| E6       | US-21  | Đo lường và hiển thị số lượng du khách trực tuyến thời gian thực (Live Active Visitors Dashboard)         | Should      | v1.3          |
| —        | US-18  | Thanh toán thật, webhook (Later)                                                                          | Later       | Later         |

### 2.3 Ma trận Đối chiếu Yêu cầu & Tiến hóa Thực tế (Traceability & Evolution Matrix)

Bảng ma trận này dùng để đối chiếu xuyên suốt giữa bản đặc tả lý thuyết ban đầu (PRD v1.2) với sản phẩm phần mềm thực tế sau các vòng lặp Agile (PRD v1.3), phục vụ trực tiếp cho báo cáo đồ án và bảo vệ môn học.

| Mã Story / Hạng mục | Đặc tả gốc (PRD v1.2 Baseline) | Hiện trạng thực tế (PRD v1.3 Implementation) | Động lực / Lý do tiến hóa (Rationale) | Tầng & File Code / Endpoint minh chứng | Tương thích kiến trúc & DB |
|---|---|---|---|---|---|
| **US-05 & Geofence** | Client gửi lat/lng, Server trả danh sách POI kèm `within_trigger` boolean. Khách phải tự bấm nút để nghe. | **Auto-play Geofence (Calm Tech):** Khách bước vào $\le 30$m tự động bung Bottom Sheet và play audio. Áp dụng cơ chế **Anti-jitter** (`lastAutoTriggeredPoiId`) và **đệm thoát Hysteresis (+8m)**. | Trải nghiệm du lịch thực địa rảnh tay (hands-free); loại bỏ hiện tượng âm thanh bị giật/bật tắt liên tục do GPS sai số khi đứng yên tại một điểm. | `client/js/map.js` (`onLocationFound`), `PoisController.cs` (`GET /pois/nearby`). | Stateless backend giữ nguyên; 100% tuân thủ bảo mật không lưu GPS. |
| **US-11 & Entity POI** | Bảng `poi` chỉ chứa lat, lng, trigger_radius_m, priority, is_active. Chưa có ảnh. | Bổ sung cột **`image_url`** (varchar 500, nullable) vào bảng `poi` và hiển thị trên giao diện thẻ di tích / bottom sheet. | Trực quan hóa hiện vật di tích cho du khách, giúp khách đối chiếu đúng cổ vật/công trình trước mặt. | Migration `AddImageUrlToPoi`, `Poi.cs` (Domain), `client/admin.html`, `client/index.html`. | Migration tương thích ngược (nullable); không vi phạm 3 lớp. |
| **US-13 & US-20 (Audio TTS)** | Bắt buộc Admin phải thu âm sẵn file MP3 và tải thủ công lên Supabase Storage qua `POST /admin/translations/{id}/audio`. | Tích hợp **Microsoft Edge Neural TTS** qua WebSocket: tự động sinh giọng đọc tự nhiên (vi, en, ja...) và lưu trữ trực tiếp vào Supabase Storage qua 1 click. | Giải quyết rào cản chi phí và công sức thu âm đa ngôn ngữ cho 6 POI trong thời gian ngắn (4 ngày). | `ITtsService.cs` (Business), `EdgeTtsService.cs` (Data), `AdminTranslationsController.cs` (`POST /admin/translations/{id}/generate-audio`). | Tách interface chuẩn 3 lớp; NetArchTest đảm bảo Business không phụ thuộc SDK. |
| **US-19 (AI Assistant)** | Dự kiến (Could): Tích hợp AI hỗ trợ sinh tóm tắt lịch sử POI. | Triển khai hoàn chỉnh **Google Gemini 2.5 Flash** (Free tier qua Google AI Studio REST API) để tự động sinh tóm tắt lịch sử và câu chuyện di tích 150–200 từ. | Giúp Admin nhanh chóng tạo nội dung thuyết minh chất lượng cao, chuẩn xác mà không cần biên soạn thủ công. | `IAiService.cs` (Business), `GeminiAiService.cs` (Data), `AdminPoisController.cs` (`POST /admin/pois/{id}/ai-summary`). | HttpClient thuần, không phụ thuộc thư viện ngoài nặng; cấu hình API Key qua Secret. |
| **US-21 (Live Telemetry)** | Chỉ có PlayLog tĩnh sau khi nghe xong (`POST /play-logs`). Dashboard chỉ xem lịch sử nghe tích lũy. | **Live Active Visitors Telemetry:** Khách gửi Heartbeat định kỳ 15s (`POST /passes/heartbeat`). Server quản lý phiên bằng **In-Memory Sliding Window 30s** (`ConcurrentDictionary`). Admin Dashboard auto-poll 15s hiển thị widget trực tuyến. | Ban quản lý cần biết chính xác số lượng du khách đang có mặt trong khu di tích ngay tại thời điểm thực để điều phối và kiểm soát an ninh. | `IVisitorTracker.cs` (Business), `ActiveVisitorTracker.cs` (Data - Singleton), `PassesController.cs`, `AdminAnalyticsController.cs`. | Không ghi vào CSDL $\rightarrow$ 0 I/O disk, không nghẽn connection pooler Supabase, không lưu GPS hay token nhạy cảm. |
| **US-22 (Admin Auth Proxy)** | Admin đăng nhập trực tiếp trên Supabase Dashboard; Backend không có endpoint auth; API chỉ kiểm tra JWT có sẵn. | **Zero-Exposure Admin Auth Proxy:** `POST /api/v1/admin/auth/login` và `POST /api/v1/admin/auth/refresh`. Tự động cấp access_token và refresh_token gia hạn 30 ngày. | Bảo vệ tuyệt đối Supabase Keys (không để lộ Anon Key/Service Key ra client); Admin đăng nhập trực tiếp ngay trên trang web quản trị SPA. | `AdminAuthController.cs` (Presentation), `IAdminAuthService.cs` (Business), `SupabaseAdminAuthService.cs` (Data). | Chuẩn 3 lớp qua DI; không lưu mật khẩu ở CSDL backend; ủy quyền xác thực cho Supabase Auth. |
| **Admin SPA & PWA Cache** | Web client chỉ là bản thử nghiệm Leaflet đơn giản; Admin quản lý qua API/Swagger; chưa có dọn cache PWA. | Hoàn thiện **Single Page App Admin** (`client/admin.html`) có bản đồ chọn vị trí POI trực quan, preview ảnh, phát audio; bổ sung nút Thoát vé và PWA Service Worker (`client/sw.js`) dọn cache chủ động. | Giúp ban quản lý vận hành trực quan; khách du lịch có thể xóa vé mua vé mới; người dùng không bị kẹt cache UI cũ khi deploy. | `client/admin.html`, `client/js/admin.js`, `client/sw.js`, `client/js/ui.js`. | Kiến trúc tách biệt Client - Server; Client tương tác 100% qua REST API chuẩn. |

### 2.4 Backlog tương lai (Later)

| **Ý tưởng**                                           | **Ghi chú**                 |
|-------------------------------------------------------|-----------------------------|
| Cổng thanh toán thật (Stripe/PayOS), webhook          | Thay MockPaymentGateway     |
| VisitorSession, phân tích luồng di chuyển đa điểm     | Khi cần phân tích hành vi nâng cao |
| PostGIS / spatial index                               | Khi số lượng POI lên đến hàng nghìn |
| Content-status (Draft/Review/Published), nhiều role admin | Mở rộng quy mô biên tập viên |
| Rate limit chi tiết theo token, Serilog tập trung    | Khi triển khai thương mại quy mô lớn |

### 2.5 Luồng hoạt động chính của khách (v1.3)

1. **Khởi tạo & Nhận quyền truy cập:**
   - FE quét mã QR tại cổng di tích $\to$ gọi `GET /areas/{code}` để lấy thông tin khu và bảng giá.
   - Gọi `POST /areas/{code}/passes` (kèm header `Idempotency-Key`) $\to$ nhận `access_token` (trả về 1 lần duy nhất), FE lưu vào `localStorage`.

2. **Duy trì hiện diện trực tuyến (Heartbeat Loop):**
   - Định kỳ mỗi **15 giây**, thiết bị khách tự động gửi `POST /passes/heartbeat` (kèm `Authorization: Bearer <access_token>`).
   - Server cập nhật timestamp vào `ActiveVisitorTracker` (In-Memory sliding window 30s) $\to$ Admin Dashboard phản ánh thời gian thực số lượng khách đang tham quan.

3. **Định vị & Quét di tích lân cận (Geofencing):**
   - Thiết bị gửi tọa độ GPS định kỳ (hoặc giả lập trên bản đồ): `GET /pois/nearby?lat={lat}&lng={lng}&lang={lang}&radius=100`.
   - Server tính toán khoảng cách Haversine và trả về danh sách POI kèm cờ `within_trigger`.

4. **Trải nghiệm Thuyết minh Tự động (Auto-play Calm Tech):**
   - Khi khách tiến vào bán kính kích hoạt ($\le 30$m) của một POI và POI đó chưa từng phát trước đó:
     * Giao diện du khách tự động trượt mở ngăn thông tin chi tiết (Bottom Sheet).
     * Tự động gọi `GET /pois/{id}?lang={lang}` lấy signed URL âm thanh và phát trực tiếp.
     * Cơ chế **Anti-jitter** ghi nhớ `lastAutoTriggeredPoiId` và **đệm Hysteresis (+8m)** để âm thanh không bị giật, ngắt quãng hay phát lặp lại khi khách đứng yên hoặc di chuyển quanh rìa bán kính.

5. **Ghi nhận lịch sử nghe:**
   - Khi nghe xong hoặc khách chủ động bấm dừng $\to$ client gửi `POST /play-logs` ghi nhận thời lượng nghe tích lũy vào hệ thống.

## 3. Yêu cầu phi chức năng (NFR)

| **Nhóm**           | **Yêu cầu**                                                                                                                                                                                                                                                                                                         |
|--------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Hiệu năng          | Theo mục 1.5. Nearby lọc bounding box trên (lat, lng) trước rồi mới tính Haversine.                                                                                                                                                                                                                                 |
| Bảo mật            | HTTPS; AccessPass opaque, DB chỉ lưu SHA-256; admin xác thực bằng JWT Supabase (kiểm chữ ký, issuer, hạn); bucket Storage để private; **bật RLS không policy** cho mọi bảng, backend chỉ kết nối bằng chuỗi kết nối server; service-role key chỉ nằm ở biến môi trường server; CORS chỉ cho origin của test client. |
| Rate limit         | Chỉ áp dụng cho POST /areas/{code}/passes: 5 request/phút/IP (ASP.NET Core RateLimiter). Đăng nhập admin do Supabase xử lý.                                                                                                                                                                                         |
| Quyền riêng tư     | Không lưu tọa độ khách ở DB hay log; log không ghi lat/lng và token.                                                                                                                                                                                                                                                |
| Lỗi                | ProblemDetails (RFC 7807) kèm trường code ổn định cho FE.                                                                                                                                                                                                                                                           |
| Log                | Log JSON cơ bản của ASP.NET Core; không ghi dữ liệu nhạy cảm.                                                                                                                                                                                                                                                       |
| Cấu hình và secret | Biến môi trường trên Render và GitHub Secrets; không commit secret; có .env.example.                                                                                                                                                                                                                                |
| Dữ liệu            | UTF-8, thời gian lưu UTC (timestamptz); migration EF Core chạy trong pipeline, không tự chạy khi API khởi động; chỉ thêm cột/bảng (tương thích ngược) để rollback code an toàn.                                                                                                                                     |
| Test               | Unit test Service (mock Repository/Gateway/Storage); test kiến trúc (NetArchTest); 1–2 integration test với Testcontainers Postgres cho luồng pass và nearby.                                                                                                                                                       |
| Tương thích        | API versioning /api/v1; Swagger bật ở demo.                                                                                                                                                                                                                                                                         |

## 4. Kiến trúc 3 lớp

Backend là **modular monolith 3 lớp** gồm 3 project chính và 1 project dùng chung.

### 4.1 Phân tầng và trách nhiệm

<table>
<colgroup>
<col style="width: 24%" />
<col style="width: 40%" />
<col style="width: 34%" />
</colgroup>
<thead>
<tr class="header">
<th><strong>Tầng (project)</strong></th>
<th><strong>Chứa</strong></th>
<th><strong>Không được</strong></th>
</tr>
</thead>
<tbody>
<tr class="odd">
<td><p><strong>Presentation</strong></p>
<p>(MultiGuide.Api)</p></td>
<td>Controller, DTO request/response, validation, middleware (JWT Supabase, AccessPass, ProblemDetails), cấu hình DI (composition root)</td>
<td>Chứa quy tắc nghiệp vụ; truy cập DbContext trực tiếp</td>
</tr>
<tr class="even">
<td><p><strong>Business</strong></p>
<p>(MultiGuide.Business)</p></td>
<td>Service, quy tắc nghiệp vụ, <strong>định nghĩa mọi interface</strong>: IXxxRepository, IPaymentGateway, IFileStorage, IClock</td>
<td>Biết EF Core, Supabase SDK/HTTP, SDK thanh toán</td>
</tr>
<tr class="odd">
<td><p><strong>Data/Infra</strong></p>
<p>(MultiGuide.Data)</p></td>
<td>DbContext, migration, Repository, SupabaseFileStorage (implement IFileStorage), MockPaymentGateway</td>
<td>Chứa quy tắc nghiệp vụ</td>
</tr>
<tr class="even">
<td><p><strong>Shared</strong></p>
<p>(MultiGuide.Domain)</p></td>
<td>Entity, enum, hằng số, exception nghiệp vụ</td>
<td>Phụ thuộc bất kỳ tầng nào</td>
</tr>
</tbody>
</table>

**Quy tắc phụ thuộc:** Api → Business ← Data, cả hai cùng tham chiếu Domain. Data implement interface do Business định nghĩa; Api đăng ký DI. Business không tham chiếu Data hay Api. Kiểm tra bằng NetArchTest trong CI.

### 4.2 Thành phần

| **Thành phần**                                                                           | **Tầng**     | **Trách nhiệm**                                                               |
|------------------------------------------------------------------------------------------|--------------|-------------------------------------------------------------------------------|
| Controller: Area, Pass, Poi, Play, AdminArea, AdminPoi, AdminTranslation, AdminAnalytics | Presentation | Nhận request, validate DTO, gọi Service, trả response                         |
| Middleware: ExceptionHandler, SupabaseJwt (admin), AccessPass                            | Presentation | Chỉ xác thực danh tính và tính hợp lệ của token                               |
| AreaService                                                                              | Business     | Lấy khu, danh sách ngôn ngữ                                                   |
| PassService                                                                              | Business     | Idempotency, gọi IPaymentGateway, cấp và xác minh pass (băm, hạn, trạng thái) |
| GeoService                                                                               | Business     | Bounding box, Haversine, within_trigger, sắp xếp (stateless)                  |
| ContentService                                                                           | Business     | Nội dung, fallback ngôn ngữ, xin signed URL qua IFileStorage                  |
| AdminContentService                                                                      | Business     | CRUD khu/POI/bản dịch, validate upload audio, kích hoạt TTS tự động           |
| AnalyticsService                                                                         | Business     | Ghi PlayLog, tổng hợp Dashboard KPIs và số liệu Active Visitors               |
| Interface: ITtsService, IVisitorTracker, IAiService, IFileStorage, IPaymentGateway        | Business     | Định nghĩa hợp đồng nghiệp vụ, đảm bảo Dependency Inversion Principle         |
| ActiveVisitorTracker (Singleton RAM)                                                     | Data/Infra   | Quản lý In-Memory Sliding Window 30s (`ConcurrentDictionary`) đo du khách online|
| EdgeTtsService                                                                           | Data/Infra   | Kết nối Microsoft Edge Neural TTS qua WebSocket để sinh file MP3               |
| GeminiAiService                                                                          | Data/Infra   | Gọi Google AI Studio REST API (Gemini 2.5 Flash) sinh tóm tắt lịch sử POI     |
| Repository, DbContext                                                                    | Data/Infra   | Truy vấn/lưu Supabase Postgres                                                |
| SupabaseFileStorage                                                                      | Data/Infra   | Upload, xóa, tạo signed URL trên Supabase Storage                             |
| MockPaymentGateway                                                                       | Data/Infra   | Giả lập thanh toán (cấu hình kết quả để test lỗi)                             |

### 4.3 Phân định xác thực và phân quyền

- **Middleware (Presentation):** JWT Supabase hợp lệ và claim role = admin → cho vào /admin/\*. Token pass đúng định dạng, băm tra DB, chưa hết hạn, chưa thu hồi → gắn PassContext (pass_id, area_id).

- **Service (Business):** kiểm tra tài nguyên: pass có thuộc khu của POI không (PASS_AREA_MISMATCH).

- Role admin được gán bằng app_metadata trên Supabase Dashboard; MVP chỉ có một role nên không cần ma trận quyền.

### 4.4 Sơ đồ (mô tả để vẽ lại)

- **Component:** 3 hộp tầng xếp dọc, mũi tên Api → Business, Data → Business (implements), cả ba → Domain. Bên ngoài: Supabase Postgres, Supabase Storage, Supabase Auth (Api chỉ kiểm JWT), Payment Mock.

- **Use case:** Tourist (quét QR, nhận pass, xem POI gần, nghe audio); Admin (quản lý khu/POI/bản dịch/audio, tạo QR); Payment Provider.

## 5. Triển khai, CI/CD

### 5.1 CI (mỗi PR và push)

1.  dotnet restore (cache NuGet).

2.  dotnet build -warnaserror.

3.  dotnet format --verify-no-changes.

4.  dotnet test kèm coverage; **fail nếu coverage Business \< 60%**; có test kiến trúc.

5.  docker build (không push) để chắc chắn image build được.

6.  dotnet list package --vulnerable, fail nếu có mức High trở lên.

**AC:** workflow chạy ≤ 8 phút; main bật branch protection, bắt buộc CI xanh mới merge.

### 5.2 CD (merge vào main)

1.  Chạy lại CI (hoặc dùng kết quả CI của commit đó).

2.  Chạy migration lên Supabase bằng EF migration bundle, dùng chuỗi kết nối **Session pooler** lưu ở GitHub Secrets.

3.  Gọi **Deploy Hook** của Render; Render build image từ Dockerfile trong repo.

4.  Smoke test: gọi GET /health/ready, thử lại tối đa 3 phút (tính cả thời gian build và khởi động nguội).

5.  Nếu smoke test fail: workflow đỏ; rollback bằng cách redeploy commit trước đó (gọi lại deploy hook với commit trước hoặc dùng nút Rollback trên Render). Migration chỉ thêm, nên code cũ vẫn chạy được trên schema mới.

**AC:** từ merge đến /health/ready OK ≤ 10 phút; lần deploy đầu tiên (ngày 1) chỉ cần /health chạy qua HTTPS trên onrender.com.

### 5.3 Sơ đồ triển khai

| **Node**                  | **Thành phần**                           | **Ghi chú**                                          |
|---------------------------|------------------------------------------|------------------------------------------------------|
| Thiết bị khách, máy admin | Trình duyệt + web test client            | Test client deploy trên Vercel                       |
| GitHub                    | Repo, Actions                            | CI/CD, secrets                                       |
| Render (free)             | Web Service chạy Docker: API             | HTTPS sẵn; ngủ khi rảnh; biến môi trường chứa secret |
| Supabase (free)           | Postgres, Storage (bucket private), Auth | Dùng Session pooler; bật RLS                         |
| Dịch vụ ngoài             | Payment Provider (Later)                 | Qua Internet                                         |

**Biến môi trường chính trên Render:** ConnectionStrings\_\_Default, Supabase\_\_Url, Supabase\_\_ServiceRoleKey (chỉ server), Supabase\_\_JwtIssuer và khóa xác thực JWT (JWKS hoặc JWT secret, tùy cấu hình project Supabase), Supabase\_\_AudioBucket, Cors\_\_AllowedOrigins.

## 6. ERD và quan hệ dữ liệu

### 6.1 Thực thể (Postgres, schema public)

| **Bảng**        | **Cột chính**                                                                                                                                                                         | **MVP** |
|-----------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------|
| language        | **code** PK, name, is_active                                                                                                                                                          | (MVP)   |
| tour_area       | **id** uuid PK, code (unique), name, center_lat, center_lng, radius_m, price_usd numeric(10,2), pass_ttl_hours (mặc định 24), is_active                                               | (MVP)   |
| poi             | **id** uuid PK, area_id FK, lat, lng, trigger_radius_m (mặc định 30), priority, image_url nullable, is_active, created_at, updated_at. Index (area_id, is_active) và (lat, lng) | (MVP)   |
| poi_translation | **id** PK, poi_id FK, language_code FK, title, description, audio_path nullable (đường dẫn trong bucket), audio_duration_sec nullable, updated_at. **Unique (poi_id, language_code)** | (MVP)   |
| payment         | **id** PK, area_id FK, amount, currency, provider, provider_ref, idempotency_key **unique**, status (pending/succeeded/failed), created_at                                            | (MVP)   |
| access_pass     | **id** PK, area_id FK, payment_id FK unique, token_hash (SHA-256) **unique**, issued_at, expires_at, status (active/revoked)                                                          | (MVP)   |
| play_log        | **id** PK, pass_id FK, poi_id FK, language_code FK, started_at, listened_sec, completed. Index (poi_id, started_at)                                                                   | (MVP)   |

Tài khoản admin nằm trong **auth.users của Supabase**, không có bảng riêng. Khóa ngoại dùng ON DELETE RESTRICT. Mọi bảng bật RLS và không tạo policy.

### 6.2 Quan hệ

| **Quan hệ**                | **Bản số** |
|----------------------------|------------|
| tour_area — poi            | 1 – N      |
| poi — poi_translation      | 1 – N      |
| language — poi_translation | 1 – N      |
| tour_area — payment        | 1 – N      |
| payment — access_pass      | 1 – 0..1   |
| access_pass — play_log     | 1 – N      |
| poi — play_log             | 1 – N      |

### 6.3 Quy tắc dữ liệu và quyết định thiết kế

- **Không xóa cứng** khu, POI, bản dịch đã có dữ liệu liên quan: dùng is_active.

- Khách không có tài khoản, danh tính là AccessPass; bỏ VisitorSession nên ngôn ngữ do FE truyền qua ?lang.

- **Idempotency:** unique(idempotency_key). Gửi lại cùng key → 409 DUPLICATE_REQUEST, không trừ tiền lần hai. Token plaintext chỉ có trong response tạo pass; mất token thì mua lại.

- **POI image_url:** Cột `image_url` (varchar 500, nullable) lưu URL ảnh hiện vật/di tích. Cho phép null để đảm bảo tương thích ngược dữ liệu.

- **ActiveVisitorTracker (In-Memory Telemetry):** Trạng thái hoạt động trực tuyến của du khách được lưu trữ hoàn toàn trên RAM (`ConcurrentDictionary<Guid, DateTime>`) với cơ chế sliding window 30s, **không lưu vào bảng CSDL**. Quyết định thiết kế này nhằm: (1) Tránh gây quá tải I/O đĩa và cạn kiệt connection pooler trên Supabase Postgres khi hàng trăm máy khách gửi heartbeat mỗi 15s; (2) Tuân thủ triệt để nguyên tắc Privacy-by-Design (không lưu vết hành trình du khách vào đĩa cứng).

- Nearby MVP dùng bounding box + Haversine trong Service; khi POI lớn thì chuyển sang PostGIS.

## 7. API (prefix /api/v1)

Đây là **nguồn duy nhất** cho Acceptance Criteria của từng endpoint.

| **Story** | **Endpoint**                                 | **Auth**                                     | **Acceptance Criteria**                                                                                                                                                                                                                                                                                                     |
|-----------|----------------------------------------------|----------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| US-01     | GET /areas/{code}                            | Public                                       | 200 kèm tên, giá, hạn pass, danh sách ngôn ngữ is_active có ít nhất một POI có bản dịch; mã không có hoặc khu tắt → 404 AREA_NOT_FOUND                                                                                                                                                                                      |
| US-02     | POST /areas/{code}/passes                    | Public + rate limit + header Idempotency-Key | Thành công → 201 kèm access_token (chỉ trả một lần) và expires_at; cổng từ chối → 402 PAYMENT_FAILED (thử lại với key mới); trùng key → 409 DUPLICATE_REQUEST; thiếu key → 422. Token 32 byte CSPRNG, DB chỉ lưu SHA-256, mỗi payment tối đa 1 pass                                                                         |
| US-03     | (middleware)                                 | —                                            | Thiếu token → 401 PASS_MISSING; sai/hết hạn/thu hồi → 403 PASS_INVALID/PASS_EXPIRED; pass khác khu của POI → 403 PASS_AREA_MISMATCH                                                                                                                                                                                         |
| US-04     | GET /passes/current                          | AccessPass                                   | Trả trạng thái và expires_at                                                                                                                                                                                                                                                                                                |
| US-21     | POST /passes/heartbeat                       | AccessPass                                   | Ghi nhận sự hiện diện trực tuyến của du khách vào `ActiveVisitorTracker` (RAM sliding window 30s); không tốn I/O DB; trả về 204 No Content                                                                                                                                                                                  |
| US-05     | GET /pois/nearby?lat&lng&lang&radius         | AccessPass                                   | lat ∈ \[-90,90\], lng ∈ \[-180,180\] (sai → 422 VALIDATION_ERROR); trả POI is_active **thuộc khu của pass** trong bán kính (mặc định 100 m, tối đa 500 m) kèm lat, lng, distance_m, image_url và within_trigger (= distance ≤ trigger_radius_m); sắp theo priority giảm dần rồi distance tăng dần; **không lưu tọa độ**. Cờ within_trigger là tín hiệu cho client kích hoạt kịch bản **Auto-play Geofence** với Anti-jitter và Hysteresis buffer |
| US-06     | GET /pois/{id}?lang                          | AccessPass                                   | Trả title, description, image_url, language thực tế, fallback_language (null nếu không fallback). Thứ tự: ngôn ngữ chọn → en → vi. Không có bản dịch nào hoặc POI tắt → 404 POI_CONTENT_NOT_FOUND                                                                                                                            |
| US-07     | (trong response US-06)                       | AccessPass                                   | audio_url là signed URL hết hạn sau 1 giờ; thiếu audio → audio_url = null; Range/streaming do Supabase Storage đảm nhiệm                                                                                                                                                                                                    |
| US-08     | POST /play-logs                              | AccessPass                                   | Body: poi_id, language_code, listened_sec, completed; pass lấy từ token chứ không tin body; listened_sec ∈ \[0, 6 giờ\]; không lưu tọa độ                                                                                                                                                                                   |
| US-09     | (mọi /admin/\*)                              | JWT Supabase                                 | Thiếu hoặc sai JWT → 401 AUTH_REQUIRED; không có role admin → 403 FORBIDDEN                                                                                                                                                                                                                                                 |
| US-22     | POST /admin/auth/login                       | Public                                       | Body: email, password. Xác thực qua Supabase Auth; thành công trả 200 kèm access_token, refresh_token, expires_in (30 ngày), user_id, email, role=admin; sai mật khẩu/email → 401; không có role admin → 403 FORBIDDEN; trống thông tin → 422 VALIDATION_ERROR. Giúp client không bao giờ phơi bày Supabase Key            |
| US-22     | POST /admin/auth/refresh                     | Public                                       | Body: refresh_token. Tự động gia hạn phiên đăng nhập; thành công trả 200 kèm access_token mới và refresh_token mới; token sai hoặc hết hạn → 401; trống token → 422 VALIDATION_ERROR                                                                                                                                       |
| US-10     | CRUD /admin/areas                            | Admin                                        | code unique (chữ thường, số, gạch ngang); giá \> 0; xóa = is_active=false                                                                                                                                                                                                                                                   |
| US-11     | CRUD /admin/pois                             | Admin                                        | Validate lat/lng; trigger_radius_m ∈ \[5, 200\]; priority 0–100; image_url nullable (≤ 500 ký tự); xóa mềm; phân trang (page, page_size ≤ 100) và lọc theo khu                                                                                                                                                             |
| US-12     | CRUD /admin/pois/{id}/translations           | Admin                                        | Tối đa 1 bản dịch cho mỗi (poi_id, language_code) (trùng → 409 TRANSLATION_EXISTS); title ≤ 200 ký tự                                                                                                                                                                                                                       |
| US-13     | POST /admin/translations/{id}/audio          | Admin                                        | Chỉ mp3, kiểm tra **magic bytes** (ID3 hoặc frame sync), tối đa 10 MB; upload qua IFileStorage vào bucket private; thay audio thì xóa file cũ                                                                                                                                                                               |
| US-20     | POST /admin/translations/{id}/generate-audio | Admin                                        | Kết nối **Microsoft Edge Neural TTS** qua WebSocket, sinh file MP3 từ description bản dịch, upload trực tiếp lên Supabase Storage và cập nhật audio_path, audio_duration_sec; trả 200 kèm signed audio_url                                                                                                                   |
| US-14     | GET /admin/areas/{id}/qr                     | Admin                                        | Trả PNG chứa URL {PUBLIC_WEB_URL}/a/{code} (Could)                                                                                                                                                                                                                                                                          |
| US-19     | POST /admin/pois/{id}/ai-summary             | Admin                                        | Gọi Google Gemini 2.5 Flash API (Google AI Studio Free) sinh tóm tắt lịch sử hiện vật (150-200 từ); alias hỗ trợ: `POST /admin/pois/{id}/generate-story` (Could)                                                                                                                                                           |
| US-21     | GET /admin/analytics/dashboard               | Admin                                        | Trả toàn bộ chỉ số vận hành: tổng khu, tổng POI, tổng pass đã bán, tổng doanh thu USD, tổng lượt nghe, tổng thời lượng nghe, **số du khách đang online thời gian thực (`active_visitors_count`)** lấy từ `ActiveVisitorTracker`, top POI và phân bổ ngôn ngữ                                                                 |
| US-17     | GET /health, /health/ready                   | Public                                       | /health luôn 200 nếu process sống; /health/ready kiểm tra kết nối DB, 503 nếu lỗi                                                                                                                                                                                                                                           |

**Bảng mã lỗi** (ProblemDetails, trường code):

| **HTTP** | **code**                                                  | **Khi nào**                           |
|----------|-----------------------------------------------------------|---------------------------------------|
| 401      | PASS_MISSING, AUTH_REQUIRED                               | Thiếu token                           |
| 402      | PAYMENT_FAILED                                            | Cổng từ chối                          |
| 403      | PASS_INVALID, PASS_EXPIRED, PASS_AREA_MISMATCH, FORBIDDEN | Token sai, hết hạn, sai khu, sai role |
| 404      | AREA_NOT_FOUND, POI_CONTENT_NOT_FOUND                     | Không có hoặc đã tắt                  |
| 409      | DUPLICATE_REQUEST, TRANSLATION_EXISTS                     | Trùng key hoặc trùng bản dịch         |
| 422      | VALIDATION_ERROR                                          | Dữ liệu sai                           |
| 429      | RATE_LIMITED                                              | Vượt rate limit                       |

## 8. Kế hoạch (4 ngày)

| **Ngày** | **Nội dung**                                                                                                                                                                                           | **Story**                      |
|----------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------|
| Ngày 1   | Tạo solution 3 lớp + NetArchTest; project Supabase (Postgres, Storage, Auth); entity, migration, seed (1 khu, 6 POI, 3 ngôn ngữ); bật RLS; Dockerfile; **CI**; deploy /health lên Render + deploy hook | US-15, US-16 (bản đầu), US-17  |
| Ngày 2   | Xác thực JWT Supabase; CRUD khu/POI/bản dịch; upload audio lên Storage; nạp audio cho 6 POI                                                                                                            | US-09 → US-13                  |
| Ngày 3   | Pass + thanh toán mock idempotent + middleware; nearby; content + fallback + signed URL; play-log; QR (nếu còn thời gian)                                                                              | US-01 → US-08, US-14           |
| Ngày 4   | Test: unit, coverage ≥ 60%, 1–2 integration test; web test client Leaflet trên Vercel; hoàn thiện CD + smoke test; đo tải; báo cáo và demo                                                             | US-15, US-16 (hoàn chỉnh), NFR |

**Definition of Done mỗi story:** code qua CI xanh, có unit test Service, Swagger cập nhật, deploy được lên Render. **Nếu trễ tiến độ, cắt theo thứ tự:** US-14 → US-04 → US-08 → integration test.

## 9. Rủi ro

| **Rủi ro**                                        | **Tác động**             | **Xử lý**                                                                     |
|---------------------------------------------------|--------------------------|-------------------------------------------------------------------------------|
| Render free ngủ (cold start 30–60 giây)           | Demo bị chậm request đầu | Gọi /health trước demo; smoke test CD chờ tối đa 3 phút; ghi rõ trong báo cáo |
| Supabase free tạm dừng sau 1 tuần không hoạt động | Mất kết nối DB           | Mở lại project trước demo; có /health/ready để phát hiện                      |
| Kết nối Postgres từ Render lỗi (IPv6/pooler)      | API không lên            | Dùng chuỗi Session pooler; kiểm tra ngay ngày 1                               |
| Lộ dữ liệu qua API công khai của Supabase         | Lộ nội dung đã trả phí   | Bật RLS không policy; bucket private; không đưa service-role key ra client    |
| Quá ít thời gian (4 ngày, một người)              | Không đủ phạm vi         | Chốt thứ tự cắt ở mục 8; giữ CI/CD và 3 lớp là ưu tiên cao nhất               |
| GPS kém chính xác                                 | Nhận nhầm điểm           | trigger_radius_m theo POI; priority làm tiêu chí phụ; debounce ở FE           |
| Dữ liệu vị trí nhạy cảm                           | Rủi ro riêng tư          | Server không lưu tọa độ, log không ghi tọa độ                                 |

**Câu hỏi mở còn lại:** (1) tên khu và 6 POI cụ thể (tọa độ thật); (2) cách cấu hình khóa xác thực JWT của project Supabase (JWKS hay JWT secret); (3) Render deploy hook có hỗ trợ chỉ định commit để rollback hay chỉ dùng nút Rollback. Cần chốt trong ngày 1.

## Phụ lục A — ADR rút gọn

| **Quyết định** | **Lựa chọn**                                                | **Lý do**                                                                            |
|----------------|-------------------------------------------------------------|--------------------------------------------------------------------------------------|
| Stack          | ASP.NET Core 3 lớp + Supabase Postgres                      | Quen với .NET; dữ liệu quan hệ chặt (payment, pass); Supabase có sẵn Storage và Auth |
| Auth admin     | Supabase Auth, API chỉ kiểm JWT                             | Giảm khối lượng (hash, khóa tài khoản); chỉ cần một role                             |
| Lưu audio      | Supabase Storage (private) + signed URL                     | Đĩa Render free không bền; signed URL tránh lộ nội dung                              |
| Geofence       | Server stateless tính khoảng cách; client debounce/cooldown | Không cần Redis, dễ test, không lưu tọa độ                                           |
| AccessPass     | Token opaque, lưu hash; gắn trực tiếp với khu               | Thu hồi được; bỏ VisitorSession cho gọn                                              |
| Deploy         | Render free + deploy hook                                   | HTTPS sẵn, không cần VPS/domain; có smoke test và rollback                           |
| Migration      | Chạy trong pipeline, chỉ thêm                               | Tránh nhiều instance cùng migrate; rollback code an toàn                             |
| Audio TTS      | Microsoft Edge Neural TTS qua WebSocket                     | Tự động sinh giọng đọc tự nhiên; không tốn phí; giải quyết bài toán thiếu audio mẫu  |
| Live Telemetry | In-Memory Sliding Window 30s (`ConcurrentDictionary`)        | Đo du khách online thời gian thực; 0 disk I/O; không lưu GPS/token nhạy cảm          |
| Auto-play Geo  | Client Auto-play kèm Anti-jitter và Hysteresis buffer (+8m) | Trải nghiệm rảnh tay thực tế; loại bỏ gián đoạn/giật âm thanh do sai số GPS         |
