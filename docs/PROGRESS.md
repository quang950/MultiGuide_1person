# PROGRESS — nhật ký trạng thái

> AI cập nhật file này cuối **mỗi task**. Chỉ giữ trạng thái hiện tại ở đầu file; lịch sử nằm bên dưới (mới nhất ở trên).

## Trạng thái hiện tại (cập nhật: 28/10/2026 — Hoàn thành Tính năng Hiển thị Tọa độ Du khách Real-time dưới nút Recenter)
- **Đang làm:** Sẵn sàng cho người dùng nghiệm thu.
- **Vừa xong:** 
  1. **Tính năng Hiển thị Tọa độ Du khách (Client - FE-3.4)**:
     - Bổ sung cụm điều khiển GPS .map-gps-control-group và badge #userCoordsDisplay hiển thị tọa độ trực tiếp dưới nút tròn Recenter ở góc dưới bên phải.
     - Tích hợp 2 nguồn dữ liệu: Tọa độ phần cứng GPS thực tế (
avigator.geolocation) chấm xanh lá, và tọa độ bước chân mô phỏng (Click-to-Move) chấm màu đồng di sản.
     - Hỗ trợ chạm 1 chạm vào badge để sao chép nhanh chuỗi tọa độ (hỗ trợ kiểm thử thực địa thuận tiện).
     - Đảm bảo 100% Client-side, tuyệt đối không gửi hay log tọa độ lên CSDL Backend tuân thủ quy tắc bảo mật riêng tư.
  2. **Tầng Backend & Kiểm thử**:
     - dotnet build MultiGuide.sln -warnaserror: 0 Warning, 0 Error.
     - dotnet test MultiGuide.sln: 331/331 tests GREEN (100%).
     - dotnet format MultiGuide.sln --verify-no-changes: Đạt chuẩn 100%.
- **Kết quả kiểm thử:**
  - dotnet build MultiGuide.sln -warnaserror: **0 Warning, 0 Error**.
  - dotnet test MultiGuide.sln: **330/330 tests GREEN (100%)**.
  - dotnet format MultiGuide.sln --verify-no-changes: **Đạt chuẩn 100%**.
- **Bước kế tiếp:** Người dùng nghiệm thu trực tiếp.
- **Đang vướng:** không
- **Cách kiểm tra nhanh:** Mở http://127.0.0.1:5500/client/index.html hoặc http://127.0.0.1:5500/client/admin.html

