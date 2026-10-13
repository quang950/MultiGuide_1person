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

## Thứ tự cắt nếu trễ
US-14 (QR) → US-19 (Gemini AI Story) → US-04 (/passes/current) → US-08 (play-log) → integration test.
