# PROGRESS — nhật ký trạng thái

> AI cập nhật file này cuối **mỗi task**. Chỉ giữ trạng thái hiện tại ở đầu file; lịch sử nằm bên dưới (mới nhất ở trên).

## Trạng thái hiện tại (cập nhật: 14/10/2026 — hoàn thành T-2.7)
- **Đang làm:** Sẵn sàng bước sang Task T-2.8 (`POST /admin/translations/{id}/audio`)
- **Vừa xong:** T-2.7: Interface `IFileStorage` (Upload, Delete, CreateSignedUrl) trong tầng Business độc lập hoàn toàn với HTTP; `SupabaseFileStorage` trong tầng Data giao tiếp REST API qua `HttpClient` sử dụng ServiceRoleKey; cấu hình DI; 136/136 test xanh (10 test cho `SupabaseFileStorage`, 15 NetArchTest, 72 Business test, 39 Integration test khác).
- **Bước kế tiếp:** T-2.8: `POST /admin/translations/{id}/audio`: kiểm magic bytes mp3, ≤ 10 MB, thay audio thì xóa file cũ — US-13
- **Đang vướng:** không
- **Lưu ý cho T-2.8:** Sử dụng `IFileStorage` để upload mp3 vào private bucket `audio`, xóa file audio cũ khi thay thế hoặc khi xóa bản dịch.
- **Cách kiểm tra nhanh:** `dotnet test MultiGuide.sln`
 
## Giá trị cần ghi khi có (không ghi secret)
- URL Render: https://multiguide-1person.onrender.com (Live trên Render, HTTPS hoạt động)
- Supabase project ref: xbrfdxtmnhdcozblyilo (URL: https://xbrfdxtmnhdcozblyilo.supabase.co, Region: Singapore, Bucket: audio)
- Tên khu + 6 POI: Văn Miếu - Quốc Tử Giám (`van-mieu`); 6 POI: Cổng Văn Miếu, Khuê Văn Các, Giếng Thiên Quang & Vườn Bia Tiến Sĩ, Cổng Đại Thành & Sân Đại Bái, Điện Đại Thành, Nhà Thái Học
- Cách xác thực JWT Supabase (JWKS / JWT secret): ECC P-256 (JWKS qua `Supabase__JwtIssuer`) kèm hỗ trợ HS256 (`Supabase__JwtSecret`)
- Admin user: `quanghi176@gmail.com` (role: `admin`)

## Lịch sử
- 14/10 — Hoàn thành T-2.7: Xây dựng interface `IFileStorage` (Upload, Delete, CreateSignedUrl) trong tầng Business độc lập; hiện thực `SupabaseFileStorage` trong tầng Data gọi Supabase Storage REST API qua `HttpClient` với ServiceRoleKey; cấu hình DI và thêm 10 bài test (`SupabaseFileStorageTests`), suite test đạt 136/136 bài test xanh (kể cả NetArchTest).
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
