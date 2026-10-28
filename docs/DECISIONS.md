# DECISIONS — các quyết định đã chốt

> Đã chốt thì không đề xuất lại. Muốn đổi → hỏi người dùng, ghi lý do mới vào cuối file.

| # | Quyết định | Lý do |
|---|---|---|
| D1 | ASP.NET Core 3 lớp, setup lại từ đầu | Quen .NET; 3 lớp rõ cho môn học |
| D2 | CSDL = Supabase Postgres (EF Core + Npgsql), kết nối qua **Session pooler** | Render không dùng được kết nối trực tiếp IPv6 của Supabase |
| D3 | Admin dùng **Supabase Auth**; API chỉ kiểm JWT + claim role=admin | Bỏ hash mật khẩu, khóa tài khoản; MVP 1 role |
| D4 | Audio lưu **Supabase Storage** bucket private, trả **signed URL** (1 giờ) | Đĩa Render free không bền; tránh lộ nội dung trả phí |
| D5 | **Bỏ VisitorSession**; `POST /areas/{code}/passes` tạo payment + pass; ngôn ngữ do FE truyền `?lang` | Giảm bảng và endpoint |
| D6 | Idempotency đơn giản: unique(idempotency_key), trùng → 409 | Thanh toán chỉ là mock |
| D7 | 1 role `admin` | MVP |
| D8 | Geofence: server stateless, tính **Haversine** trong Service; debounce/cooldown ở FE; không lưu tọa độ | 6 POI, dễ test, riêng tư |
| D9 | Deploy: Render free (Docker) + **Deploy Hook** từ GitHub Actions + smoke test | HTTPS sẵn, có CD thật |
| D10 | Migration chạy trong pipeline (EF bundle), chỉ thêm | Rollback code an toàn |
| D11 | Test: xUnit unit (Business), NetArchTest, 1–2 Testcontainers Postgres; coverage Business ≥ 60% | Chứng minh 3 lớp |
| D12 | Cắt khỏi MVP: upload ảnh, content-status, rate limit nhiều tầng, correlation id, request_hash, PostGIS, QR là Could | 4 ngày, 1 người |
| D13 | Rate limit chỉ cho `POST /areas/{code}/passes`: 5 req/phút/IP | Đăng nhập admin do Supabase xử lý |
| D14 | Test client FE: Leaflet, deploy Vercel, bấm bản đồ để giả lập vị trí | Test geofence không cần GPS |
| D15 | Dùng .NET 10 (LTS), net10.0; ghim SDK bằng global.json | Máy dev chỉ có SDK 9/10 |
| D16 | Tích hợp Google Gemini 2.5 Flash qua Google AI Studio REST API (Free tier, HttpClient) cho tính năng sinh tóm tắt lịch sử / câu chuyện POI | Tiện lợi, chuẩn, free, không cài thêm SDK nặng; phục vụ Admin pre-generate hoặc on-demand cache |
| D17 | Bổ sung trường `image_url` (varchar 500, nullable) vào POI | Phục vụ FE hiển thị hình ảnh hiện vật/di tích trên bản đồ và bottom sheet |
| D18 | Tích hợp **Microsoft Edge Neural TTS** qua WebSocket (`ITtsService` -> `EdgeTtsService`) sinh audio tự động cho bản dịch | Giải quyết bài toán thiếu file audio thu âm sẵn; tự động lưu file MP3 lên Supabase Storage qua endpoint `POST /admin/translations/{id}/generate-audio` |
| D19 | Đo lường **Du khách trực tuyến thời gian thực (Live Active Visitors)** bằng cơ chế Heartbeat 15s kết hợp **In-Memory Sliding Window 30s** (`ConcurrentDictionary<Guid, DateTime>`) và Auto-polling 15s tại Admin Dashboard | Đảm bảo phản ánh đúng người dùng đang truy cập thực tế với độ trễ tối đa 15-30s; tuân thủ nghiêm ngặt bảo mật không lưu GPS/token; không tốn I/O đĩa DB |
| D20 | Tự động hóa trải nghiệm thuyết minh Geofencing (**Auto-play Calm Tech**): khách vào bán kính $\le 30$m tự động mở Bottom Sheet và stream audio; áp dụng chống lặp tín hiệu (**Anti-jitter**) và đệm thoát (**Hysteresis buffer +8m**) | Nâng tầm trải nghiệm thực tế (khách đi bộ không cần bấm tay); chống giật popup và ngắt quãng âm thanh khi đứng yên |
| D21 | **Zero-Exposure Admin Auth Proxy** (`POST /api/v1/admin/auth/login` qua `IAdminAuthService` -> `SupabaseAdminAuthService`) | Bảo vệ an toàn tuyệt đối Anon Key/Service Key của Supabase không bị phơi ra trình duyệt; phát hành Long-lived JWT 30 ngày cho Admin và bỏ endpoint refresh token riêng để tránh lỗi rate limit của Supabase Auth |
| D22 | Xây dựng **Admin SPA hoàn chỉnh** (`client/admin.html`) và **PWA Active Cache Buster** (`client/sw.js`) | Cung cấp giao diện trực quan cho quản trị viên vận hành; loại bỏ triệt để hiện tượng kẹt cache client cũ khi deploy phiên bản mới |
| D23 | Kiểm tra ràng buộc ngoại `language_code` trong `PlayLogService` qua `ILanguageRepository` | Trả về 422 `VALIDATION_ERROR` nếu gửi ngôn ngữ không tồn tại/bị tắt, ngăn ngừa lỗi 500 vi phạm Foreign Key trong CSDL PostgreSQL thật |
| D24 | Giữ endpoint `POST /passes/heartbeat` trả về 200 OK kèm payload JSON `{ status, timestamp }` | Giúp client đồng bộ thời gian và kiểm tra phản hồi kết nối trực tuyến |

## Thứ tự cắt nếu trễ
US-14 (QR) → US-19 (Gemini AI Story) → US-04 (/passes/current) → US-08 (play-log) → integration test.