## Giá trị cần ghi khi có (không ghi secret)
- URL Render: https://multiguide-1person.onrender.com (Live trên Render, HTTPS hoạt động)
- Supabase project ref: xbrfdxtmnhdcozblyilo (URL: https://xbrfdxtmnhdcozblyilo.supabase.co, Region: Singapore, Bucket: audio)
- Tên khu + 6 POI: Văn Miếu - Quốc Tử Giám (`van-mieu`); 6 POI: Cổng Văn Miếu, Khuê Văn Các, Giếng Thiên Quang & Vườn Bia Tiến Sĩ, Cổng Đại Thành & Sân Đại Bái, Điện Đại Thành, Nhà Thái Học
- Cách xác thực JWT Supabase (JWKS / JWT secret): ECC P-256 (JWKS qua `Supabase__JwtIssuer`) kèm hỗ trợ HS256 (`Supabase__JwtSecret`)
- Admin user: `quanghi176@gmail.com` (role: `admin`)
- Model Gemini AI: `gemini-2.5-flash` (Google AI Studio Free Tier)

## Lịch sử
- 15/10 — Hoàn thành Task T-4.2: Tích hợp Testcontainers PostgreSQL 16 (`PostgresIntegrationTests`), kiểm thử luồng mua vé, xác minh trực tiếp trong database, kiểm thử idempotency và truy vấn POI lân cận với token hash SHA-256 trên container PostgreSQL thật. Nâng tổng số bài test lên 314/314 tests xanh lá (100%). Hoàn thành 100% toàn bộ kế hoạch 4 Ngày của dự án!
- 15/10 — Hoàn thành Task T-4.4 (US-19): Triển khai `IAiService` trong Business, `GeminiAiService` trong Data gọi Google AI Studio REST API, endpoint `POST /admin/pois/{id}/ai-summary` trong `AdminPoisController` (phân quyền admin); bổ sung 14 bài test nâng tổng test suite lên 311/311 tests xanh lá (100%).
- 15/10 — Hoàn thành Cụm 3 (T-4.8, T-4.9, T-4.10): Biên soạn tài liệu `README.md` chuyên sâu, kịch bản thuyết trình demo 5 phút `docs/DEMO_SCRIPT.md`, bộ 5 sơ đồ UML/Mermaid báo cáo đồ án `docs/REPORT_DIAGRAMS.md`.
- 15/10 — Hoàn thành T-4.6 & T-4.7: Hoàn thiện pipeline CD `cd.yml` (migration bundle Session Pooler, deploy hook, smoke test `/health/ready` 3 phút); xây dựng kịch bản k6 `load_test_k6.js` & runner `run_load_test.py`, đo tải thành công trên Render đạt 12.96 req/s, 0% lỗi, xuất báo cáo `docs/LOAD_TEST_RESULTS.md`.
- 15/10 — Hoàn thành T-4.3 & T-4.5: Xây dựng Web Test Client Leaflet 1 trang responsive trong `client/` kèm cấu hình `vercel.json`; cấu hình CORS trên Backend `MultiGuide.Api` hỗ trợ localhost & `*.vercel.app`, thêm bộ integration test `CorsTests` (7 bài); toàn bộ 297/297 test xanh lá.
- 15/10 — Hoàn thành T-4.1: Bổ sung các unit test còn thiếu (`PoiServiceTests`, `SystemClockTests`, `DependencyInjectionTests`), nâng test suite lên 290/290 test xanh; xây dựng `scripts/check_coverage.py` và tích hợp rào chắn CI kiểm tra ngưỡng tối thiểu 60% (độ phủ thực tế đạt 96.41% Line Coverage và 98.96% Branch Coverage).
- 15/10 — Hoàn thành MỐC CUỐI NGÀY 3: Kiểm thử tự động E2E thành công 100% trên Render (`https://multiguide-1person.onrender.com`) luồng khách: `areas` -> `passes` -> `nearby` -> `pois/{id}` -> `passes/current`. Toàn bộ Ngày 3 đã hoàn tất xuất sắc (286/286 tests xanh).
- 15/10 — Hoàn thành MỐC CUỐI NGÀY 3: Kiểm thử tự động E2E thành công 100% trên Render (`https://multiguide-1person.onrender.com`) luồng khách: `areas` -> `passes` -> `nearby` -> `pois/{id}` -> `passes/current`. Toàn bộ Ngày 3 đã hoàn tất xuất sắc (286/286 tests xanh).
- 15/10 — Hoàn thành T-3.11 & T-3.12: Triển khai `PassesController` (`GET /passes/current`), `PlayLogsController` (`POST /play-logs`), `PlayLogService`, `PlayLogRepository`; bổ sung 13 unit tests và 12 integration tests; toàn bộ 277/277 tests xanh lá.
- 15/10 — Hoàn thành T-3.9 & T-3.10: Triển khai `ContentService` (fallback ngôn ngữ lang → en → vi, bắt 404 POI_CONTENT_NOT_FOUND, 403 PASS_AREA_MISMATCH, sinh Signed URL 1 giờ qua IFileStorage), `IPoiRepository.GetWithTranslationsByIdAsync`, endpoint `GET /pois/{id}?lang` bảo vệ bằng `[RequireAccessPass]`; bổ sung 10 unit tests và 9 integration tests; toàn bộ 252/252 tests xanh lá.
- 15/10 — Hoàn thành T-3.7 & T-3.8: Triển khai `GeoService` (Haversine distance, Bounding box, cờ within_trigger, sắp xếp Priority/Distance), `PoiService`, `IPoiRepository.GetActiveByAreaIdAsync` và endpoint `GET /pois/nearby` (US-05) bảo vệ bằng `[RequireAccessPass]`; bổ sung 10 unit tests và 10 integration tests; toàn bộ 233/233 tests xanh lá.
- 15/10 — Hoàn thành T-3.6: Triển khai Middleware `AccessPassMiddleware`, attribute `[RequireAccessPass]`, method `ValidateTokenAsync` trong `PassService`, gắn `PassContext` vào `HttpContext`; xử lý đầy đủ các lỗi 401 `PASS_MISSING`, 403 `PASS_INVALID`, 403 `PASS_EXPIRED`; bổ sung 5 unit tests và 6 integration tests; toàn bộ 213/213 tests xanh lá.
- 14/10 — Hoàn thành T-3.5: Cấu hình Rate Limiter middleware (`Microsoft.AspNetCore.RateLimiting`) với Fixed Window (5 req/phút/IP), tùy biến `OnRejected` trả ProblemDetails 429 `code: "RATE_LIMITED"`, áp dụng vào endpoint `CreatePass` trong `AreasController`; bổ sung 2 integration tests trong `RateLimitingTests` bao quát kịch bản vượt ngưỡng và cô lập IP; toàn bộ 197/197 tests xanh lá.
- 15/10 — Hoàn thành MỐC CUỐI NGÀY 3: Kiểm thử tự động E2E thành công 100% trên Render (`https://multiguide-1person.onrender.com`) luồng khách: `areas` (quét QR Văn Miếu) -> `passes` (mua vé cấp token 64 ký tự) -> `nearby` (GPS 5 POI kèm cờ withinTrigger) -> `pois/{id}` (lấy bài thuyết minh tiếng Việt kèm signed URL audio từ Supabase Storage) -> `passes/current` (trạng thái vé active). Toàn bộ Ngày 3 đã hoàn tất xuất sắc (286/286 tests xanh).
- 15/10 — Hoàn thành T-3.13 & T-3.14: Tích hợp thư viện QRCoder sinh mã QR cho khu du lịch dạng PNG không phụ thuộc GDI+ (`GET /admin/areas/{id}/qr`), kiểm tra và tạo architecture tests bảo mật quyền riêng tư không log hay lưu lat, lng, token.
- 15/10 — Hoàn thành T-3.11 & T-3.12: Triển khai endpoint `GET /passes/current` (US-04) và `POST /play-logs` (US-08) ghi nhận thời gian nghe thuyết minh của khách.
- 15/10 — Hoàn thành T-3.9 & T-3.10: Triển khai `ContentService` và endpoint `GET /pois/{id}?lang` (US-06, US-07), hỗ trợ fallback ngôn ngữ động và sinh signed URL audio 1 giờ từ Supabase Storage.
- 14/10 — Hoàn thành T-3.7 & T-3.8: Triển khai `GeoService` tính khoảng cách Haversine và endpoint `GET /pois/nearby` (US-05).
- 14/10 — Hoàn thành T-3.5 & T-3.6: Triển khai `AccessPassMiddleware`, `PassContext` xác thực token khách tham quan trên các endpoint được bảo vệ.
- 14/10 — Hoàn thành T-3.4: Triển khai endpoint `POST /areas/{code}/passes` (US-02) đọc header `Idempotency-Key`, gọi `PassService`, trả 201 Created kèm AccessToken plaintext duy nhất; xử lý các mã lỗi 402, 404, 409, 422; bổ sung 7 integration tests bao quát các trường hợp; test suite đạt 195/195 tests xanh lá.
- 14/10 — Hoàn thành T-3.3: Triển khai `PassService` sinh token 32-byte CSPRNG, băm SHA-256 lưu DB, tính `ExpiresAt = UtcNow + pass_ttl_hours`, kiểm tra IdempotencyKey trùng lặp (409) và cổng từ chối (402); tạo interface và repository cho `Payment` & `AccessPass`; thêm 12 unit tests trong `PassServiceTests`; test suite đạt 188/188 tests xanh lá.
- 14/10 — Hoàn thành T-3.2: Định nghĩa interface `IPaymentGateway`, `PaymentRequest`, `PaymentResult` trong tầng Business độc lập; hiện thực `MockPaymentGateway` trong tầng Data hỗ trợ cấu hình kết quả (success/failed/failure/default) qua `Payment:MockResult` / `Payment__MockResult`; đăng ký DI; bổ sung 11 unit tests trong `MockPaymentGatewayTests`; toàn bộ 176/176 tests xanh lá.
- 14/10 — Hoàn thành T-3.1: Triển khai endpoint Public `GET /areas/{code}` (và `/api/v1/areas/{code}`) (US-01) kèm DTO `AreaDetailResponse`, interface `IAreaRepository.GetSupportedLanguagesAsync`, `IAreaService` và `AreaService` chuẩn 3 lớp; viết 8 unit tests và 4 integration tests bao quát các trường hợp mã không tồn tại, khu bị tắt, lọc ngôn ngữ/POI active; test suite đạt 165/165 test xanh lá.
- 14/10 — Hoàn thành T-2.10 & MỐC CUỐI NGÀY 2: Tạo script sinh 18 file audio MP3 bằng Microsoft Edge Neural TTS cho 6 POI × 3 ngôn ngữ (vi, en, ja), upload lên Supabase Storage bucket `audio` và cập nhật `audio_path` cho 18/18 bản dịch trong CSDL; kiểm thử signed URL stream audio 200 OK; toàn bộ Ngày 2 đã hoàn thành 100%.
- 14/10 — Hoàn thành T-2.8 & T-2.9: Triển khai endpoint `POST /admin/translations/{id}/audio` (US-13); kiểm tra dung lượng ≤ 10 MB; kiểm tra magic bytes MP3 (ID3v2 và MPEG Layer III Frame Sync); bóc tách `Stream` tại Controller giữ Business độc lập không phụ thuộc HTTP; xóa audio cũ khi thay file hoặc khi xóa bản dịch; thêm 8 unit test và 8 integration test bao quát trọn vẹn; test suite đạt 153/153 xanh lá (80 Business, 58 Integration, 15 Architecture).
- 14/10 — Hoàn thành T-2.7: Interface `IFileStorage` (Upload, Delete, CreateSignedUrl) trong tầng Business độc lập; hiện thực `SupabaseFileStorage` trong tầng Data gọi Supabase Storage REST API qua `HttpClient` với ServiceRoleKey; cấu hình DI và thêm 10 bài test (`SupabaseFileStorageTests`), suite test đạt 136/136 bài test xanh (kể cả NetArchTest).
- 14/10 — Hoàn thành T-2.6: CRUD bản dịch POI (US-12) đủ 3 lớp; thêm 24 unit test + 10 integration test.
- 14/10 — Hoàn thành T-2.5: CRUD `/admin/pois` (US-11) đủ 3 lớp, query phân trang dùng `page` + `page_size` theo PRD; thêm 18 unit test + 7 integration test.
- 14/10 — Hoàn thành T-2.4: Tạo `AdminAreasController` (/admin/areas và /api/v1/admin/areas), bảo vệ bằng policy role=admin, hỗ trợ GET, POST, PUT, DELETE (xóa mềm). Viết 4 integration tests trong `AdminAreasControllerTests` kiểm thử trọn vẹn luồng CRUD, bắt trùng mã 409 DUPLICATE_REQUEST, validate 422 VALIDATION_ERROR và 401/403 (toàn bộ 56/56 bài test xanh).
- 14/10 — Hoàn thành T-2.3: Định nghĩa interface `IAreaRepository`, DTOs và hiện thực `AdminContentService` trong tầng Business với đầy đủ validation và logic xóa mềm; hiện thực `AreaRepository` trong tầng Data bằng EF Core; bổ sung 21 unit tests trong `AdminContentServiceTests`.
- 14/10 — Hoàn thành T-2.2: Tạo user admin `quanghi176@gmail.com` trên Supabase Dashboard, gán `app_metadata` role=admin qua SQL, xác thực và lấy token thật thành công qua Auth API.
- 14/10 — Hoàn thành T-2.1: Cấu hình xác thực JWT Supabase và policy role=admin trên `/admin/*` (US-09), triển khai AdminRoleRequirement, AdminRoleHandler, ProblemDetailsAuthorizationMiddlewareResultHandler (401 AUTH_REQUIRED, 403 FORBIDDEN), cấu hình Swagger UI Bearer, hoàn thiện bộ 6 integration tests trong AdminAuthTests; test suite nâng lên 31/31 xanh.
- 13/10 — Hoàn thành mốc Ngày 1 (T-1.12, T-1.13, T-1.14): Kích hoạt branch protection cho `main`, tạo Web Service `multiguide-1person` trên Render (Singapore), kết nối DB Supabase, tự động seed 6 POI Văn Miếu, kiểm tra `/health`, `/health/ready` và `/swagger` qua HTTPS thành công; tạo workflow `.github/workflows/cd.yml` tự động gọi Deploy Hook và smoke test.
- 13/10 — Hoàn thành T-1.9: Tạo `AppException` và các exception dẫn xuất (`NotFoundException`, `ValidationException`, `ConflictException`, `UnauthorizedException`, `ForbiddenException`, `PaymentRequiredException`), hằng số `ErrorCodes` trong `MultiGuide.Domain`; triển khai `GlobalExceptionHandler` trong `MultiGuide.Api` ghi đè `ProblemDetails` kèm trường mở rộng `code`, xử lý validation 422 tự động, BaseApiController `/api/v1`, OpenAPI Swagger UI v1, bổ sung integration tests `ExceptionHandlerTests` (25/25 tests xanh).
- 13/10 — Hoàn thành T-1.8: Hoàn thiện cấu hình DI (`AddBusinessServices`, `AddDataServices`) trong `Program.cs`, triển khai endpoint `GET /health/ready` xác thực DB readiness (200/503), tự động seed dữ liệu demo theo cờ cấu hình, viết bộ integration test `HealthEndpointsTests` (offline InMemory), toàn bộ test suite 18/18 xanh.
- 13/10 — Hoàn thành T-1.7: Tạo `DbSeeder` trong `MultiGuide.Data` hỗ trợ seed 3 ngôn ngữ (`vi`, `en`, `ja`), 1 tour area (`van-mieu`), 6 POI tọa độ thực tế tại Văn Miếu và 18 PoiTranslation. Hỗ trợ cờ cấu hình `Seed:Enabled` / `Seed__Enabled` (mặc định tắt), đảm bảo tính idempotent và offline unit tests với In-Memory EF Core (14/14 tests xanh).
- 13/10 — Hoàn thành T-1.6: Tạo migration InitialCreate (7 bảng, indexes, unique, ON DELETE RESTRICT), tự động chạy SQL bật RLS (Row Level Security) không policy cho cả 7 bảng, kết nối và cập nhật thành công lên Supabase Session pooler; test 10/10 xanh.
- 13/10 — Hoàn thành T-1.5: Người dùng tạo xong project Supabase (ref: xbrfdxtmnhdcozblyilo, Region Singapore), bucket `audio` (private), cấu hình file .env thành công.
- 13/10 — Hoàn thành T-1.4: Xây dựng MultiGuideDbContext với 7 DbSets, cấu hình Fluent API đầy đủ chuẩn ERD (7 bảng, index, unique, ON DELETE RESTRICT, enum string, UTC), DesignTimeDbContextFactory hỗ trợ DATABASE_URL / postgres URI, DependencyInjection extension, unit test xác thực 10/10 test xanh.
- 13/10 — Hoàn thành T-1.3: Tạo 7 Domain entities (Language, TourArea, Poi, PoiTranslation, Payment, AccessPass, PlayLog), 2 enums (PaymentStatus, AccessPassStatus), xuất tài liệu docs/ERD.md; build và NetArchTest 5/5 xanh.
- 13/10 — Hoàn thành khung Ngày 1 (T-1.1, T-1.2, T-1.8 một phần, T-1.10, T-1.11), test kiến trúc 5/5 xanh, /health trả 200, Dockerfile & CI sẵn sàng.
- 13/10 — Hoàn thành PRD v1.2 và bộ tài liệu khởi tạo.
